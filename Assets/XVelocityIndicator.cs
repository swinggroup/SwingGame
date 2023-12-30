using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XVelocityIndicator : MonoBehaviour
{
    public PlayerController player;
    void Update()
    {
        if (player.rb.velocity.x > 0.1f)
        {
            this.GetComponent<SpriteRenderer>().enabled = true;
            this.transform.position = player.transform.position + new Vector3(1.6f, 0, 0);
            this.GetComponent<SpriteRenderer>().flipX = false;
        } else if (player.rb.velocity.x < -0.1f)
        {
            this.enabled = true;
            this.GetComponent<SpriteRenderer>().enabled = true;
            this.transform.position = player.transform.position + new Vector3(-1.6f, 0, 0);
            this.GetComponent<SpriteRenderer>().flipX = true;
        } else
        {
            this.GetComponent<SpriteRenderer>().enabled = false;
        }
    }
}
