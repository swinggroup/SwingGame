using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomLightningOffset : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("startinG?");
        foreach (Transform t in transform)
        {
            t.gameObject.GetComponent<Animator>().SetFloat("cycleoffset", Random.Range(0f, 1f));
            t.gameObject.GetComponent<SpriteRenderer>().flipX = Random.Range(0f, 1f) > 0.5f;
            t.gameObject.GetComponent<SpriteRenderer>().flipY = Random.Range(0f, 1f) > 0.5f;
        } 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
