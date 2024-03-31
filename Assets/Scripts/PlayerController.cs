using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    /******************************************************************************************
     * Debug Variables
     *******************************************************************************************/
    private Vector2 spawnZone;
    public bool debugOn;
    public GameObject screenDebug;
    private bool flightMode;

    /******************************************************************************************
     * Assigned via Inspector: animation, sfx, tilemaps, rope
     *******************************************************************************************/
    public Animator animator;
    public AudioSource drums;
    public AudioLowPassFilter drumsFilter;
    public bool musicOn;
    public AudioSource theme;
    public AudioLowPassFilter themeLowFilter;
    public AudioHighPassFilter themeHighFilter;
    public AudioClip grappleSound;
    public AudioClip whiffSound;
    public AudioClip jumpSound;
    public AudioClip thudSound;
    public AudioClip whipSound;
    public AudioClip boostSound;
    public AudioClip stunSound;
    public AudioClip breakLineSound;
    public AudioClip backflipSound;
    public AudioClip scrapeSound;
    public AudioClip wavedashSound;
    public Tilemap cloudMap;
    public Tilemap unhookableMap;
    public Tilemap BoostMap;
    public Tilemap cloudDistanceMap;
    public PhysicsMaterial2D noFrictionMaterial;
    SortedDictionary<float, List<Tuple<Vector3Int, TileBase>>> CloudDistanceList;

    public Rope rope;
    public AnchorIndicator anchorIndicator;
    public AudioSource windSource;
    public FairyMove Cursor;

    /******************************************************************************************
     * Constants 
     *******************************************************************************************/
    private readonly float gravity = 6f;
    private readonly float terminalVelocity = 27f;
    private readonly float accelFactor = 0.2f;
    private readonly float wavedashVelocity = 17f;
    private readonly float arrowKeyVelocityMagnitude = 20f;
    public static readonly float GRAPPLE_RANGE = 9;
    public static readonly float DELAY_NORMAL = 0.4f;
    public static readonly float DELAY_SWING = 0.6f;
    public static readonly float CLOUD_DELAY = 0.6f;
    public static readonly int MAX_JUMP_FRAMES = 23;
    public static readonly int MAX_BACKJUMP_FRAMES = 30;
    public static readonly float JUMP_FORCE = 8;
    public static readonly float BACKJUMP_FORCE = 7;
    public static readonly float CURSOR_RADIUS = 3;
    public static readonly float WORLD_TO_SCREEN = 16;
    public static readonly float RELEASE_RADIUS = 2;


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
    bool onSlope = false;
    Coroutine swingDelayCoroutine;
    Coroutine flickTimeCoroutine;
    public bool facingRight = true;
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
    float jumpForce;
    public bool jumpedRecently = false;
    int boostFixedFrames;
    public Rigidbody2D rb;
    ConstantForce2D currConstantForce; // for boosting
    public BoxCollider2D boxCollider;
    Vector2 spinVelocity;
    public bool leftCollision = false;
    public bool rightCollision = false;
    public bool ceilingCollision = false;
    public bool floorCollision = false;
    private Vector3 adjustingDirection;

    /******************************************************************************************
     * Haiku Management
     *******************************************************************************************/

    private Dictionary<string, string[]> haiku;
    public GameObject haikuContent;
    public GameObject haikuScroller;
    private bool showingHaiku;
    public GameObject haikuPrefab;


    // Start is called before the first frame update
    void Start()
    {
        if(musicOn)
        {
            theme.Play();
            drums.Play();
        }
        flightMode = false;
        spawnZone = this.gameObject.transform.position;
        rb = this.GetComponent<Rigidbody2D>();
        rb.gravityScale = gravity;
        boxCollider = this.GetComponent<BoxCollider2D>();
        CloudDistanceList = new();
        state = State.Airborne;
        currConstantForce = this.gameObject.GetComponent<ConstantForce2D>();
        if (debugOn == true) screenDebug.SetActive(true);
        else screenDebug.SetActive(false);
        haiku = new Dictionary<string, string[]>();
        showingHaiku = false;
        haikuScroller.SetActive(false);
    }

    IEnumerator Whip()
    {
        yield return new WaitForSeconds(0.1f);
        rope.DeleteRope();
    }

    IEnumerator JumpedRecently()
    {
        jumpedRecently = true;
        yield return new WaitForSeconds(0.1f);
        jumpedRecently = false;
    }

    void ShowHaiku()
    {

        var list = haiku.Keys.ToList();
        list.Sort();

        haikuPrefab.SetActive(true);

        //refresh
        showingHaiku = true;
        haikuScroller.SetActive(true);

        float mody = Screen.height / 8;
        float modx = Screen.width / 7;

        int i = 0;
        foreach (string key in list)
        {
            
            var pos = new Vector3(haikuPrefab.transform.position.x + (modx * (i / 4)), haikuPrefab.transform.position.y - (i*mody), haikuPrefab.transform.position.z);
            var newHaiku = Instantiate(haikuPrefab, pos, haikuPrefab.transform.rotation);
            TextMeshProUGUI[] textMeshes = newHaiku.GetComponentsInChildren<TextMeshProUGUI>();
            for (int k = 0; k < textMeshes.Length; k++)
            {
                Debug.Log("looking at " + textMeshes[k].name);
                if (textMeshes[k].name == "Title")
                {
                    textMeshes[k].text = key;
                } else if (textMeshes[k].name == "Body")
                {
                    textMeshes[k].text = string.Join("\n", haiku[key]);
                }
            }
            newHaiku.transform.SetParent(haikuContent.transform);
            newHaiku.transform.localScale = haikuPrefab.transform.localScale;
            i++;

            var image = haikuScroller.GetComponentInChildren<Image>();
        }
        haikuPrefab.SetActive(false);

    }

    void HideHaiku()
    {
        //Delete all clones
        foreach (var haiku in GameObject.FindGameObjectsWithTag("haiku"))
        {
            if (haiku.name != "haiku")
            {
                Debug.Log("destroying" + haiku.name);
                Destroy(haiku);
            }
            else
            {
                Debug.Log("not destroying " + haiku.name);
            }
        }
        showingHaiku = false;
        haikuScroller.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        // lock mouse to center radius 
        if (!MouseInPlayerRadius())
        {
            var overExtendedMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            var screenCenterVector = new Vector2(Screen.width/2, Screen.height/2);
            Mouse.current.WarpCursorPosition(screenCenterVector + (CURSOR_RADIUS * WORLD_TO_SCREEN * (overExtendedMousePos - screenCenterVector).normalized));
        } 

        if(Input.GetKeyDown(KeyCode.H))
        {
            if (!showingHaiku)
            {
                PrintHaiku();
                ShowHaiku();
            } else
            {
                HideHaiku();
            }

        }

        Vector3 mousePos = Input.mousePosition;

        if (rb.velocity.magnitude > 15)
        {
            if(!windSource.isPlaying)
            {
                windSource.Play();
            }
            windSource.volume = (rb.velocity.magnitude - 15) / 25f;
        }
        else
        {
            windSource.Stop();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(JumpedRecently());
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            spawnZone = this.transform.position;
        }
        if (mousePos.x < Screen.width / 2.0f && facingRight)
        {
            facingRight = false;
            GetComponent<SpriteRenderer>().flipX = true;
        }
        else if ((mousePos.x > Screen.width / 2.0f) && !facingRight)
        {
            facingRight = true;
            GetComponent<SpriteRenderer>().flipX = false;
        }

        if (Input.GetKeyDown(KeyCode.F)) // toggle flight mode
        {
            flightMode = !flightMode;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            this.transform.position = spawnZone;
            rb.velocity = new Vector2();
            canSwing = true;
            isStunned = false;
            animator.SetBool("stunned", false);
            onSlope = false;
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            ResetSwingDelay();
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

    void LateUpdate()
    {
        // lock mouse to center radius 
        if (!MouseInPlayerRadius())
        {
            var overExtendedMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            var screenCenterVector = new Vector2(Screen.width / 2, Screen.height / 2);
            Mouse.current.WarpCursorPosition(screenCenterVector + ((overExtendedMousePos - screenCenterVector).normalized * CURSOR_RADIUS * WORLD_TO_SCREEN));
        }
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
        stringBuilder.Append("adjusting : " + onSlope + "\n");
        debugLogs.SetText(stringBuilder.ToString());
    }

    public void AddHaiku(string tag, string[] lines)
    {
        haiku.Add(tag, lines);
        if(showingHaiku)
        {
            ShowHaiku();
        }
    }

    public void PrintHaiku()
    {
        for (int i = 0; i< haiku.Count; i++)
        {
            Debug.Log(haiku.ElementAt(i).Key);
            for (int j = 0; j<haiku.ElementAt(i).Value.Length; j++)
            {
                Debug.Log(haiku.ElementAt(i).Value[j]);
            }
        }
    }

    void FixedUpdate()
    {


        if (state.Equals(State.Grounded))
        {
            if (rb.velocity.magnitude < 1)
            {
                drumsFilter.cutoffFrequency = 2000;
                //themeLowFilter.cutoffFrequency = 2000;
                //themeHighFilter.cutoffFrequency = 2000;

            }
        }
        else
        {
            drumsFilter.cutoffFrequency = 22000;
            themeLowFilter.cutoffFrequency = 22000;
            themeHighFilter.cutoffFrequency = 0;
        }
        if (rb.velocity.magnitude > terminalVelocity)
        {
            rb.velocity = rb.velocity.normalized * terminalVelocity;
        }
        /*
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
        */

        // Removing for orientation mechanics
        /*
        if (rb.velocity.x > 0.1f)
        {
            GetComponent<SpriteRenderer>().flipX = false;
        }
        else if (rb.velocity.x < -0.1f)
        {
            GetComponent<SpriteRenderer>().flipX = true;
        }
        */

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

                // If player is flickering between idle and rolling, the condition is too strict
                // and not catching float error when the rigid body is still.
                if ((rb.velocity.x > 1f && facingRight) || (rb.velocity.x < -1f && !facingRight))
                {
                    animator.SetBool("rolling", true);
                    animator.SetBool("backsliding", false);
                    //animator.SetBool("bonk", false);
                }
                else if ((rb.velocity.x > 1f && !facingRight) || (rb.velocity.x < -1f && facingRight))
                {
                    Camera.main.GetComponent<AudioSource>().PlayOneShot(scrapeSound, 2f);
                    animator.SetBool("backsliding", true);
                    animator.SetBool("rolling", false);
                    //animator.SetBool("bonk", false);
                }
                else
                {
                    animator.SetBool("rolling", false);
                    animator.SetBool("backsliding", false);
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
                //animator.SetBool("bonk", false);
                HandleAttachedPhysics();
                break;
            case State.Swinging:
                animator.SetBool("jump", true);
                //animator.SetBool("bonk", false);
                animator.SetBool("falling", false);
                HandleSwingingPhysics();
                break;
            default:
                break;
        }
        rope.playerPhysicsTransform = rb.position + (rb.velocity * Time.fixedDeltaTime);
    }

    void HandleGrounded()
    {
        if (Camera.main.GetComponent<FollowPlayer>().movingCam) return;
        if ((Input.GetKeyDown(KeyCode.Space) || jumpedRecently) && !isStunned)
        {
            // backflip if jumping while sliding backwards
            if ((rb.velocity.x < -1 && facingRight) || (rb.velocity.x > 1 && !facingRight))
            {
                // This function could be called again causing a double jump sound if jumpedRecently is still true.
                jumpedRecently = false;
                rb.velocity = new Vector2(2 * rb.velocity.x, 0);
                jumpFixedFrames = MAX_BACKJUMP_FRAMES;
                jumpForce = BACKJUMP_FORCE;
                Camera.main.GetComponent<AudioSource>().PlayOneShot(backflipSound, 10f);
                rb.AddForce(new Vector2(3 * rb.velocity.x, 2600));
                state = State.Airborne;
                onSlope = false;
            }
            else
            {
                // This function could be called again causing a double jump sound if jumpedRecently is still true.
                jumpedRecently = false;
                rb.velocity = new Vector2(rb.velocity.x, 0);
                jumpFixedFrames = MAX_JUMP_FRAMES;
                jumpForce = JUMP_FORCE;
                Camera.main.GetComponent<AudioSource>().PlayOneShot(jumpSound);
                rb.AddForce(new Vector2(0, 2600));
                state = State.Airborne;
                onSlope = false;
            }
        }
    }

    IEnumerator FlickTime()
    {
        yield return new WaitForSeconds(0.2f);
        flickTimeCoroutine = null;
    }

    void HandleAirborne()
    {
        // Developer mode: flightmode; in the future, just enable gravity every frame
        if (flightMode)
        {
            rb.gravityScale = 0;
        } else
        {
            rb.gravityScale = gravity;
        }

        if (MouseInPlayerRadius())
        {
            if (flickTimeCoroutine != null) StopCoroutine(flickTimeCoroutine);
            flickTimeCoroutine = StartCoroutine(FlickTime());
        } else
        {
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 ourPos = new Vector2(this.transform.position.x, this.transform.position.y);
            if (flickTimeCoroutine != null && Vector2.Distance(mouse, ourPos) > 5 && canSwing && !isStunned)
            {
                canSwing = false;
                Vector2 mousePos = anchorIndicator.transform.position;

                // Get the unit vector towards the anchor indicator
                Vector2 unitVector = (mousePos - ourPos).normalized;
                // Raycast to first platform hit
                RaycastHit2D hit = Physics2D.CircleCast(ourPos, 0.1f, unitVector, PlayerController.GRAPPLE_RANGE + 0.1f, LayerMask.GetMask("Hookables"));

                if (hit && (!hit.collider.CompareTag("Unhookable")))
                {
                    // Get the hit coordinate
                    Vector2 swingPoint = hit.point;

                    Camera.main.GetComponent<AudioSource>().PlayOneShot(grappleSound);
                    // Passing in anchorIndicator point can cause rope swinging on air, stick with swingPoint.
                    rope.NewRope(swingPoint);
                    // Debug.Log("anchor point: " + rope.anchorPoint.ToString("0.000000000000000"));
                    state = State.Attached;
                }
                else
                {
                    // Debug.Log("Raycast range: " + PlayerController.GRAPPLE_RANGE + 0.1f);
                    // Debug.Log("Anchor Indicator range: " + ((Vector2)anchorIndicator.transform.position - ourPos).magnitude);
                    Camera.main.GetComponent<AudioSource>().PlayOneShot(whiffSound);
                    swingDelayCoroutine = StartCoroutine(DelaySwing(DELAY_NORMAL));
                }
               
            }
        }

        /*
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && canSwing && !isStunned)
        {
            canSwing = false;
            Vector2 mousePos = anchorIndicator.transform.position;

            // Get the unit vector towards the anchor indicator
            Vector2 unitVector = (mousePos - ourPos).normalized;
            // Raycast to first platform hit
            RaycastHit2D hit = Physics2D.CircleCast(ourPos, 0.1f, unitVector, Mathf.Min((ourPos - mousePos).magnitude + 0.3f, PlayerController.GRAPPLE_RANGE + 0.1f), LayerMask.GetMask("Hookables"));

            if (hit && (!hit.collider.CompareTag("Unhookable")))
            {
                if (Input.GetMouseButtonDown(1)) // if right click, whip
                {
                    Vector2 swingPoint = hit.point;
                    rope.NewRope(swingPoint); // todo: make it look like a whip
                    StartCoroutine(Whip());
                    Camera.main.GetComponent<AudioSource>().PlayOneShot(whipSound);
                    rb.velocity += 3 * (rb.position - swingPoint);
                    StartCoroutine(HandleClouds());
                    swingDelayCoroutine = StartCoroutine(DelaySwing(DELAY_NORMAL));
                } else // otherwise do normal swing
                {
                    // Get the hit coordinate
                    Vector2 swingPoint = hit.point;

                    Camera.main.GetComponent<AudioSource>().PlayOneShot(grappleSound);
                    // Passing in anchorIndicator point can cause rope swinging on air, stick with swingPoint.
                    rope.NewRope(swingPoint);
                    // Debug.Log("anchor point: " + rope.anchorPoint.ToString("0.000000000000000"));
                    state = State.Attached;
                }
            }
            else
            {
                // Debug.Log("Raycast range: " + PlayerController.GRAPPLE_RANGE + 0.1f);
                // Debug.Log("Anchor Indicator range: " + ((Vector2)anchorIndicator.transform.position - ourPos).magnitude);
                Camera.main.GetComponent<AudioSource>().PlayOneShot(whiffSound);
                swingDelayCoroutine = StartCoroutine(DelaySwing(DELAY_NORMAL));
            }
        }*/
    }

    void HandleAirbornePhysics()
    {
        // Prevent levitating when player gets airborne from a buffered jump.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpFixedFrames = 0;
        }
        if (Input.GetKey(KeyCode.Space) && jumpFixedFrames > 0)
        {
            rb.AddForce(new Vector2(0, jumpFixedFrames * jumpForce));
            jumpFixedFrames--;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpFixedFrames = 0;
        }
    }

    bool MouseInPlayerRadius()
    {
        Vector2 mouse = Cursor.transform.position;
        Vector2 ourPos = new Vector2(this.transform.position.x, this.transform.position.y);
        return Vector2.Distance(mouse, ourPos) <= RELEASE_RADIUS;
    }

    void HandleAttached()
    {
        if (MouseInPlayerRadius()) // (Input.GetMouseButtonUp(0))
        {

            StartCoroutine(HandleClouds());
            swingDelayCoroutine = StartCoroutine(DelaySwing(DELAY_NORMAL));
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
            StartCoroutine(HandleClouds());
            swingDelayCoroutine = StartCoroutine(DelaySwing(DELAY_NORMAL));
            state = State.Airborne;
            rb.gravityScale = gravity;
            Camera.main.GetComponent<AudioSource>().PlayOneShot(breakLineSound, 3f);
            rope.DeleteRope();
            return;
        }

        if (MouseInPlayerRadius()) // (Input.GetMouseButtonUp(0))
        {
            StartCoroutine(HandleClouds());
            swingDelayCoroutine = StartCoroutine(DelaySwing(DELAY_NORMAL));
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
        rope.playerPhysicsTransform = rb.position + (rb.velocity * Time.fixedDeltaTime);
    }

    void RemoveCloudGivenPoints(List<Vector3> worldPos, List<Tuple<Vector3Int, TileBase>> cloudTiles)
    {
        foreach (var pos in worldPos)
        {
            Vector3Int world = cloudMap.WorldToCell(Vector3Int.RoundToInt(pos));

            if (cloudMap.HasTile(world))
            {
                Vector3Int cloudPos = world;

                HashSet<Tuple<int, int>> visited = new();
                RemoveCloud(cloudTiles, cloudPos, visited, cloudMap);
                break;
            }
        }
    }

    IEnumerator HandleClouds()
    {
        List<Tuple<Vector3Int, TileBase>> cloudTiles = new();
        List<Tuple<Vector3Int, TileBase>> cloudDistanceTiles = new();

        float discrepancy = 0.6f;

        List<Vector3> world = new List<Vector3>{
            rope.anchorPoint,
            new Vector3(rope.anchorPoint.x, rope.anchorPoint.y + discrepancy, 0),
            new Vector3(rope.anchorPoint.x, rope.anchorPoint.y - discrepancy, 0),
            new Vector3(rope.anchorPoint.x + discrepancy, rope.anchorPoint.y, 0),
            new Vector3(rope.anchorPoint.x + discrepancy, rope.anchorPoint.y + discrepancy, 0),
            new Vector3(rope.anchorPoint.x + discrepancy, rope.anchorPoint.y - discrepancy, 0),
            new Vector3(rope.anchorPoint.x - discrepancy, rope.anchorPoint.y, 0),
            new Vector3(rope.anchorPoint.x - discrepancy, rope.anchorPoint.y + discrepancy, 0),
            new Vector3(rope.anchorPoint.x - discrepancy, rope.anchorPoint.y - discrepancy, 0)
        };

        // check if we swung on a cloud, if we did, remove the cloud
        RemoveCloudGivenPoints(world, cloudTiles);

        // If we hook onto a CloudDistance
        if (cloudDistanceMap.GetTile(cloudDistanceMap.WorldToCell(rope.anchorPoint)) != null)
        {
            Vector3Int cloudPos = cloudDistanceMap.WorldToCell(rope.anchorPoint);


            HashSet<Tuple<int, int>> visited = new();
            RemoveCloud(cloudDistanceTiles, cloudPos, visited, cloudDistanceMap);

            CloudDistanceList.Add(rope.anchorPoint.y + 30, cloudDistanceTiles);
        }

        // set cloud delay
        yield return new WaitForSeconds(CLOUD_DELAY); 

        // Function to verify that player is not overlapping with the respawning cloud. Delay spawn if so.
        bool playerOverlapping() => !cloudTiles.All(x => Physics2D.OverlapBox((Vector2Int)x.Item1, Vector2.one, 0f, LayerMask.GetMask("Default")).IsUnityNull());

        while (playerOverlapping())
        {
            yield return new WaitForSeconds(1);
        }

        // respawn the cloud
        foreach (var pair in cloudTiles)
        {
            cloudMap.SetTile(pair.Item1, pair.Item2);
        }
    }

    IEnumerator DelaySwing(float delay)
    {
        delayingSwing = true;
        rope.anchorPoint = new();
        this.GetComponent<SpriteRenderer>().color = delay == DELAY_NORMAL ? Color.red : Color.magenta;
        yield return new WaitForSeconds(delay);
        this.GetComponent<SpriteRenderer>().color = Color.white;
        canSwing = true;
        delayingSwing = false;
    }

    public void ResetSwingDelay()
    {
        if (delayingSwing)
        {
            StopCoroutine(swingDelayCoroutine);
            rope.anchorPoint = new();
            this.GetComponent<SpriteRenderer>().color = Color.white;
            canSwing = true;
            delayingSwing = false;
        }
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
                //animator.SetBool("bonk", false);
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
        Camera.main.GetComponent<AudioSource>().PlayOneShot(boostSound, 9f);
        if (state == State.Attached || state == State.Swinging)
        {
            StartCoroutine(HandleClouds());
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
        else if (IsCeilingCollision(collision))
        {
            currConstantForce.force = new Vector2(0, -boostVelocity);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Debug.Log("Player velocity in FixedUpdate: " + fixedUpdatePlayerVelocity.x.ToString("0.00000000000000000000000000000000000"));
        // Debug.Log("Player velocity in OnCollisionEnter: " + rb.velocity.x.ToString("0.00000000000000000000000000000000000"));
        Camera.main.GetComponent<AudioSource>().PlayOneShot(thudSound);
        if (collision.collider.name.Contains("StunMap"))
        {
            Camera.main.GetComponent<AudioSource>().PlayOneShot(stunSound, 7f);
            isStunned = true;
            animator.SetBool("stunned", true);
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
        if (isStunned)
        {
            if (IsFloorCollision(collision) && !collision.collider.name.Contains("StunMap"))
            {
                animator.SetBool("stunned", false);
                state = State.Grounded;
                delayingSwing = false;
                isStunned = false;
                canSwing = true;
            }

        }
        switch (state)
        {
            case State.Grounded:
                // No swing CD if we're grounded, but we need to reset canSwing
                canSwing = true;
                break;
            case State.Airborne:
                if ((IsLeftCollision(collision) && collision.relativeVelocity.x > 0) || (IsRightCollision(collision) && collision.relativeVelocity.x < 0))
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
                    Camera.main.GetComponent<AudioSource>().PlayOneShot(breakLineSound, 3f);
                    rb.velocity = new Vector2(collision.relativeVelocity.x / 2, rb.velocity.y);
                    return;
                }
                else if (IsFloorCollision(collision))
                {
                    // No swing CD if we're grounded
                    rope.DeleteRope();
                } else
                {
                    Camera.main.GetComponent<AudioSource>().PlayOneShot(breakLineSound, 3f);
                }
                rope.DeleteRope();
                // Do not transition state to airborne so not taut bounce mechanic still works.
                break;
            case State.Swinging:

                // No swing CD if we're grounded
                if (!IsFloorCollision(collision))
                {
                    Camera.main.GetComponent<AudioSource>().PlayOneShot(breakLineSound, 3f);
                    StartCoroutine(HandleClouds());
                    StartCoroutine(DelaySwing(DELAY_SWING));
                }
                else // on floor collision while swinging, wavedash
                {
                    Camera.main.GetComponent<AudioSource>().PlayOneShot(wavedashSound, 10f);
                    rb.velocity = new Vector2(facingRight ? wavedashVelocity : -wavedashVelocity, 0);
                }
                // Wall bounce when swinging into wall
                // Debug.Log("player velocity: " + rb.velocity);
                if ((IsLeftCollision(collision) && collision.relativeVelocity.x > 0) || (IsRightCollision(collision) && collision.relativeVelocity.x < 0))
                {
                    // Debug.Log("velocity of collision: " + collision.relativeVelocity);
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
        if (leftCollision && !onSlope)
        {
            this.transform.position += new Vector3(0.01f, 0, 0);
        }
        else if (rightCollision && !onSlope)
        {
            this.transform.position -= new Vector3(0.01f, 0, 0);
        }
        switch (state)
        {
            case State.Grounded:
                // No swing CD if we're grounded, but we need to reset canSwing
                canSwing = true;
                // if grounded and hit a wall (should be rolling) we stop and bonk
                if ((leftCollision && rb.velocity.x < 0) || (rightCollision && rb.velocity.x > 0))
                {
                    //animator.SetBool("bonk", true);
                    rb.velocity = new Vector3(0, 0, 0);
                }
                break;
            case State.Airborne:
                break;
            case State.Attached:
                break;
            case State.Swinging:
                state = State.Airborne;
                // No swing CD if we're grounded
                if (!floorCollision)
                {
                    StartCoroutine(HandleClouds());
                    StartCoroutine(DelaySwing(DELAY_NORMAL));
                }
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
                state = State.Airborne;
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
        Vector3 leftRayStart = transform.position - new Vector3(1.117647058823529f * boxCollider.size.x / 2, boxCollider.size.y / 2 - 0.2f, 0);
        Vector3 rightRayStart = transform.position + new Vector3(1.117647058823529f * boxCollider.size.x / 2, -boxCollider.size.y / 2 + 0.2f, 0);
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
                onSlope = false;
                return;
            }
            /*
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
            */
            if (leftHit && rb.velocity.x < 0f || rightHit && rb.velocity.x > 0f)
            {
                // adjustingDirection *= -1;
            }
            if (!onSlope)
            {
                boxCollider.sharedMaterial = noFrictionMaterial;
                onSlope = true;
                if ((rb.velocity.x > 0 && facingRight) || (rb.velocity.x < 0 && !facingRight))
                {
                    animator.SetBool("rolling", true);
                    animator.SetBool("backsliding", false);
                }
                else
                {
                    Camera.main.GetComponent<AudioSource>().PlayOneShot(jumpSound);
                    animator.SetBool("backsliding", true);
                    animator.SetBool("rolling", false);
                }
            }
            /*
            var adjustedVelocity = adjustingDirection * rb.velocity.magnitude;
            Debug.Log("--------------------\nState: " + state);
            Debug.Log("dist between player and righthit point: " + Vector2.Distance(transform.position, rightHit.point));
            Debug.Log("Adjusted Velocity x: " + adjustedVelocity.x.ToString("0.000000000000000000") + "\n------------------\n");
            Debug.Log("Adjusted Velocity y: " + adjustedVelocity.y.ToString("0.000000000000000000") + "\n------------------\n");
            adjustedVelocity *= 1.01f;
            if (adjustedVelocity.y < -0.0000000000000001f || adjustedVelocity.y > 0.0000000000000001f)
            {
                if (!onSlope)
                {
                    boxCollider.sharedMaterial = noFrictionMaterial;
                    onSlope = true;
                    animator.SetBool("rolling", true);
                }
                rb.velocity = adjustedVelocity;
            }
            // For the case where our collider is hanging off the edge, we don't want adjusting to be true.
            else
            {
                boxCollider.sharedMaterial = null;
                onSlope = false;
            }
            */
        }
        else
        {
            boxCollider.sharedMaterial = null;
            onSlope = false;
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
        stringBuilder.Append("Player Velocity: " + rb.velocity + "\n");
        stringBuilder.Append("Collision Velocity: " + collision.relativeVelocity + "\n");
        stringBuilder.Append("leftCollision: " + IsLeftCollision(collision) + "\n");
        stringBuilder.Append("rightCollision: " + IsRightCollision(collision) + "\n");
        stringBuilder.Append("ceilingCollision: " + IsCeilingCollision(collision) + "\n");
        stringBuilder.Append("floorCollision: " + IsFloorCollision(collision) + "\n");
        stringBuilder.Append("Timestamp : " + Time.time + "\n");
        debugLogs.SetText(stringBuilder.ToString());
    }
}
