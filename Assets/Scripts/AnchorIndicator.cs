using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class anchorIndicator : MonoBehaviour
{
    public PlayerController player;
    public GameObject endMarker;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 ourPos = new(player.transform.position.x, player.transform.position.y);
        Vector2 unitVector = (mousePos - ourPos).normalized;
        RaycastHit2D hit = Physics2D.Raycast(ourPos, unitVector, PlayerController.GRAPPLE_RANGE, LayerMask.GetMask("Hookables"));
        if (hit)
        {
            this.GetComponent<SpriteRenderer>().color = Color.red;
            this.transform.position = hit.point;
            endMarker.SetActive(false);
        } 
        else
        {
            this.GetComponent<SpriteRenderer>().color = Color.black;
            this.transform.position = mousePos;
            if ((ourPos - mousePos).magnitude > PlayerController.GRAPPLE_RANGE)
            {if (player.state == PlayerController.State.Swinging)
        {
            this.transform.position = player.rope.anchorPoint;
        }
                //endMarker.SetActive(true);
                //endMarker.transform.position = ourPos + (unitVector * PlayerController.GRAPPLE_RANGE);
                this.transform.position = ourPos + (unitVector * PlayerController.GRAPPLE_RANGE);
            }
       
            else
            {
                endMarker.SetActive(false);
            }
        }
        if (player.state == PlayerController.State.Swinging)
        {
            this.transform.position = player.rope.anchorPoint;
        }
    }
}
