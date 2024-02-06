using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TextLayer : MonoBehaviour
{
    public PlayerController player;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(GetComponent<MeshRenderer>().sortingLayerName);
        GetComponent<MeshRenderer>().sortingOrder = 10001;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        this.transform.position = new(player.transform.position.x, player.transform.position.y - 8.475f);
    }

    private void FixedUpdate()
    {
        this.transform.position = new(player.transform.position.x, player.transform.position.y - 8.475f);
    }
}
