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

    public static readonly float GRAPPLE_RANGE = 6;
    public static readonly float DELAY_NORMAL = 0.4f;
    public static readonly float DELAY_SWING = 0.7f;
    public static readonly int MAX_JUMP_FRAMES = 23;

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
    }

    // Update is called once per frame
    void Update()
    {
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
                HandleAirborne();
                break;
            case State.Attached:
                HandleAttached();
                break;
            case State.Swinging:
                HandleSwinging();
                UpdateRopeRender();
                break;
            default:
                break;
        }

        //Debug.Log("CanSwing "+canSwing);
        //Debug.Log("State "+ state);
    }

    void FixedUpdate()
    {
        
        switch (state)
        {
            case State.Grounded:
                break; 
            case State.Airborne:
                HandleAirbornePhysics();
                break;
            case State.Attached:
                HandleAttachedPhysics();
                break;
            case State.Swinging:
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
            canSwing = false;
            Vector2 mouse_position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mouse_position);

            if (hit && (hit.CompareTag("Hookable") || hit.CompareTag("Cloud") || hit.CompareTag("CloudDistance")) && Vector2.Distance(mouse_position, ourPos) <= GRAPPLE_RANGE)
            {
                Camera.main.GetComponent<AudioSource>().PlayOneShot(grappleSound);
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                rope.anchorPoint = new Vector2(mousePos.x, mousePos.y);
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
            Debug.Log("gravity off");
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

        List<Tuple<Vector3Int, TileBase>> cloudTiles = new();
        List<Tuple<Vector3Int, TileBase>> cloudDistanceTiles = new();

        // If we hook onto a Cloud
        if (cloudMap.GetTile(cloudMap.WorldToCell(rope.anchorPoint)) != null)
        {
            Vector3Int cloudPos = cloudMap.WorldToCell(rope.anchorPoint);
            Debug.Log("cloudPos:" + cloudPos);

            
            HashSet<Tuple<int, int>> visited = new();
            RemoveCloud(cloudTiles, cloudPos, visited, cloudMap);
        }

        // If we hook onto a CloudDistance
        if (cloudDistanceMap.GetTile(cloudDistanceMap.WorldToCell(rope.anchorPoint)) != null)
        {
            Vector3Int cloudPos = cloudDistanceMap.WorldToCell(rope.anchorPoint);
            Debug.Log("cloudPos:" + cloudPos);

            
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
        Debug.Log(IsWallCollision(collision));
        ropeLine.enabled = false;
        if (state == State.Swinging)
        {
            StartCoroutine(DelaySwing(DELAY_SWING));
            if (collision.collider.transform.CompareTag("Wall"))
            {
                Debug.Log("why");
                rb.velocity = new Vector2(collision.relativeVelocity.x / 2, rb.velocity.y);
            }
        }
        else if (state == State.Attached)
        {
            StartCoroutine(DelaySwing(DELAY_NORMAL));
            if (collision.collider.transform.CompareTag("Wall"))
            {
                rb.velocity = new Vector2(collision.relativeVelocity.x/2, rb.velocity.y);
            }
        }
        else if (state == State.Airborne)
        {
            TilemapCollider2D tilemapCollider = (TilemapCollider2D)collision.collider;
            //tilemapCollider
            if (collision.collider.transform.CompareTag("Wall"))
            {
                rb.velocity = new Vector2(collision.relativeVelocity.x/2, rb.velocity.y);

            }
            StartCoroutine(DelaySwing(DELAY_NORMAL));
        }
        state = State.Airborne;
        rb.gravityScale = gravity;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsFloorCollision(collision) && IsWallCollision(collision).Item1)
        {
            Debug.Log("wall cooll");
            if (IsWallCollision(collision).Item2)
            {
                this.transform.position += new Vector3(0.01f, 0, 0);
            } else
            {
                this.transform.position -= new Vector3(0.01f, 0, 0);
            }
            state = State.Grounded;
        } else if (IsFloorCollision(collision))
        {
            state = State.Grounded;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        state = State.Airborne;
    }

    private bool IsFloorCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);
        float colliderWidth = this.GetComponent<BoxCollider2D>().size.x;
        float hypotenuse = Mathf.Sqrt(colliderWidth * colliderWidth / 2);
        foreach (ContactPoint2D contactPoint in points)
        {
            if (Vector2.Distance(this.transform.position, contactPoint.point) > hypotenuse * 0.9f)
            {
                continue;
            }
            float colliderHeight = this.GetComponent<BoxCollider2D>().size.y;
            float colliderPos = (this.transform.position.y - (colliderHeight / 2.0f));
            float collisionPos = contactPoint.point.y;
            if (collisionPos < colliderPos) return true;
        }
        return false;
    }

    // returns: if is wall collision, and if is left side collision
    private (bool, bool) IsWallCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);
        float colliderWidth = this.GetComponent<BoxCollider2D>().size.x;
        float hypotenuse = Mathf.Sqrt(colliderWidth * colliderWidth / 2);
        foreach (ContactPoint2D contactPoint in points)
        {
            if (Vector2.Distance(this.transform.position, contactPoint.point) > hypotenuse * 0.9f)
            {
                continue;
            }
            float leftColliderPoint = this.transform.position.x - (colliderWidth / 2.0f);
            float rightColliderPoint = this.transform.position.x + (colliderWidth / 2.0f);
            float collisionPos = contactPoint.point.x;
            if (collisionPos < leftColliderPoint) return (true, true);
            if (collisionPos > rightColliderPoint) return (true, false);
        }
        return (false, false);
}

    private bool IsCeilingCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);

        float colliderWidth = this.GetComponent<BoxCollider2D>().size.x;
        float hypotenuse = Mathf.Sqrt(colliderWidth * colliderWidth / 2);
        foreach (ContactPoint2D contactPoint in points)
        {
            if (Vector2.Distance(this.transform.position, contactPoint.point) > hypotenuse * 0.9f)
            {
                continue;
            }
            float colliderHeight = this.GetComponent<BoxCollider2D>().size.y;
            float colliderPos = (this.transform.position.y + (colliderHeight / 2.0f));
            float collisionPos = contactPoint.point.y;
            if (collisionPos > colliderPos) return true;
        }
        return false;
    }
}
