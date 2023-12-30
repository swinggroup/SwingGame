using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationIndicator : MonoBehaviour
{
    public PlayerController player;
    void Update()
    {
        if (player.facingRight)
        {
            this.transform.position = player.transform.position + new Vector3(1.2f, 0, 0);
            this.GetComponent<SpriteRenderer>().flipX = false;
        } else
        {
            this.transform.position = player.transform.position + new Vector3(-1.2f, 0, 0);
            this.GetComponent<SpriteRenderer>().flipX = true;
        }
    }
}
