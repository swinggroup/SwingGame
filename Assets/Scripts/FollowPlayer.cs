using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public GameObject player;
    bool goStart;
    int camSize = 12;
    Camera cam;
    private const float offsetMax = 10f;
    private Vector3 horizontalOffset;
    private Vector3 verticalOffset;
    bool left;
    bool right;
    bool up;
    bool down;

    // Start is called before the first frame update
    void Start()
    {
        verticalOffset = new();
        horizontalOffset = new();
        cam = GetComponent<Camera>();
        GoAgain();
    }

    public void GoAgain()
    {
        goStart = false;
        transform.position = new Vector3(-34.3f, -29.3f, -10f);
        cam.orthographicSize = 48;
        InvokeRepeating("Zoomer", .01f, .01f);  //1s delay, repeat every 1s
    }

    private void Update()
    {
        verticalOffset = new();
        horizontalOffset = new();
        if (Input.GetKeyDown(KeyCode.R))
        {
            GoAgain();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            up = true;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            down = true;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            left = true;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            right = true;
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            up = false;
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            left = false;
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            down = false;
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            right = false;
        }

        if (up)
        {
            verticalOffset += new Vector3(0, offsetMax, 0);
        }
        if (down)
        {
            verticalOffset += new Vector3(0, -offsetMax, 0);
        }
        if (left)
        {
            horizontalOffset += new Vector3(-offsetMax, 0, 0);
        }
        if (right)
        {
            horizontalOffset += new Vector3(offsetMax, 0, 0);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 desiredPosition = player.transform.position + horizontalOffset + verticalOffset;
        desiredPosition.z = transform.position.z;
        if ((!up && !down && !left && !right) && Vector3.Distance(transform.position, desiredPosition) < 1f)
        {
            this.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, this.transform.position.z);
        } else
        {
            transform.position = new Vector3(Mathf.Lerp(transform.position.x, desiredPosition.x, 0.1f), Mathf.Lerp(transform.position.y, desiredPosition.y, 0.1f), -10);
        }
        /*
        if (goStart)
        {
            this.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, this.transform.position.z) + horizontalOffset + verticalOffset; 
        }
        */
    }

    void Zoomer()
    {

        if (goStart == false)
        {
            this.transform.position = Vector3.MoveTowards(this.transform.position, new Vector3(player.transform.position.x, player.transform.position.y, this.transform.position.z), 3);
            cam.orthographicSize -= 1;

            if (cam.orthographicSize == 15)
            {
                goStart = true;
            }
        }
    }
}
