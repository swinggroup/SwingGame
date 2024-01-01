using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAnimationOffset : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform t in transform)
        {
            Animator anim = t.gameObject.GetComponent<Animator>();
            anim.Play(anim.GetCurrentAnimatorClipInfo(0)[0].clip.name, 0, Random.Range(0f, 1f));
        }
    }
}
