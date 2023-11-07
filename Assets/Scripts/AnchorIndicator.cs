using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class AnchorIndicator : MonoBehaviour
{
    public PlayerController player;
    // Start is called before the first frame update
    private const float OVERLAP_CIRCLE_RADIUS = 1.8f;
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

        RaycastHit2D raycastHit = Physics2D.Raycast(ourPos, unitVector, Mathf.Min((ourPos - (Vector2)this.transform.position).magnitude + 0.3f, PlayerController.GRAPPLE_RANGE), LayerMask.GetMask("Hookables"));

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
                // Instead, raycast from the anchor indicator to the closest point, then raycast from the player to the first raycast hit
                // point to snap the anchor indicator to the closest platform visible to the player.
                
                unitVector = (closestPoint - ourPos).normalized;
                raycastHit = Physics2D.CircleCast(ourPos, 0.1f, unitVector, (closestPoint - ourPos).magnitude + 0.2f, LayerMask.GetMask("Hookables"));
                // Debug.Log("Rayhit point: " + raycastHit.point);
                // Debug.DrawRay(ourPos, unitVector * ((raycastHit.point - ourPos).magnitude + 0.2f), Color.blue);
                if (raycastHit)
                {
                    // TODO: Rethink tags 
                    if (collidersInRange[0].CompareTag("Hookable"))
                    {
                        this.GetComponent<SpriteRenderer>().color = Color.red;
                    }
                    if (withinRange(raycastHit.point))
                    {
                        this.transform.position = raycastHit.point;
                    }
                    // Anchor indicator is potentially outside of the grapple range but still close enough to a platform that we want to snap it.
                    // We snap to the intersection point between the circle made by the grapple radius and the platform.
                    else
                    {
                        var angle = Vector2.SignedAngle(Vector2.up.normalized, (closestPoint - ourPos).normalized);
                        angle = angle < 0 ? angle + 360 : angle;
                        angle += 90f;
                        // Debug.Log("Angle: " + angle);
                        // Debug.Log("Max angle needed: " + Mathf.Asin((player.boxCollider.size.y / 2) / PlayerController.GRAPPLE_RANGE) * Mathf.Rad2Deg);
                        Vector2 intersectionPoint = ArcColliderIntersectionPoint(ourPos, PlayerController.GRAPPLE_RANGE, angle);
                        if (!intersectionPoint.Equals(Vector2.negativeInfinity))
                        {
                            this.transform.position = intersectionPoint;
                        }
                        else
                        {
                            this.GetComponent<SpriteRenderer>().color = Color.black;
                        }
                    }
                        
                }
            }
        }
        if (player.state == PlayerController.State.Swinging || player.state == PlayerController.State.Attached)
        {
            this.GetComponent<SpriteRenderer>().color = Color.yellow;
            this.transform.position = player.rope.anchorPoint;
        }
    }

    Vector2 ArcColliderIntersectionPoint(Vector2 circleCenter, float circleRadius, float startAngle)
    {
        float step = .05f;
        float arcAngle = Mathf.Asin(((player.boxCollider.size.y / 2) / PlayerController.GRAPPLE_RANGE) + 0.1f) * Mathf.Rad2Deg;

        for (float angleOffset = 0f; angleOffset <= arcAngle; angleOffset += step)
        {
            var radianAngleIncreasing = Mathf.Deg2Rad * (startAngle + angleOffset);

            Vector2 direction = new Vector2(Mathf.Cos(radianAngleIncreasing), Mathf.Sin(radianAngleIncreasing));
            RaycastHit2D hit = Physics2D.Raycast(circleCenter, direction, circleRadius, LayerMask.GetMask("Hookables"));

            if (radianAngleIncreasing == startAngle) Debug.DrawRay(circleCenter, direction, Color.green);

            if (!hit.collider.IsUnityNull())
            {
                if (radianAngleIncreasing != startAngle) Debug.DrawRay(circleCenter, direction);
                // Debug.Log("Angle offset when first hit is: " + angleOffset);
                // Debug.Log("Num rays used this frame: " + Mathf.RoundToInt(angleOffset / step));
                return hit.point;
            }

            var radianAngleDecreasing = Mathf.Deg2Rad * (startAngle - angleOffset);


            direction = new Vector2(Mathf.Cos(radianAngleDecreasing), Mathf.Sin(radianAngleDecreasing));
            hit = Physics2D.Raycast(circleCenter, direction, circleRadius, LayerMask.GetMask("Hookables"));

            if (radianAngleDecreasing == startAngle) Debug.DrawRay(circleCenter, direction, Color.green);

            if (!hit.collider.IsUnityNull())
            {
                if (radianAngleDecreasing != startAngle) Debug.DrawRay(circleCenter, direction);
                // Debug.Log("Angle offset when first hit is: " + angleOffset);
                // Debug.Log("Num rays used this frame: " + Mathf.RoundToInt(angleOffset / step));
                return hit.point;
            }
        }
        
        return Vector2.negativeInfinity;
    }
}
