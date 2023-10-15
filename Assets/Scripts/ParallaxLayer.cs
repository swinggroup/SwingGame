using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public PlayerController playerController;
    private float xCenter = -28f;
    private float startXPos;
    // Start is called before the first frame update
    void Start()
    {
        startXPos = this.transform.position.x; 
    }

    // Update is called once per frame
    void Update()
    {
        // standard parallax
        if (gameObject.CompareTag("Parallax1"))
        {
            this.transform.position = new Vector3(-0.08f * (playerController.transform.position.x - (startXPos)) + (startXPos), this.transform.position.y, this.transform.position.z);
        } else if (gameObject.CompareTag("Parallax2"))
        {
            this.transform.position = new Vector3(-0.04f * (playerController.transform.position.x - (startXPos)) + (startXPos), this.transform.position.y, this.transform.position.z);
        } else if (gameObject.CompareTag("Parallax3"))
        {
            this.transform.position = new Vector3(-0.025f * (playerController.transform.position.x - (startXPos)) + (startXPos), this.transform.position.y, this.transform.position.z);
        } else
        {
            Debug.LogError("no parallax tag on this object: " + gameObject);
            return;
        }

        // parallax, but centered on the player
        // this.transform.position = new Vector3(-0.15f * (playerController.transform.position.x - (xCenter)) + (playerController.transform.position.x), this.transform.position.y, this.transform.position.z);
    }
}
