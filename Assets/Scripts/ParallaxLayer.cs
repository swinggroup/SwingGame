using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public PlayerController playerController;
    private float startXPos;
    private float startYPos;
    private float playerXStart;
    private float playerYStart;
    // Start is called before the first frame update
    void Start()
    {
        startXPos = this.transform.position.x; 
        startYPos = this.transform.position.y;
        playerXStart = playerController.transform.position.x;
        playerYStart = playerController.transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        // standard parallax
        if (gameObject.CompareTag("Parallax1"))
        {
            this.transform.position = new Vector3(-0.08f * (playerController.transform.position.x - (playerXStart)) + (startXPos), -0.08f * (playerController.transform.position.y - (playerYStart)) + (startYPos), this.transform.position.z);
        } else if (gameObject.CompareTag("Parallax2"))
        {
            this.transform.position = new Vector3(-0.04f * (playerController.transform.position.x - (playerXStart)) + (startXPos), -0.04f * (playerController.transform.position.y - (playerYStart)) + (startYPos), this.transform.position.z);
        } else if (gameObject.CompareTag("Parallax3"))
        {
            // this.transform.position = new Vector3(-0.025f * (playerController.transform.position.x - (playerXStart)) + (startXPos), -0.025f * (playerController.transform.position.y - (playerYStart)) + (startYPos), this.transform.position.z);
            this.transform.position = new Vector3(((250f/300f) * playerController.transform.position.x - (playerXStart)) + (startXPos), ((250f/300f) * playerController.transform.position.y - (playerYStart)) + (startYPos), this.transform.position.z);
        } else
        {
            Debug.LogError("no parallax tag on this object: " + gameObject);
            return;
        }

        // parallax, but centered on the player
        // this.transform.position = new Vector3(-0.15f * (playerController.transform.position.x - (xCenter)) + (playerController.transform.position.x), this.transform.position.y, this.transform.position.z);
    }
}
