using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    public Transform StartPoint;
    public Transform EndPoint;

    private const int NOT_TAUT_SWING_FRAMES = 50;

    private int notTautSwingFrames;
    public double length;
    public Vector2 anchorPoint;
    public GameObject player;
    Rigidbody2D rb;

    private PlayerController playerController;
    private LineRenderer lineRenderer;
    private List<RopeSegment> ropeSegments = null;
    private float ropeSegLen = 0.25f;
    private float lineWidth = 0.0625f;
    private int segmentLength;
    public Vector3 playerPhysicsTransform;

    // Use this for initialization
    void Start()
    {
        this.rb = player.GetComponent<Rigidbody2D>();
        this.lineRenderer = this.GetComponent<LineRenderer>();
        this.playerController = player.GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        this.DrawRope();
    }

    private void FixedUpdate()
    {
        if (ropeSegments != null && notTautSwingFrames > 0)
        {
            this.Simulate();
        }
    }

    public void NewRope(Vector2 anchorPoint)
    {
        notTautSwingFrames = NOT_TAUT_SWING_FRAMES;
        this.anchorPoint = anchorPoint;
        ropeSegments = new();
        StartPoint.position = anchorPoint;
        EndPoint.position = player.transform.position;
        length = Vector2.Distance(player.transform.position, anchorPoint);

        segmentLength = (int)(length * (1f / ropeSegLen));
        Vector3 ropeStartPoint = StartPoint.position;

        for (int i = 0; i < segmentLength; i++)
        {
            this.ropeSegments.Add(new RopeSegment(ropeStartPoint));
            ropeStartPoint.y -= ropeSegLen;
        }
    }

    public bool RopeExists() { return ropeSegments != null; }

    public void DeleteRope()
    {
        ropeSegments = null;
    }

    private void Simulate()
    {
        // SIMULATION
        Vector2 forceGravity = new Vector2(0f, -0.5f);

        for (int i = 1; i < this.segmentLength - 1; i++) // don't apply gravity to 1st or last segment
        {
            RopeSegment currSegment = this.ropeSegments[i];
            Vector2 velocity = currSegment.posNow - currSegment.posOld;
            currSegment.posOld = currSegment.posNow;
            currSegment.posNow += velocity;
            currSegment.posNow += forceGravity * Time.fixedDeltaTime;
            this.ropeSegments[i] = currSegment;
        }

        //CONSTRAINTS
        for (int i = 0; i < 50; i++)
        {
            this.ApplyConstraint();
        }

    }

    private void ApplyConstraint()
    {
        
        // Constraint to First Point 
        RopeSegment firstSegment = this.ropeSegments[0];
        firstSegment.posNow = this.StartPoint.position;
        this.ropeSegments[0] = firstSegment;


        // Constraint to Second Point 
        RopeSegment endSegment = this.ropeSegments[ropeSegments.Count - 1];
        // endSegment.posNow = this.EndPoint.position;
        endSegment.posNow = playerPhysicsTransform;
        this.ropeSegments[ropeSegments.Count - 1] = endSegment;

        for (int i = 0; i < this.segmentLength - 1; i++)
        {
            RopeSegment firstSeg = this.ropeSegments[i];
            RopeSegment secondSeg = this.ropeSegments[i + 1];

            float dist = (firstSeg.posNow - secondSeg.posNow).magnitude;
            float error = Mathf.Abs(dist - this.ropeSegLen);
            Vector2 changeDir = Vector2.zero;

            if (dist > ropeSegLen)
            {
                changeDir = (firstSeg.posNow - secondSeg.posNow).normalized;
            }
            else if (dist < ropeSegLen)
            {
                changeDir = (secondSeg.posNow - firstSeg.posNow).normalized;
            }

            Vector2 changeAmount = changeDir * error;
            if (i != 0 && i != segmentLength - 2)
            {
                firstSeg.posNow -= changeAmount * 0.5f;
                this.ropeSegments[i] = firstSeg;
                secondSeg.posNow += changeAmount * 0.5f;
                this.ropeSegments[i + 1] = secondSeg;
            } else if (i == segmentLength - 2) // don't change the position of the second anchor point (where rope attaches to player)
            {
                firstSeg.posNow -= changeAmount;
                this.ropeSegments[i] = firstSeg;
            }
            else // don't change position of the first anchor point (where we lock on)
            {
                secondSeg.posNow += changeAmount;
                this.ropeSegments[i + 1] = secondSeg;
            }
        }
    }

    private void DrawRope()
    {
        if (ropeSegments == null)
        {
            lineRenderer.positionCount = 0;
            return;
        }
        if (playerController.state == PlayerController.State.Swinging && notTautSwingFrames > 0)
        {
            notTautSwingFrames -= 1;
        }

        if (playerController.state == PlayerController.State.Swinging && notTautSwingFrames == 0)
        {
            lineRenderer.startWidth = this.lineWidth;
            lineRenderer.endWidth = this.lineWidth;

            Vector3[] straightRopePos = new Vector3[2];
            straightRopePos[0] = StartPoint.transform.position;
            straightRopePos[1] = playerPhysicsTransform;
            lineRenderer.positionCount = straightRopePos.Length;
            lineRenderer.SetPositions(straightRopePos);
            return;
        }

        lineRenderer.startWidth = this.lineWidth;
        lineRenderer.endWidth = this.lineWidth;

        Vector3[] ropePositions = new Vector3[this.segmentLength];
        for (int i = 0; i < this.segmentLength; i++)
        {
            ropePositions[i] = this.ropeSegments[i].posNow;
        }
        // lock draw rope final segment to player transform pos
        // doesnt work properly, bc the simulation thinks player position is elsewehre
        // ropePositions[segmentLength - 1] = player.transform.position;
        lineRenderer.positionCount = ropePositions.Length;
        lineRenderer.SetPositions(ropePositions);
    }

    public struct RopeSegment
    {
        public Vector2 posNow;
        public Vector2 posOld;

        public RopeSegment(Vector2 pos)
        {
            this.posNow = pos;
            this.posOld = pos;
        }
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