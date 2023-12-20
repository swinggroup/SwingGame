using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DottedAnchorLine : MonoBehaviour
{
    LineRenderer line;
    public PlayerController player;
    public AnchorIndicator anchor;
    // Start is called before the first frame update
    void Start()
    {
        line = this.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 playerToAnchor = (anchor.transform.position - player.transform.position).normalized;
        playerToAnchor = new Vector3(playerToAnchor.x, playerToAnchor.y, 0).normalized;
        Vector3 start = player.transform.position + 5 * playerToAnchor;
        Vector3 end = player.transform.position - 5 * playerToAnchor;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }
}
