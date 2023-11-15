using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxScrollLayer : MonoBehaviour
{
    List<Transform> children;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Transform child in transform)
        {
            child.position += new Vector3(1f * Time.deltaTime, 0, 0);
            if (child.position.x > 100)
            {
                child.position -= new Vector3(4 * 62.5f, 0, 0);
            }
        }
    }
}
