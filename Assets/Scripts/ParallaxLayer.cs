using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    private float startXPos;
    private float startYPos;
    readonly private float cameraXStart = -81.5625f;
    readonly private float cameraYStart = -60f;

    readonly private float PARALLAX_1 = 0.13f;
    readonly private float PARALLAX_2 = 0.18f;
    readonly private float PARALLAX_3 = 0.23f;
    readonly private float PARALLAX_4 = 0.28f;
    readonly private float PARALLAX_5 = 0.33f;
    readonly private float PARALLAX_6 = 0.38f;
    readonly private float PARALLAX_7 = 0.43f;
    readonly private float PARALLAX_8 = 0.48f;
    readonly private float PARALLAX_9 = 0.53f;
    readonly private float PARALLAX_10 = 0.58f;
    readonly private float PARALLAX_11 = 0.63f;
    readonly private float PARALLAX_12 = 0.68f;
    readonly private float PARALLAX_13 = 0.73f;
    readonly private float PARALLAX_14 = 0.83f;
    readonly private float PARALLAX_15 = 0.88f;

    // Start is called before the first frame update
    void Start()
    {
        startXPos = this.transform.position.x; 
        startYPos = this.transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        float parallax = 0;
        // standard parallax
        switch (gameObject.tag)
        {
            case "Parallax1":
                parallax = PARALLAX_1;
                break;
            case "Parallax2":
                parallax = PARALLAX_2;
                break;
            case "Parallax3":
                parallax = PARALLAX_3;
                break;
            case "Parallax4":
                parallax = PARALLAX_4;
                break;
            case "Parallax5":
                parallax = PARALLAX_5;
                break;
            case "Parallax6":
                parallax = PARALLAX_6;
                break;
            case "Parallax7":
                parallax = PARALLAX_7;
                break;
            case "Parallax8":
                parallax = PARALLAX_8;
                break;
            case "Parallax9":
                parallax = PARALLAX_9;
                break;
            case "Parallax10":
                parallax = PARALLAX_10;
                break;
            case "Parallax11":
                parallax = PARALLAX_11;
                break;
            case "Parallax12":
                parallax = PARALLAX_12;
                break;
            case "Parallax13":
                parallax = PARALLAX_13;
                break;
            case "Parallax14":
                parallax = PARALLAX_14;
                break;
            case "Parallax15":
                parallax = PARALLAX_15;
                break;
            default:
                Debug.LogError("no parallax tag on object" + gameObject);
                return;
        }
        this.transform.position = new Vector3((parallax * (Camera.main.transform.position.x - cameraXStart)) + (startXPos), (parallax * (Camera.main.transform.position.y - cameraYStart)) + (startYPos), this.transform.position.z);
    }
}
