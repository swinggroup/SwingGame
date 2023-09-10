using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using UnityEngine.WSA;

public class PlayerController : MonoBehaviour
{
    // Debug vars
    int updateCount = 0;
    int collisionEnterCount = 0;
    int collisionStayCount = 0;
    int collisionExitCount = 0;
    // Debug vars^

    public bool debugOn;
    public Animator animator;

    public static readonly float GRAPPLE_RANGE = 9;
    public static readonly float DELAY_NORMAL = 0.4f;
    public static readonly float DELAY_SWING = 0.6f;
    public static readonly int MAX_JUMP_FRAMES = 23;

    GameObject swingedObject;

    public AudioClip grappleSound;
    public AudioClip whiffSound;
    public AudioClip jumpSound;
    public AudioClip thudSound;

    int jumpFixedFrames;
    // bool delaying; on hold for now
    Rigidbody2D rb;
    public Rope rope;
    public State state;
    LineRenderer ropeLine;
    Vector2 spinVelocity;

    public bool ropeShow;

    private float gravity = 6f;
    private float terminalVelocity = 27f;
    private float accelFactor = 0.2f; 
    bool canSwing = true;
    bool isStunned = false;
    public Tilemap cloudMap;
    public Tilemap cloudDistanceMap;
    public Tilemap wallMap;
    SortedDictionary<float, List<Tuple<Vector3Int, TileBase>>> CloudDistanceList;

    public bool leftCollision = false;
    public bool rightCollision = false;
    public bool ceilingCollision = false;
    public bool floorCollision = false;
    public bool delayingSwing = false;
    public bool delayingJumpAnimation = false;

    public GameObject winScreen;

    public GameObject screenDebug;

    private Vector2 spawnZone;


    private class RevolutionData
    {
        public static float threshold;
        public static int positionRelativeToThreshold;
        public static int positionSwitchCount;
    }

    public class Rope
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
            }
            else
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

    public enum State
    {
        Grounded, Airborne, Attached, Swinging, Stunned
    }

    // Start is called before the first frame update
    void Start()
    {
        spawnZone = this.gameObject.transform.position;
        ropeLine = this.gameObject.AddComponent<LineRenderer>();
        ropeLine.enabled = false;
        rb = this.GetComponent<Rigidbody2D>();
        rope = new(this.gameObject);
        rb.gravityScale = gravity;
        CloudDistanceList = new();
        state = State.Airborne;
        if (debugOn == true)
        {
            screenDebug.SetActive(true);
        }
        else
        {
            screenDebug.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        // Update debug on screen
        TextMeshProUGUI debugLogs = screenDebug.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name == "State");
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("State: " + state.ToString() + "\n");
        stringBuilder.Append("canSwing: " + canSwing + "\n");
        stringBuilder.Append("Timestamp : " + Time.time + "\n");
        debugLogs.SetText(stringBuilder.ToString());

        leftCollision = false;
        rightCollision = false;
        ceilingCollision = false;
        floorCollision = false;
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            this.transform.position = spawnZone;
            rb.velocity = new Vector2();
        }

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
            case State.Stunned:
                HandleStunned();
                break;
            default:
                break;
        }


    }

    void FixedUpdate()
    {
        if (rb.velocity.x > 0.1f)
        {
            GetComponent<SpriteRenderer>().flipX = false;
        }
        else if (rb.velocity.x < -0.1f)
        {
            GetComponent<SpriteRenderer>().flipX = true;
        }

        switch (state)
        {
            case State.Grounded:
                animator.SetBool("jump", false);
                animator.SetBool("falling", false);
                if (Mathf.Abs(rb.velocity.x) > 1f)
                {
                    animator.SetBool("rolling", true);
                } else
                {
                    animator.SetBool("rolling", false);
                }
                break;
            case State.Airborne:
                StartCoroutine(DelayJumpAnimation(0.07f));
                HandleAirbornePhysics();
                if (rb.velocity.y < 0)
                {
                    animator.SetBool("falling", true);
                }
                break;
            case State.Attached:
                animator.SetBool("jump", true);
                HandleAttachedPhysics();
                break;
            case State.Swinging:
                animator.SetBool("jump", true);
                animator.SetBool("falling", false);
                HandleSwingingPhysics();
                break;
            case State.Stunned:
                break;
            default:
                break;
        }
        if (rb.velocity.magnitude > terminalVelocity)
        {
            rb.velocity = rb.velocity.normalized * terminalVelocity;
        }
    }

    void HandleGrounded()
    {
        ropeLine.enabled = false;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Camera.main.GetComponent<AudioSource>().PlayOneShot(jumpSound);
            jumpFixedFrames = MAX_JUMP_FRAMES;
            rb.AddForce(new Vector2(0, 2600));
            //rb.AddForce(new Vector2(0, 650));
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
                if (ropeShow == true)
                {
                    ropeLine.enabled = true;
                }
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
            rb.AddForce(new Vector2(0, jumpFixedFrames * 8f));
            jumpFixedFrames--;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
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
            }
            else
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
    
    void HandleStunned()
    {
        
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
            }
            else // crossThreshold is -1
            {
                RevolutionData.positionRelativeToThreshold = 1;
                RevolutionData.positionSwitchCount++;
            }
        }
        else // position is less than threshold
        {
            if (RevolutionData.positionRelativeToThreshold <= 0)
            {
                RevolutionData.positionRelativeToThreshold = -1;
            }
            else // position is 1
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
        if ((rb.velocity + (rb.velocity.normalized * accelFactor)).magnitude < terminalVelocity)
        {
            rb.velocity += rb.velocity.normalized * accelFactor;
        }
        double forceMagnitude = rb.mass * Vector2.SqrMagnitude(rb.velocity) / rope.length;
        Vector2 force = rope.NormalizedPlayerToAnchor();
        force = new Vector2(force.x * (float)forceMagnitude, force.y * (float)forceMagnitude);
        rb.AddForce(force, ForceMode2D.Force);
    }

    private Vector2 GetHandPosition()
    {
        Quaternion rotation = this.transform.rotation;
        if (rb.velocity.x > 0)
        {
            rotation *= Quaternion.Euler(Vector3.forward * -90);
        }
        else
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

    IEnumerator DelayJumpAnimation(float delay)
    {
        if (!delayingJumpAnimation)
        {
            delayingJumpAnimation = true;
            yield return new WaitForSeconds(delay);
            delayingJumpAnimation = false;
            if (state == State.Airborne)
            {
                animator.SetBool("jump", true);
            }
        }
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
        ropeLine.startWidth = 0.05f;
        ropeLine.endWidth = 0.05f;
        ropeLine.SetPositions(pos.ToArray());
        ropeLine.useWorldSpace = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Camera.main.GetComponent<AudioSource>().PlayOneShot(thudSound);
        if (collision.collider.name == "StunMap")
        {
            state = State.Stunned;
            isStunned = true;
            canSwing = false;
        } else if (state == State.Grounded)
        {
            if ((IsLeftCollision(collision) || (IsRightCollision(collision))))
            {
                animator.SetTrigger("bonk");
            }
        }
        else if (state == State.Swinging)
        {
            StartCoroutine(DelaySwing(DELAY_SWING));
            if ((IsLeftCollision(collision) && rb.velocity.x < 0) || (IsRightCollision(collision) && rb.velocity.x > 0))
            {
                rb.velocity = new Vector2(collision.relativeVelocity.x / 2, rb.velocity.y);
            }
            state = State.Airborne;
        }
        else if (state == State.Attached)
        {
            if ((IsLeftCollision(collision) && rb.velocity.x < 0) || (IsRightCollision(collision) && rb.velocity.x > 0))
            {
                rb.velocity = new Vector2(collision.relativeVelocity.x / 2, rb.velocity.y);
                return;
            }
            else if (IsFloorCollision(collision))
            {
                StartCoroutine(DelaySwing(DELAY_SWING));
            }
            state = State.Airborne;
        }
        else if (state == State.Airborne)
        {
            if ((IsLeftCollision(collision) && rb.velocity.x < 0) || (IsRightCollision(collision) && rb.velocity.x > 0))
            {
                rb.velocity = new Vector2(collision.relativeVelocity.x / 2, rb.velocity.y);
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
            if (IsCeilingCollision(collision))
            {
                jumpFixedFrames = 0;
                if (delayingSwing == false)
                {
                    canSwing = true;
                }
            }
            if (IsFloorCollision(collision))
            {
                state = State.Grounded;
            }
        }
        else if (state == State.Stunned)
        {
            if (IsFloorCollision(collision))
            {
                state = State.Grounded;
                isStunned = false;
                canSwing = true;
            }
        }
        TextMeshProUGUI debugLogs = screenDebug.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name == "OnCollisionEnter");
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("OnCollisionEnter\n");
        stringBuilder.Append("State: " + state.ToString() + "\n");
        stringBuilder.Append("canSwing: " + canSwing + "\n");
        stringBuilder.Append("isStunned: " + isStunned + "\n");
        stringBuilder.Append("leftCollision: " + IsLeftCollision(collision) + "\n");
        stringBuilder.Append("rightCollision: " + IsRightCollision(collision) + "\n");
        stringBuilder.Append("ceilingCollision: " + IsCeilingCollision(collision) + "\n");
        stringBuilder.Append("floorCollision: " + IsFloorCollision(collision) + "\n");
        stringBuilder.Append("Timestamp : " + Time.time + "\n");
        debugLogs.SetText(stringBuilder.ToString());
        ropeLine.enabled = false;
        
        rb.gravityScale = gravity;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        switch (state)
        {
            case State.Grounded:
                break;
            case State.Airborne:
                break;
            case State.Attached:
                //state = State.Airborne;
                //StartCoroutine(DelaySwing(DELAY_NORMAL));
                break;
            case State.Swinging:
                state = State.Airborne;
                StartCoroutine(DelaySwing(DELAY_NORMAL));
                break;
            case State.Stunned:
                break;
            default:
                Debug.LogError("broke oncollisionstay");
                break;
        }
        if (IsCeilingCollision(collision))
        {
            ceilingCollision = true;
        }
        if (IsFloorCollision(collision))
        {
            floorCollision = true;
        }
        if (IsLeftCollision(collision))
        {
            leftCollision = true;
        }
        if (IsRightCollision(collision))
        {
            rightCollision = true;
        }
        if (state != State.Stunned)
        {
            if ((leftCollision && rightCollision) || floorCollision)
            {
                state = State.Grounded;
            }
            else
            {
                state = State.Airborne;
            }
        }
        if (leftCollision)
        {
            this.transform.position += new Vector3(0.01f, 0, 0);
        }
        else if (rightCollision)
        {
            this.transform.position -= new Vector3(0.01f, 0, 0);
        }

        TextMeshProUGUI debugLogs = screenDebug.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name == "OnCollisionStay");
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("OnCollisionStay\n");
        stringBuilder.Append("State: " + state.ToString() + "\n");
        stringBuilder.Append("canSwing: " + canSwing + "\n");
        stringBuilder.Append("isStunned: " + isStunned + "\n");
        stringBuilder.Append("leftCollision: " + leftCollision + "\n");
        stringBuilder.Append("rightCollision: " + rightCollision + "\n");
        stringBuilder.Append("ceilingCollision: " + ceilingCollision + "\n");
        stringBuilder.Append("floorCollision: " + floorCollision + "\n");
        stringBuilder.Append("Timestamp : " + Time.time + "\n");
        debugLogs.SetText(stringBuilder.ToString());

    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        switch (state)
        {
            case State.Grounded:
                state = State.Airborne;
                break;
            case State.Airborne:
                state = State.Airborne;
                break;
            case State.Attached:
                break;
            case State.Swinging:
                break;
            case State.Stunned:
                break;
            default:
                Debug.LogError("OnCollisionExit2D broke");
                break;
        }
        TextMeshProUGUI debugLogs = screenDebug.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name == "OnCollisionExit");
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("OnCollisionExit\n");
        stringBuilder.Append("State: " + state.ToString() + "\n");
        stringBuilder.Append("canSwing: " + canSwing + "\n");
        stringBuilder.Append("isStunned: " + isStunned + "\n");
        stringBuilder.Append("leftCollision: " + IsLeftCollision(collision) + "\n");
        stringBuilder.Append("rightCollision: " + IsRightCollision(collision) + "\n");
        stringBuilder.Append("ceilingCollision: " + IsCeilingCollision(collision) + "\n");
        stringBuilder.Append("floorCollision: " + IsFloorCollision(collision) + "\n");
        stringBuilder.Append("Timestamp : " + Time.time + "\n");
        debugLogs.SetText(stringBuilder.ToString());
    }



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("FinishPlatform"))
        {
            // Victory screen to pop up
            winScreen.SetActive(true);

            // A button to move to the next scene
        }
    }

    private bool IsLeftCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);

        foreach (ContactPoint2D contactPoint in points)
        {
            if (Vector2.Dot(contactPoint.normal, Vector2.right) > .707f)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsRightCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);

        foreach (ContactPoint2D contactPoint in points)
        {
            if (Vector2.Dot(contactPoint.normal, Vector2.left) > .707f)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsFloorCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);

        foreach (ContactPoint2D contactPoint in points)
        {
            if (Vector2.Dot(contactPoint.normal, Vector2.up) > .707f)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsCeilingCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);

        foreach (ContactPoint2D contactPoint in points)
        {
            if (Vector2.Dot(contactPoint.normal, Vector2.down) > .707f)
            {
                return true;
            }
        }
        return false;
    }

}


