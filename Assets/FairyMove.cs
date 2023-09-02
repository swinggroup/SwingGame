using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FairyMove : MonoBehaviour
{
    public PlayerController player;
    public GameObject runeRope;
    // Start is called before the first frame update
    void Start()
    {
        //runeRope.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (player.state == PlayerController.State.Attached || player.state == PlayerController.State.Swinging)
        {
            runeRope.SetActive(true);
            runeRope.transform.right = player.transform.position - runeRope.transform.position;
            runeRope.transform.position = transform.position;

            Vector2 anchorPoint = player.rope.anchorPoint;

            // Get in between player and mouse
            transform.position = Vector3.Lerp(anchorPoint, player.transform.position, 0.5f);
        } else
        {
            runeRope.SetActive(false);

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Get in between player and mouse
            transform.position = Vector3.Lerp(mousePos, player.transform.position, 0.5f);
        }

        
    }
}
