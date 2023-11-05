using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AnchorIndicator : MonoBehaviour
{
    public PlayerController player;
    // Start is called before the first frame update
    private const float OVERLAP_CIRCLE_RADIUS = 3f;
    void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 ourPos = new(player.transform.position.x, player.transform.position.y);
        Vector2 unitVector = (mousePos - ourPos).normalized;

        bool withinRange(Vector2 vector) => (ourPos - vector).magnitude <= PlayerController.GRAPPLE_RANGE;

        RaycastHit2D raycastHit = Physics2D.Raycast(ourPos, unitVector, (ourPos - (Vector2)this.transform.position).magnitude + 0.1f, LayerMask.GetMask("Hookables"));

        if (raycastHit && raycastHit.collider.CompareTag("Hookable"))
        {
            this.GetComponent<SpriteRenderer>().color = Color.red;
            this.transform.position = raycastHit.point;
        }
        else if (raycastHit && raycastHit.collider.CompareTag("Unhookable"))
        {
            this.GetComponent<SpriteRenderer>().color = Color.black;
            this.transform.position = raycastHit.point;
        }
        else
        {
            // doing physics calculations in lateupdate causes indicator jitter
            this.GetComponent<SpriteRenderer>().color = Color.black;
            this.transform.position = mousePos;
            if (!withinRange(mousePos))
            {
                if (player.state == PlayerController.State.Swinging)
                {
                    this.transform.position = player.rope.anchorPoint;
                }
                this.transform.position = ourPos + (unitVector * PlayerController.GRAPPLE_RANGE);
            }
            // Anchor indicator snap
            Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(this.transform.position, OVERLAP_CIRCLE_RADIUS, LayerMask.GetMask("Hookables"));
            if (collidersInRange.Length > 0)
            {
                collidersInRange = collidersInRange.OrderBy(c => Vector2.Distance(c.ClosestPoint(this.transform.position), this.transform.position)).ToArray();
                Vector2 closestPoint = collidersInRange[0].ClosestPoint(this.transform.position);

                // Directly raycasting from the player to closest point doesn't always work. (the raycast might barely miss on a corner)
                // Instead, raycast from the anchor indicator to the closest point, then raycast from the player to thefirst raycast hit
                // point to snap the anchor indicator to the closest platform visible to the player.
                unitVector = (closestPoint - (Vector2)this.transform.position).normalized;
                raycastHit = Physics2D.Raycast(this.transform.position, unitVector, ((Vector2)this.transform.position - closestPoint).magnitude + 0.1f, LayerMask.GetMask("Hookables"));
                unitVector = (raycastHit.point - ourPos).normalized;
                raycastHit = Physics2D.Raycast(ourPos, unitVector, PlayerController.GRAPPLE_RANGE, LayerMask.GetMask("Hookables"));
                if (raycastHit && withinRange(raycastHit.point))
                {
                    // TODO: Rethink tags 
                    if (collidersInRange[0].CompareTag("Hookable"))
                    {
                        this.GetComponent<SpriteRenderer>().color = Color.red;
                    }
                    this.transform.position = raycastHit.point;
                }
            }
        }
        if (player.state == PlayerController.State.Swinging || player.state == PlayerController.State.Attached)
        {
            this.GetComponent<SpriteRenderer>().color = Color.yellow;
            this.transform.position = player.rope.anchorPoint;
        }
    }
}
