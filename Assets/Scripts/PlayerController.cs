using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    Rigidbody2D rb;
    Rope rope;
    State state;
    LineRenderer ropeLine;

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
        rope = new(this.gameObject, this.rb);
    }

    // Update is called once per frame
    void Update()
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
            state = State.Swinging;
        }
    }

    void HandleSwinging()
    {
        UpdateRope();
    }

    private void UpdateRope()
    {
        Vector2 ourPos = new Vector2(this.transform.position.x, this.transform.position.y);
        List<Vector3> pos = new List<Vector3>();
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
