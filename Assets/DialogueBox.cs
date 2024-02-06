using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueBox : MonoBehaviour
{
    public PlayerController player;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void LateUpdate()
    {
        this.transform.position = new(player.transform.position.x, player.transform.position.y - 8.5f);
    }

    private void FixedUpdate()
    {
        this.transform.position = new(player.transform.position.x, player.transform.position.y - 8.5f);
    }
}
