using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    /******************************************************************************************
     * Debug Variables
     *******************************************************************************************/
    private Vector2 spawnZone;
    public bool debugOn;
    public GameObject screenDebug;

    /******************************************************************************************
     * Assigned via Inspector: animation, sfx, tilemaps, rope
     *******************************************************************************************/
    public Animator animator;
    public AudioClip grappleSound;
    public AudioClip whiffSound;
    public AudioClip jumpSound;
    public AudioClip thudSound;
    public Tilemap cloudMap;
    public Tilemap unhookableMap;
    public Tilemap BoostMap;
    public Tilemap cloudDistanceMap;
    public PhysicsMaterial2D noFrictionMaterial;
    SortedDictionary<float, List<Tuple<Vector3Int, TileBase>>> CloudDistanceList;

    public Rope rope;

    /******************************************************************************************
     * Constants 
     *******************************************************************************************/
    private readonly float gravity = 6f;
    private readonly float terminalVelocity = 27f;
    private readonly float accelFactor = 0.2f;
    private readonly float arrowKeyVelocityMagnitude = 200f;
    private readonly Vector3 fortyFiveLeft = new Vector3(-1, -1, 0).normalized;
    private readonly Vector3 fortyFiveRight = new Vector3(1, -1, 0).normalized;
    private readonly Vector3 steepLeft = new Vector3(-1, -2, 0).normalized;
    private readonly Vector3 steepRight = new Vector3(1, -2, 0).normalized;
    private readonly Vector3 shallowLeft = new Vector3(-2, -1, 0).normalized;
    private readonly Vector3 shallowRight = new Vector3(2, -1, 0).normalized;
    public static readonly float GRAPPLE_RANGE = 9;
    public static readonly float DELAY_NORMAL = 0.4f;
    public static readonly float DELAY_SWING = 0.6f;
    public static readonly int MAX_JUMP_FRAMES = 23;

    /******************************************************************************************
     * State / Pseudo-State variables 
     *******************************************************************************************/
    public enum State
    {
        Grounded, Airborne, Attached, Swinging, Stunned
    }
    public State state;
    bool canSwing = true;
    bool isStunned = false;
    bool adjusting = false;
    public bool delayingSwing = false;
    public bool delayingJumpAnimation = false;

    /******************************************************************************************
     * Physics variables
     *******************************************************************************************/
    private class RevolutionData
    {
        public static float threshold;
        public static int positionRelativeToThreshold;
        public static int positionSwitchCount;
    }
    int jumpFixedFrames;
    int boostFixedFrames;
    public Rigidbody2D rb;
    ConstantForce2D currConstantForce; // for boosting
    BoxCollider2D boxCollider;
    Vector2 spinVelocity;
    public bool leftCollision = false;
    public bool rightCollision = false;
    public bool ceilingCollision = false;
    public bool floorCollision = false;
    private Vector3 adjustingDirection;


    // Start is called before the first frame update
    void Start()
    {
        spawnZone = this.gameObject.transform.position;
        rb = this.GetComponent<Rigidbody2D>();
        rb.gravityScale = gravity;
        boxCollider = this.GetComponent<BoxCollider2D>();
        CloudDistanceList = new();
        state = State.Airborne;
        currConstantForce = this.gameObject.GetComponent<ConstantForce2D>();
        if (debugOn == true) screenDebug.SetActive(true);
        else screenDebug.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            spawnZone = this.transform.position;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            this.transform.position = spawnZone;
            rb.velocity = new Vector2();
            canSwing = true;
            adjusting = false;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            rb.velocity += Input.GetKeyDown(KeyCode.LeftArrow) ? new Vector2(-arrowKeyVelocityMagnitude, 0) : new Vector2(arrowKeyVelocityMagnitude, 0);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            rb.velocity += Input.GetKeyDown(KeyCode.DownArrow) ? new Vector2(0, -arrowKeyVelocityMagnitude) : new Vector2(0, arrowKeyVelocityMagnitude);
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
            default:
                break;
        }
    }
    private void LateUpdate()
    {
        leftCollision = false;
        rightCollision = false;
        ceilingCollision = false;
        floorCollision = false;

        // Update debug on screen
        TextMeshProUGUI debugLogs = screenDebug.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name == "State");
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("State: " + state.ToString() + "\n");
        stringBuilder.Append("canSwing: " + canSwing + "\n");
        stringBuilder.Append("Timestamp : " + Time.time + "\n");
        stringBuilder.Append("adjusting : " + adjusting + "\n");
        debugLogs.SetText(stringBuilder.ToString());
    }

    void FixedUpdate()
    {
        if (rb.velocity.magnitude > terminalVelocity)
        {
            rb.velocity = rb.velocity.normalized * terminalVelocity;
        }
        if (adjusting)
        {
            rb.velocity += new Vector2(0, (-Physics.gravity.y * gravity) * Time.fixedDeltaTime);
            if (Vector3.Dot(adjustingDirection, fortyFiveLeft) > 0.99f || Vector3.Dot(adjustingDirection, fortyFiveRight) > 0.99f)
            {
                // 45 degree slope
                rb.velocity = new Vector2(rb.velocity.x, -Math.Abs(rb.velocity.x));
            }
            else if (Vector3.Dot(adjustingDirection, steepLeft) > 0.99f || Vector3.Dot(adjustingDirection, steepRight) > 0.99f)
            {
                // steep slope
                rb.velocity = new Vector2(rb.velocity.x, -2 * Math.Abs(rb.velocity.x));
            }
            else
            {
                // shallow slope
                //rb.velocity = new Vector2(rb.velocity.x, -0.5f * Math.Abs(rb.velocity.x));
                rb.velocity = new Vector2(rb.velocity.x * 0.5f, rb.velocity.y);
                rb.velocity = new Vector2(2 * rb.velocity.x, -Math.Abs(rb.velocity.x));
            }
        }

        if (rb.velocity.x > 0.1f)
        {
            GetComponent<SpriteRenderer>().flipX = false;
        }
        else if (rb.velocity.x < -0.1f)
        {
            GetComponent<SpriteRenderer>().flipX = true;
        }

        // Track how long we keep applying the constant force to boost
        if (boostFixedFrames == 0 || state == State.Swinging)
        {
            currConstantForce.force = (new Vector2(0, 0));
        }
        else
        {
            boostFixedFrames--;
        }

        switch (state)
        {
            case State.Grounded:
                animator.SetBool("jump", false);
                animator.SetBool("falling", false);

                if (Mathf.Abs(rb.velocity.x) > 1f)
                {
                    animator.SetBool("rolling", true);
                    animator.SetBool("bonk", false);
                }
                else
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
                    animator.SetBool("bonk", false);
                }
                break;
            case State.Attached:
                animator.SetBool("jump", true);
                animator.SetBool("bonk", false);
                HandleAttachedPhysics();
                break;
            case State.Swinging:
                animator.SetBool("jump", true);
                animator.SetBool("bonk", false);
                animator.SetBool("falling", false);
                HandleSwingingPhysics();
                break;
            default:
                break;
        }
    }

    void HandleGrounded()
    {
        if (Camera.main.GetComponent<FollowPlayer>().movingCam) return;
        if (Input.GetKeyDown(KeyCode.Space) && !isStunned)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            Camera.main.GetComponent<AudioSource>().PlayOneShot(jumpSound);
            jumpFixedFrames = MAX_JUMP_FRAMES;
            rb.AddForce(new Vector2(0, 2600));
            state = State.Airborne;
            adjusting = false;
        }
    }

    void HandleAirborne()
    {
        rb.gravityScale = gravity;
        Vector2 ourPos = new Vector2(this.transform.position.x, this.transform.position.y);
        if (Input.GetMouseButtonDown(0) && canSwing && !isStunned)
        {
            canSwing = false;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Get the unit vector towards the mouse
            Vector2 unitVector = (mousePos - ourPos).normalized;
            // Raycast to first platform hit
            RaycastHit2D hit = Physics2D.Raycast(ourPos, unitVector, GRAPPLE_RANGE, LayerMask.GetMask("Hookables"));

            if (hit && (!hit.collider.CompareTag("Unhookable")))
            {
                // Get the hit coordinate
                Vector2 swingPoint = hit.point;

                Camera.main.GetComponent<AudioSource>().PlayOneShot(grappleSound);
                rope.NewRope(new Vector2(swingPoint.x, swingPoint.y));
                state = State.Attached;
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
            StartCoroutine(DelaySwing(DELAY_NORMAL));
            state = State.Airborne;
            rope.DeleteRope();
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
            StartCoroutine(DelaySwing(DELAY_NORMAL));
            state = State.Airborne;
            rb.gravityScale = gravity;
            rope.DeleteRope();
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            StartCoroutine(DelaySwing(DELAY_NORMAL));
            state = State.Airborne;
            rb.gravityScale = gravity;
            rope.DeleteRope();
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

        this.GetComponent<SpriteRenderer>().color = delay == DELAY_NORMAL ? Color.red : Color.magenta;
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
                animator.SetBool("bonk", false);
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
        rope.StartPoint.position = rope.anchorPoint;
        rope.EndPoint.position = this.transform.position;
        Vector2 ourPos = GetHandPosition();
        List<Vector3> pos = new();
        pos.Add(new Vector3(rope.anchorPoint.x, rope.anchorPoint.y));
        pos.Add(new Vector3(ourPos.x, ourPos.y));
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

    private void HandleBoosting(Collision2D collision)
    {
        if (state == State.Attached || state == State.Swinging)
        {
            StartCoroutine(DelaySwing(DELAY_SWING));
        }
        state = State.Airborne;

        int boostVelocity = 500;
        boostFixedFrames = 20;
        if (IsFloorCollision(collision))
        {
            currConstantForce.force = new Vector2(0, boostVelocity);
        }
        else if (IsLeftCollision(collision))
        {
            currConstantForce.force = new Vector2(boostVelocity, 0);
        }
        else if (IsRightCollision(collision))
        {
            currConstantForce.force = new Vector2(-boostVelocity, 0);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Camera.main.GetComponent<AudioSource>().PlayOneShot(thudSound);
        if (collision.collider.name == "StunMap")
        {
            isStunned = true;
            canSwing = false;
            if (rope.RopeExists())
            {
                rope.DeleteRope();
            }
        }
        if (collision.collider.name == "BoostMap")
        {
            HandleBoosting(collision);
            rope.DeleteRope();
        }
        if(isStunned)
        {
            if (IsFloorCollision(collision) && collision.collider.name != "StunMap")
            {
                state = State.Grounded;
                isStunned = false;
                canSwing = true;
            }

        }
        switch (state)
        {
            case State.Grounded:
                break;
            case State.Airborne:
                if ((IsLeftCollision(collision) && rb.velocity.x < 0) || (IsRightCollision(collision) && rb.velocity.x > 0))
                {
                    rb.velocity = new Vector2(collision.relativeVelocity.x / 2, rb.velocity.y);
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
                break;
            case State.Attached:
                if ((IsLeftCollision(collision) && rb.velocity.x < 0) || (IsRightCollision(collision) && rb.velocity.x > 0))
                {
                    rb.velocity = new Vector2(collision.relativeVelocity.x / 2, rb.velocity.y);
                    return;
                }
                else if (IsFloorCollision(collision))
                {
                    StartCoroutine(DelaySwing(DELAY_SWING));
                    rope.DeleteRope();
                }
                rope.DeleteRope();
                // Do not transition state to airborne so not taut bounce mechanic still works.

                break;
            case State.Swinging:
                StartCoroutine(DelaySwing(DELAY_SWING));
                // Wall bounce when swinging into wall
                if ((IsLeftCollision(collision) && rb.velocity.x < 0) || (IsRightCollision(collision) && rb.velocity.x > 0))
                {
                    rb.velocity = new Vector2(collision.relativeVelocity.x / 2, rb.velocity.y);
                }
                // Transition state to airborne since OnCollisionStay is not always called to prevent being in Swinging state after rope is deleted.
                state = State.Airborne;
                rope.DeleteRope();
                break;
        }
        DebugGUI("OnCollisionEnter", collision);
        rb.gravityScale = gravity;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        AdjustVelocity();
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
            else if (state != State.Attached)
            {
                state = State.Airborne;
            }
        }
        if (leftCollision && !adjusting)
        {
            this.transform.position += new Vector3(0.01f, 0, 0);
        }
        else if (rightCollision && !adjusting)
        {
            this.transform.position -= new Vector3(0.01f, 0, 0);
        }
        switch (state)
        {
            case State.Grounded:
                // if grounded and hit a wall (should be rolling) we stop and bonk
                if ((leftCollision && rb.velocity.x < 0) || (rightCollision && rb.velocity.x > 0))
                {
                    animator.SetBool("bonk", true);
                    rb.velocity = new Vector3(0, 0, 0);
                }
                break;
            case State.Airborne:
                break;
            case State.Attached:
                break;
            case State.Swinging:
                state = State.Airborne;
                StartCoroutine(DelaySwing(DELAY_NORMAL));
                break;
            default:
                Debug.LogError("broke oncollisionstay");
                break;
        }
        DebugGUI("OnCollisionStay", collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {

        switch (state)
        {
            case State.Grounded:
                AdjustVelocity();
                if (!adjusting)
                {
                    state = State.Airborne;
                }
                break;
            case State.Airborne:
                state = State.Airborne;
                break;
            case State.Attached:
                break;
            case State.Swinging:
                break;
            default:
                Debug.LogError("OnCollisionExit2D broke");
                break;
        }
        DebugGUI("OnCollisionExit", collision);
    }

    private bool IsLeftCollision(Collision2D collision)
    {
        ContactPoint2D[] points = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(points);

        foreach (ContactPoint2D contactPoint in points)
        {
            if (Vector2.Dot(contactPoint.normal, Vector2.right) > 0.98480775301f) // .98f) // .707f)
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
            if (Vector2.Dot(contactPoint.normal, Vector2.left) > 0.98480775301f) // .98f) //.707f)
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
            if (Vector2.Dot(contactPoint.normal, Vector2.up) > .17364817766f) // .707f)
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
            if (Vector2.Dot(contactPoint.normal, Vector2.down) > .17364817766f) // .98f) // .707f)
            {
                return true;
            }
        }
        return false;
    }

    private void AdjustVelocity()
    {
        Vector3 leftRayStart = transform.position - new Vector3(1.1f * boxCollider.size.x / 2, boxCollider.size.y / 2 - 0.2f, 0);
        Vector3 rightRayStart = transform.position + new Vector3(1.1f * boxCollider.size.x / 2, -boxCollider.size.y / 2 + 0.2f, 0);
        RaycastHit2D leftHit = Physics2D.Raycast(leftRayStart, Vector3.down, 0.35f, LayerMask.GetMask("Hookables"));
        RaycastHit2D rightHit = Physics2D.Raycast(rightRayStart, Vector3.down, 0.35f, LayerMask.GetMask("Hookables"));

        Debug.DrawLine(leftRayStart, leftHit.point, Color.green);
        Debug.DrawLine(rightRayStart, rightHit.point, Color.green);
        if (leftHit ^ rightHit)
        {
            RaycastHit2D hit = leftHit ? leftHit : rightHit;
            // If what we hit is horizontal
            if (Vector2.Dot(Vector2.up, hit.normal) > 0.99f)
            {
                adjusting = false;
            }
            if (Vector2.Dot(Vector2.left, hit.normal) > 0.1f)
            {
                adjustingDirection = Quaternion.AngleAxis(90, Vector3.forward) * hit.normal;
            }
            else if (Vector2.Dot(Vector2.left, hit.normal) < -0.1f)
            {
                adjustingDirection = Quaternion.AngleAxis(-90, Vector3.forward) * hit.normal;
            }
            else
            {
                return;
            }
            if (leftHit && rb.velocity.x < 0f || rightHit && rb.velocity.x > 0f)
            {
                adjustingDirection *= -1;
            }
            var adjustedVelocity = adjustingDirection * rb.velocity.magnitude;
            Debug.Log("--------------------\nState: " + state);
            Debug.Log("dist between player and righthit point: " + Vector2.Distance(transform.position, rightHit.point));
            Debug.Log("Adjusted Velocity: " + adjustedVelocity.y.ToString("0.000000000000000000") + "\n------------------\n");
            adjustedVelocity *= 1.01f;
            if (adjustedVelocity.y < -0.0000000000000001f)
            {
                if (!adjusting)
                {
                    boxCollider.sharedMaterial = noFrictionMaterial;
                    adjusting = true;
                    animator.SetBool("rolling", true);
                }
                rb.velocity = adjustedVelocity;
            }
            // For the case where our collider is hanging off the edge, we don't want adjusting to be true.
            else
            {
                boxCollider.sharedMaterial = null;
                adjusting = false;
            }
        }
        else
        {
            boxCollider.sharedMaterial = null;
            adjusting = false;
        }
    }
    bool EqualWithinApproximation(float delta, float a, float b)
    {
        if ((b - delta <= a && a <= b + delta) || (a - delta <= b && b <= a + delta))
        {
            return true;
        }
        return false;
    }

    private void DebugGUI(string name, Collision2D collision)
    {
        TextMeshProUGUI debugLogs = screenDebug.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name == name);
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(name + "\n");
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
}
