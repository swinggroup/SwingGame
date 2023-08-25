using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using UnityEngine.WSA;

public class PlayerController : MonoBehaviour
{

    public Animator animator;

    public static readonly float GRAPPLE_RANGE = 9;
    public static readonly float DELAY_NORMAL = 0.4f;
    public static readonly float DELAY_SWING = 0.7f;
    public static readonly int MAX_JUMP_FRAMES = 23;

    GameObject swingedObject;

    public AudioClip grappleSound;
    public AudioClip whiffSound;
    public AudioClip jumpSound;

    int jumpFixedFrames;
    // bool delaying; on hold for now
    Rigidbody2D rb;
    Rope rope;
    State state;
    LineRenderer ropeLine;
    Vector2 spinVelocity;
    const float gravity = 4f;
    bool canSwing = true;
    public Tilemap cloudMap;
    public Tilemap cloudDistanceMap;
    public Tilemap wallMap;
    SortedDictionary<float, List<Tuple<Vector3Int, TileBase>>> CloudDistanceList;

    public (bool,bool) wallCollision = (false,false);
    public bool floorCollision = false;
    public bool delayingSwing = false;

    public GameObject winScreen;

    private class RevolutionData
    {
        public static float threshold;
        public static int positionRelativeToThreshold;
        public static int positionSwitchCount;
    }

    private class Rope
    {
        public double length;
        public Vector2 anchorPoint;
        GameObject player;
        Rigidbody2D rb;

        public Rope(GameObject player)
        {
            this.player = player;
            this.rb = player.GetComponent<Rigidbody2D>();
        }

        public bool IsTaut()
        {
            Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
            Vector2 playerToAnchor = anchorPoint - playerPos;
            if (Vector2.Dot(playerToAnchor, rb.velocity) <= 0)
            {
                return Vector2.Distance(playerPos, anchorPoint) >= length;
            } else
            {
                return false;
            }
        }

        public Vector2 NormalizedPlayerToAnchor()
        {
            Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
            Vector2 vec = (anchorPoint - playerPos).normalized;
            return vec;
        }
    }

    enum State
    {
        Grounded, Airborne, Attached, Swinging
    }

    // Start is called before the first frame update
    void Start()
    {
        ropeLine = this.gameObject.AddComponent<LineRenderer>();
        ropeLine.enabled = false;
        rb = this.GetComponent<Rigidbody2D>();
        rope = new(this.gameObject);
        rb.gravityScale = gravity;
        CloudDistanceList = new();
        state = State.Airborne;
    }

    private void LateUpdate()
    {
        wallCollision = (false, false);
        floorCollision = false;
    }


    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyUp(KeyCode.F))
        {
            Debug.Log("-----------------------------------------------------");
            Debug.Log("Player State: " + state);
            Debug.Log("canSwing: " + canSwing);
            Debug.Log("-----------------------------------------------------");
        }
        // //Debug.Log("State: " + state);
        if (CloudDistanceList.Count > 0 && CloudDistanceList.Keys.First() <= this.transform.position.y)
        {
            foreach (var pair in CloudDistanceList[CloudDistanceList.Keys.First()])
            {
                cloudDistanceMap.SetTile(pair.Item1, pair.Item2);
            }
            CloudDistanceList.Remove(CloudDistanceList.Keys.First());
        }

        switch (state)
        {
            case State.Grounded:
                HandleGrounded();
                break;
            case State.Airborne:
                //Debug.Log("===================================================");
                HandleAirborne();
                break;
            case State.Attached:
                //Debug.Log("===================================================");
                HandleAttached();
                break;
            case State.Swinging:
                //Debug.Log("===================================================");
                HandleSwinging();
                UpdateRopeRender();
                break;
            default:
                break;
        }
    }

    void FixedUpdate()
    {
        if(rb.velocity.x>0)
        {
            animator.SetBool("right", true);
            GetComponent<SpriteRenderer>().flipX=false;
        } else
        {
            animator.SetBool("right", false);
            GetComponent<SpriteRenderer>().flipX = true;
        }

        switch (state)
        {
            case State.Grounded:
                animator.SetBool("jump", false);
                break; 
            case State.Airborne:
                animator.SetBool("jump", true);
                HandleAirbornePhysics();
                break;
            case State.Attached:
                animator.SetBool("jump", true);
                HandleAttachedPhysics();
                break;
            case State.Swinging:
                animator.SetBool("jump", true);
                HandleSwingingPhysics();
                break;
            default:
                break;
        }
        if (rb.velocity.magnitude > 20f)
        {
            rb.velocity = rb.velocity.normalized * 20f;
        }
    }

    void HandleGrounded()
    {
        ropeLine.enabled = false;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Camera.main.GetComponent<AudioSource>().PlayOneShot(jumpSound);
            jumpFixedFrames = MAX_JUMP_FRAMES;
            rb.AddForce(new Vector2(0, 500));
            state = State.Airborne;
        }
    }
  
    void HandleAirborne()
    {
        rb.gravityScale = gravity;
        Vector2 ourPos = new Vector2(this.transform.position.x, this.transform.position.y);

        if (Input.GetMouseButtonDown(0) && canSwing)
        {
            Debug.Log("Attempted swing");
            canSwing = false;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Get the unit vector towards the mouse
            Vector2 unitVector = (mousePos - ourPos).normalized;
            // Raycast to first platform hit
            RaycastHit2D hit = Physics2D.Raycast(ourPos, unitVector, GRAPPLE_RANGE, LayerMask.GetMask("Hookables"));

            if (hit && (hit.collider.CompareTag("Hookable") || hit.collider.CompareTag("Cloud") || hit.collider.CompareTag("CloudDistance")))
            {
                // Get the hit coordinate

                Vector2 swingPoint = hit.point;

                swingedObject = hit.collider.gameObject;

                Camera.main.GetComponent<AudioSource>().PlayOneShot(grappleSound);
                rope.anchorPoint = new Vector2(swingPoint.x, swingPoint.y);
                rope.length = Vector2.Distance(ourPos, rope.anchorPoint);

                state = State.Attached;

                // Draw line from player to anchor
                ropeLine.enabled = true;
            }
            else
            {
                Camera.main.GetComponent<AudioSource>().PlayOneShot(whiffSound);
                StartCoroutine(DelaySwing(DELAY_NORMAL));
            }
        }
    }
    
    void HandleAirbornePhysics()
    {
        if (Input.GetKey(KeyCode.Space) && jumpFixedFrames > 0)
        {
            rb.AddForce(new Vector2(0, jumpFixedFrames * 2.5f));
            jumpFixedFrames--;
        } else if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpFixedFrames = 0;
        }
    }

    void HandleAttached()
    {
        if (Input.GetMouseButtonUp(0))
        {
            ropeLine.enabled = false;
            StartCoroutine(DelaySwing(DELAY_NORMAL));
            state = State.Airborne;
        }
    }

    void HandleAttachedPhysics()
    {
        UpdateRopeRender();
        // if rope is taut, go into swinging
        if (rope.IsTaut())
        {
            rb.gravityScale = 0;
            spinVelocity = rb.velocity;

            // set initial velocity to be tangent to circle
            Vector2 vec1 = new(-spinVelocity.x, -spinVelocity.y); // inverse of initial velocity
            Vector2 vec2 = rope.NormalizedPlayerToAnchor(); // rope vector
            float rotation = Vector2.SignedAngle(vec1, vec2);
            if (rotation > 0)
            {
                rotation += 90;
            } else
            {
                rotation -= 90;
            }
            vec1 = Quaternion.Euler(0, 0, rotation) * vec1;
            rb.velocity = vec1;

            // record initial y pos and reset flag / flag switches
            RevolutionData.threshold = this.transform.position.y;
            RevolutionData.positionRelativeToThreshold = 0;
            RevolutionData.positionSwitchCount = 0;

            state = State.Swinging;
        }
    }

    private void RevolutionCheck()
    {
        // revolution check: if we cross the threshold twice, we completed
        // a full revolution
        if (this.transform.position.y > RevolutionData.threshold)
        {
            if (RevolutionData.positionRelativeToThreshold >= 0)
            {
                RevolutionData.positionRelativeToThreshold = 1;
            } else // crossThreshold is -1
            {
                RevolutionData.positionRelativeToThreshold = 1;
                RevolutionData.positionSwitchCount++;
            }
        } else // position is less than threshold
        {
            if (RevolutionData.positionRelativeToThreshold <= 0)
            {
                RevolutionData.positionRelativeToThreshold = -1;
            } else // position is 1
            {
                RevolutionData.positionRelativeToThreshold = -1;
                RevolutionData.positionSwitchCount++;
            }
        }
    }

    void HandleSwinging()
    {
        // set player's rotation
        Vector2 ropeVec = rope.NormalizedPlayerToAnchor();
        float zRotation = Vector2.SignedAngle(Vector2.up, ropeVec);
        // this.transform.rotation = Quaternion.Euler(0, 0, zRotation);

        RevolutionCheck();

        if (RevolutionData.positionSwitchCount == 2) // we crossed the y threshold twice, so stop swinging
        {
            ropeLine.enabled = false;
            StartCoroutine(DelaySwing(DELAY_NORMAL));
            state = State.Airborne;
            rb.gravityScale = gravity;
            return;
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            ropeLine.enabled = false;
            StartCoroutine(DelaySwing(DELAY_NORMAL));
            state = State.Airborne;
            rb.gravityScale = gravity;
        }
    }

    void HandleSwingingPhysics()
    {
        if (rb.velocity.magnitude < 5f)
        {
            rb.velocity = rb.velocity.normalized * 5f;
        }
        if (rb.velocity.magnitude * 1.035f < 20f)
        {
            rb.velocity *= 1.035f;
        }
        double forceMagnitude = rb.mass * Vector2.SqrMagnitude(rb.velocity) / rope.length;
        Vector2 force = rope.NormalizedPlayerToAnchor();
        force = new Vector2(force.x * (float) forceMagnitude, force.y * (float) forceMagnitude);
        rb.AddForce(force, ForceMode2D.Force);
    }

    private Vector2 GetHandPosition()
    {
        Quaternion rotation = this.transform.rotation;
        if (rb.velocity.x > 0)
        {
            rotation *= Quaternion.Euler(Vector3.forward * -90);
        } else
        {
            rotation *= Quaternion.Euler(Vector3.forward * 90);
        }
        Vector3 direction = Vector3.up;
        direction = rotation * direction;
        direction = 0.5f * direction.normalized;
        return this.transform.position + direction;
    }
    IEnumerator DelaySwing(float delay)
    {
        delayingSwing = true;

        List<Tuple<Vector3Int, TileBase>> cloudTiles = new();
        List<Tuple<Vector3Int, TileBase>> cloudDistanceTiles = new();

        // If we hook onto a Cloud
        if (cloudMap.GetTile(cloudMap.WorldToCell(rope.anchorPoint)) != null)
        {
            Vector3Int cloudPos = cloudMap.WorldToCell(rope.anchorPoint);

            
            HashSet<Tuple<int, int>> visited = new();
            RemoveCloud(cloudTiles, cloudPos, visited, cloudMap);
        }

        // If we hook onto a CloudDistance
        if (cloudDistanceMap.GetTile(cloudDistanceMap.WorldToCell(rope.anchorPoint)) != null)
        {
            Vector3Int cloudPos = cloudDistanceMap.WorldToCell(rope.anchorPoint);

            
            HashSet<Tuple<int, int>> visited = new();
            RemoveCloud(cloudDistanceTiles, cloudPos, visited, cloudDistanceMap);

            CloudDistanceList.Add(rope.anchorPoint.y + 30, cloudDistanceTiles);
        }
       
        rope.anchorPoint = new();
        
        this.GetComponent<SpriteRenderer>().color = Color.red;
        yield return new WaitForSeconds(delay);
        foreach (var pair in cloudTiles)
        {
            cloudMap.SetTile(pair.Item1, pair.Item2);
        }

        this.GetComponent<SpriteRenderer>().color = Color.white;
        canSwing = true;
        delayingSwing = false;

    }

    void RemoveCloud(List<Tuple<Vector3Int, TileBase>> cloudTiles, Vector3Int position, HashSet<Tuple<int, int>> visited, Tilemap map)
    {
        if (map.GetTile(position) == null || visited.Contains(Tuple.Create(position.x, position.y)))
        {
            return;
        }
        TileBase cloudTile = map.GetTile(position);
        cloudTiles.Add(Tuple.Create(position, cloudTile));
        map.SetTile(position, null);
        visited.Add(Tuple.Create(position.x, position.y));
        RemoveCloud(cloudTiles, new Vector3Int(position.x - 1, position.y), visited, map);
        RemoveCloud(cloudTiles, new Vector3Int(position.x + 1, position.y), visited, map);
        RemoveCloud(cloudTiles, new Vector3Int(position.x, position.y - 1), visited, map);
        RemoveCloud(cloudTiles, new Vector3Int(position.x, position.y + 1), visited, map);
    }


    private void UpdateRopeRender()
    {
        Vector2 ourPos = GetHandPosition();
        List<Vector3> pos = new();
        pos.Add(new Vector3(rope.anchorPoint.x, rope.anchorPoint.y));
        pos.Add(new Vector3(ourPos.x, ourPos.y));
        ropeLine.startWidth = 0.2f;
        ropeLine.endWidth = 0.2f;
        ropeLine.SetPositions(pos.ToArray());
        ropeLine.useWorldSpace = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        /*
        Debug.Log("OnCollisionEnter2D state: " + state);

        Debug.Log("Wall Collision: " + IsLeftCollision(collision));
        Debug.Log("Ceiling Collision: " + IsCeilingCollision(collision));
        Debug.Log("Floor Collision: " + IsFloorCollision(collision));
        Debug.Log("canSwing: " + canSwing);
        */
        if (state == State.Swinging)
        {
            StartCoroutine(DelaySwing(DELAY_SWING));
            if ((IsLeftCollision(collision) && rb.velocity.x < 0) || (IsRightCollision(collision) && rb.velocity.x > 0))
            {
                rb.velocity = new Vector2(collision.relativeVelocity.x / 2, rb.velocity.y);
            }
        }
        else if (state == State.Attached)
        {
            if ((IsLeftCollision(collision) && rb.velocity.x < 0) || (IsRightCollision(collision) && rb.velocity.x > 0))
            {
                rb.velocity = new Vector2(collision.relativeVelocity.x/2, rb.velocity.y);
                return;
            } else if (IsFloorCollision(collision))
            {
                StartCoroutine(DelaySwing(DELAY_SWING));
            }
        }
        else if (state == State.Airborne)
        {
            if ((IsLeftCollision(collision) && rb.velocity.x < 0) || (IsRightCollision(collision) && rb.velocity.x > 0))
            {
                rb.velocity = new Vector2(collision.relativeVelocity.x/2, rb.velocity.y);
                Debug.Log("backward");
            }
            // TODO: Reevaluate if we want to have no delay for ceiling collision.
            if (!IsCeilingCollision(collision) && !IsFloorCollision(collision))
            {
                /*
                Debug.Log("OnCollisionEnter2D IsCeilingCollision: " + IsCeilingCollision(collision));
                Debug.Log("OnCollisionEnter2D IsFloorCollision: " + IsFloorCollision(collision));
                */
                StartCoroutine(DelaySwing(DELAY_NORMAL));
            }
            if (IsCeilingCollision(collision) )
            {
                jumpFixedFrames = 0;
                if (delayingSwing == false)
                {
                    canSwing = true;
                }
            }

        }
        ropeLine.enabled = false;
        state = State.Airborne;
        rb.gravityScale = gravity;
    }

    private void OnCollisionStay2D(Collision2D collision) {
        if (Input.GetKeyUp(KeyCode.F))
        {
            Debug.Log("-----------------------------------------------------");
            Debug.Log("IsFloorCollision: " + IsFloorCollision(collision) );
            Debug.Log("IsCeilingCollision: " + IsCeilingCollision(collision));
            Debug.Log("-----------------------------------------------------");

        }
        if (state == State.Swinging)
        {
            state = State.Airborne;
            StartCoroutine(DelaySwing(DELAY_NORMAL));
        }
        if (IsFloorCollision(collision))
        {
            floorCollision = true;
        }
        if (IsLeftCollision(collision))
        {
            wallCollision = (true, true);
        } else if (IsRightCollision(collision))
        {
            wallCollision = (true, false);
        }
        if (floorCollision)
        {
            if (wallCollision.Item1 && wallCollision.Item2)
            {
                this.transform.position += new Vector3(0.01f, 0, 0);
            }
            else if (wallCollision.Item1 && !wallCollision.Item2)
            {
                this.transform.position -= new Vector3(0.01f, 0, 0);
            }
            state = State.Grounded;
        }

        //Debug.Log("Collision: " + collision.collider.name);
        //Debug.Log("wallCollision: " + wallCollision);
        //Debug.Log("floorCollision: " + floorCollision);
        foreach (var collisionPoint in collision.contacts)
        {
            //Debug.Log("point: " +  collisionPoint.point);
        }
        //Debug.Log("----------------------------------------------");
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        switch (state) 
        {
            case State.Attached:
                break;
            case State.Grounded:
                state = State.Airborne;
                break;
            case State.Airborne:
                state = State.Airborne;
                break;
            case State.Swinging:
                state = State.Airborne;
                break;
            default:
                Debug.LogError("OnCollisionExit2D broke");
                break;
        }
    }

    

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("OnTriggerEnter2d called.");
        Debug.Log("collision tag: " + collision.transform.tag);
        if (collision.transform.CompareTag("FinishPlatform"))
        {
            // Victory screen to pop up
            winScreen.SetActive(true);

            // A button to move to the next scene
        }
    }

    private bool IsFloorCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);
        float colliderHeight = this.GetComponent<BoxCollider2D>().size.y;
        float deltaHeight = colliderHeight * 0.025f;
        float playerY = this.transform.position.y;

        List<ContactPoint2D> possibleFloorPoints = new();
        foreach (ContactPoint2D contactPoint in points)
        {
            if (contactPoint.point.y < playerY - (0.98f * colliderHeight / 2))
            {
                possibleFloorPoints.Add(contactPoint); 
            }
        }

        if (possibleFloorPoints.Count < 2)
        {
            return false;
        }
        float maxX = float.NegativeInfinity;
        float minX = float.PositiveInfinity;
        foreach (ContactPoint2D point in possibleFloorPoints)
        {
            if (point.point.x > maxX)
            {
                maxX = point.point.x;
            } 
            if (point.point.x < minX)
            {
                minX = point.point.x;
            }
        }
        return (maxX - minX > deltaHeight);
    }


    private bool IsLeftCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);
        float colliderWidth = this.GetComponent<BoxCollider2D>().size.x;
        float deltaWidth = colliderWidth * 0.025f;
        float playerX = this.transform.position.x;

        List<ContactPoint2D> possibleWallPoints = new();
        foreach (ContactPoint2D contactPoint in points)
        {
            if (contactPoint.point.x < playerX - (0.98f * colliderWidth / 2))
            {
                possibleWallPoints.Add(contactPoint);
            }
        }

        if (possibleWallPoints.Count < 2)
        {
            return false;
        }
        float maxY = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        foreach (ContactPoint2D point in possibleWallPoints)
        {
            if (point.point.y > maxY)
            {
                maxY = point.point.y;
            }
            if (point.point.y < minY)
            {
                minY = point.point.y;
            }
        }
        return (maxY - minY > deltaWidth);
    }

    private bool IsRightCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);
        float colliderWidth = this.GetComponent<BoxCollider2D>().size.x;
        float deltaWidth = colliderWidth * 0.025f;
        float playerX = this.transform.position.x;

        List<ContactPoint2D> possibleWallPoints = new();
        foreach (ContactPoint2D contactPoint in points)
        {
            if (contactPoint.point.x > playerX + (0.98f * colliderWidth / 2))
            {
                possibleWallPoints.Add(contactPoint);
            }
        }

        if (possibleWallPoints.Count < 2)
        {
            return false;
        }
        float maxY = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        foreach (ContactPoint2D point in possibleWallPoints)
        {
            if (point.point.y > maxY)
            {
                maxY = point.point.y;
            }
            if (point.point.y < minY)
            {
                minY = point.point.y;
            }
        }
        return (maxY - minY > deltaWidth);
    }

    private bool IsCeilingCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);
        float colliderHeight = this.GetComponent<BoxCollider2D>().size.y;
        float deltaHeight = colliderHeight * 0.025f;
        float playerY = this.transform.position.y;

        List<ContactPoint2D> possibleCeilingPoints = new();
        foreach (ContactPoint2D contactPoint in points)
        {
            if (contactPoint.point.y > playerY + (0.98f * colliderHeight / 2))
            {
                possibleCeilingPoints.Add(contactPoint); 
            }
        }

        if (possibleCeilingPoints.Count < 2)
        {
            return false;
        }
        float maxX = float.NegativeInfinity;
        float minX = float.PositiveInfinity;
        foreach (ContactPoint2D point in possibleCeilingPoints)
        {
            if (point.point.x > maxX)
            {
                maxX = point.point.x;
            } 
            if (point.point.x < minX)
            {
                minX = point.point.x;
            }
        }
        return (maxX - minX > deltaHeight);
    }
}


