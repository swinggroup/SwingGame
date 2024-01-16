using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FairyMove : MonoBehaviour
{
    public AudioSource hum;
    public PlayerController player;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if ((player.rb.velocity.x < -0.1f && player.facingRight) || (player.rb.velocity.x > 0.1f && !player.facingRight)) {
            this.GetComponent<SpriteRenderer>().color = Color.cyan;
        } else
        {
            this.GetComponent<SpriteRenderer>().color = Color.white;
        }
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = mousePos;
        if (player.state == PlayerController.State.Attached || player.state == PlayerController.State.Swinging)
        {
            hum.loop = true;
            if (!hum.isPlaying)
            {
                hum.Play();
            }
        }
        else
        {
            hum.Stop();
        }


    }
}
