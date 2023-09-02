using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class anchorIndicator : MonoBehaviour
{
    public PlayerController player;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 ourPos = new(player.transform.position.x, player.transform.position.y);
        Vector2 unitVector = (mousePos - ourPos).normalized;
        Debug.Log(mousePos);
        Debug.Log("ourpos" + ourPos);
        RaycastHit2D hit = Physics2D.Raycast(ourPos, unitVector, PlayerController.GRAPPLE_RANGE, LayerMask.GetMask("Hookables"));
        if (hit)
        {
            this.GetComponent<SpriteRenderer>().color = Color.red;
            this.transform.position = hit.point;
        } 
        else
        {
            this.GetComponent<SpriteRenderer>().color = Color.black;
            this.transform.position = mousePos;
        }
    }
}
