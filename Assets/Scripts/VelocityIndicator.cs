using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityIndicator : MonoBehaviour
{
    public PlayerController player;
    LineRenderer line;
    // Start is called before the first frame update
    void Start()
    {
        line = this.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        line.SetPosition(0, player.transform.position);
        line.SetPosition(1, player.transform.position + 3 * (Vector3)player.rb.velocity.normalized);
        /*
        for (int i = 0; i < this.transform.childCount; i++)
        {
            Transform t = transform.GetChild(i);
            t.position = player.transform.position + 2 * (i + 1) * (Vector3)player.rb.velocity.normalized;
        }
        */
    }
}
