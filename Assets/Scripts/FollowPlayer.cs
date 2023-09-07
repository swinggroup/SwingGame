using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public GameObject player;
    bool goStart;
    int camSize = 12;
    Camera cam;

    // Start is called before the first frame update
    void Start()
    {

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
        if (Input.GetKeyDown(KeyCode.R))
        {
            GoAgain();
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {

        if (goStart)
        {
            this.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, this.transform.position.z);

        }
    }

    void Zoomer()
    {

        if (goStart == false)
        {
            this.transform.position = Vector3.MoveTowards(this.transform.position, new Vector3(player.transform.position.x, player.transform.position.y, this.transform.position.z), 3);
            cam.orthographicSize -= 1;

            if (cam.orthographicSize == 12)
            {
                goStart = true;
            }
        }
    }
}
