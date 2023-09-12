using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FairyMove : MonoBehaviour
{
    public AudioSource hum;
    public PlayerController player;
    public GameObject runeRope;
    public GameObject runeRope2;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(Mathf.Lerp(transform.position.x, mousePos.x, 0.1f), Mathf.Lerp(transform.position.y, mousePos.y, 0.1f), 0);
        if (player.state == PlayerController.State.Attached || player.state == PlayerController.State.Swinging)
        {
            Vector2 anchorPoint = player.rope.anchorPoint;
            runeRope.transform.right = player.transform.position - runeRope.transform.position;
            runeRope.transform.position = anchorPoint;//Vector3.Lerp(anchorPoint, player.transform.position, 0.5f);

            //runeRope2.transform.right = runeRope.transform.position - runeRope2.transform.position;
            runeRope2.transform.position = player.transform.position;//Vector3.Lerp(anchorPoint, player.transform.position, 0.5f);

            hum.loop = true;
            if (!hum.isPlaying)
            {
                hum.Play();
            }

            runeRope.SetActive(true);
            runeRope2.SetActive(true);

            // Get in between player and mouse
            // transform.position = Vector3.Lerp(anchorPoint, player.transform.position, 0.5f);
        }
        else
        {
            hum.Stop();
            runeRope.SetActive(false);
            runeRope2.SetActive(false);
            // Get in between player and mouse
            // transform.position = Vector3.Lerp(mousePos, player.transform.position, 0.5f);
        }


    }
}
