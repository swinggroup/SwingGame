using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FairyMove : MonoBehaviour
{
    private readonly float PixelsPerUnit = 16;
    public AudioSource hum;
    public PlayerController player;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        // Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = player.transform.position;
    }

    private bool CursorInRadius()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Debug.Log("screenCneter in screen:" + screenCenter);
        Debug.Log("screenCenter in world: " + Camera.main.ScreenToWorldPoint(screenCenter));
        Debug.Log("transform.position: " + transform.position);
        Debug.Log(Vector2.Distance(Camera.main.ScreenToWorldPoint(screenCenter), transform.position));
        return Vector2.Distance(Camera.main.ScreenToWorldPoint(screenCenter), transform.position) <= PlayerController.CURSOR_RADIUS;
        
    }

    private void FixedUpdate()
    {
        transform.position += new Vector3(player.rb.velocity.x * Time.fixedDeltaTime, player.rb.velocity.y * Time.fixedDeltaTime);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position += new Vector3(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"), 0);

        Debug.Log(transform.position);
        

        if (!CursorInRadius())
        {
            Vector3 cursorScreenSpace = Camera.main.WorldToScreenPoint(transform.position);
            cursorScreenSpace.z = 0;
            Vector3 centerToCursorScreenSpace = cursorScreenSpace - new Vector3(Screen.width / 2, Screen.height / 2, 0);
            Debug.Log("centerToCursorScreenSpace: " + centerToCursorScreenSpace);
            centerToCursorScreenSpace = centerToCursorScreenSpace.normalized * PixelsPerUnit * PlayerController.CURSOR_RADIUS;
            Debug.Log("centerToCursorScreenSpace.normalized * PixelsPerUnit * PlayerController.CURSOR_RADIUS: " + centerToCursorScreenSpace);
            transform.position = player.transform.position + (centerToCursorScreenSpace / PixelsPerUnit);
            Debug.Log("transform.position (world): " + transform.position);
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        }


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
        Debug.Log("-------------------");

    }
}
