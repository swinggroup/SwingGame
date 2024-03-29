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
        return Vector3.Distance(screenCenter, transform.position) <= PlayerController.CURSOR_RADIUS;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position += new Vector3(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"), 0);

        Debug.Log(transform.position);

        if (!CursorInRadius())
        {
            Vector3 cursorScreenSpace = Camera.main.WorldToScreenPoint(transform.position);
            Vector3 centerToCursorScreenSpace = cursorScreenSpace - new Vector3(Screen.width / 2, Screen.height / 2, 0);
            centerToCursorScreenSpace = cursorScreenSpace.normalized * PixelsPerUnit * PlayerController.CURSOR_RADIUS;
            transform.position = Camera.main.ScreenToWorldPoint(centerToCursorScreenSpace);
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


    }
}
