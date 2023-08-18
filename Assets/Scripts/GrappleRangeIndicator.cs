using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleRangeIndicator : MonoBehaviour
{
    public GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        this.transform.localScale = new Vector3(1, 1, 1);
        this.transform.localScale *= 2 * PlayerController.GRAPPLE_RANGE;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        this.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, this.transform.position.z);
    }
}
