using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    Rigidbody2D rb;
    Rope rope;
    State state;
    LineRenderer ropeLine;
    Vector2 spinVelocity;

    private class Rope
    {
        public double length;
        public Vector2 anchorPoint;
        GameObject player;
        Rigidbody2D rb;

        public Rope(GameObject player, Rigidbody2D rb)
        {
            this.player = player;
            this.rb = rb;
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
        Grounded, Airborne, Attached, Swinging, Test
    }

    // Start is called before the first frame update
    void Start()
    {
        ropeLine = this.gameObject.AddComponent<LineRenderer>();
        ropeLine.enabled = false;
        rb = this.GetComponent<Rigidbody2D>();        
        rope = new(this.gameObject, this.rb);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
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
                break;
            default:
                break;
        }
    }

    void HandleGrounded()
    {
        ropeLine.enabled = false;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(new Vector2(0, 1000));
            state = State.Airborne;
        } 
    }

    void HandleAirborne()
    {
        Vector2 ourPos = new Vector2(this.transform.position.x, this.transform.position.y);

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            rope.anchorPoint = new Vector2(mousePos.x, mousePos.y);
            Debug.Log("anchor point: " + rope.anchorPoint);
            Debug.Log("our pos: " + ourPos);
            rope.length = Vector2.Distance(ourPos, rope.anchorPoint);
            Debug.Log("rope length: " + rope.length);

            state = State.Attached;

            // Draw line from player to anchor
            ropeLine.enabled = true;
        }
    }

    void HandleAttached()
    {
        UpdateRope();
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

            state = State.Swinging;
        }
    }

    void HandleSwinging()
    {
        UpdateRope();
        
        double forceMagnitude = rb.mass * Vector2.SqrMagnitude(spinVelocity) / rope.length;
        Vector2 force = rope.NormalizedPlayerToAnchor();
        force = new Vector2(force.x * (float) forceMagnitude, force.y * (float) forceMagnitude);
        Debug.Log(force.magnitude);
        rb.AddForce(force, ForceMode2D.Force);
    }

    private void UpdateRope()
    {
        Vector2 ourPos = new(this.transform.position.x, this.transform.position.y);
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
        state = State.Grounded;        
    }

}
