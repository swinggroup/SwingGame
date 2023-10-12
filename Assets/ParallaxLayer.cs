using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public PlayerController playerController;
    private float xCenter = -28f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // standard parallax
        this.transform.position = new Vector3(-0.02f * (playerController.transform.position.x - (xCenter)) + (xCenter), this.transform.position.y, this.transform.position.z);

        // parallax, but centered on the player
        // this.transform.position = new Vector3(-0.15f * (playerController.transform.position.x - (xCenter)) + (playerController.transform.position.x), this.transform.position.y, this.transform.position.z);
    }
}
