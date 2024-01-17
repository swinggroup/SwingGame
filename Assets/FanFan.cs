using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FanFan : MonoBehaviour
{
    public PlayerController player;
    public AudioClip sound;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Camera.main.GetComponent<AudioSource>().PlayOneShot(sound);
        if (player.state == PlayerController.State.Airborne)
        {
            player.ResetSwingDelay();
        }
        StartCoroutine(Despawn());
    }

    IEnumerator Despawn()
    {
        GetComponent<BoxCollider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;
        yield return new WaitForSeconds(3f);
        GetComponent<BoxCollider2D>().enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
    }
}
