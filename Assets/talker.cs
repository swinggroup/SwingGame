using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class talker : MonoBehaviour
{
    public string[] lines;
    public string spriteName;
    public GameObject player;
    private TextMeshPro text;
    private int len;
    private bool talking;
    private GameObject circle;
    private bool talked;
    private Vector3 home;
    private Color originalColor;
    private GameObject dialogue;

    // Start is called before the first frame update
    void Start()
    {
        talked = false;
        talking = false;
        text = GetComponentInChildren<TextMeshPro>();
        dialogue = GameObject.Find("dialogue");
        len = lines.Length;
        circle = transform.Find("Circle").gameObject;
        home = text.transform.position;
        originalColor = text.faceColor;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(!talking)
        {
            if (Vector3.Distance(player.transform.position, this.transform.position) < 5)
            {
                circle.SetActive(true);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    talking = true;
                    StartCoroutine(IterateTalk());
                    dialogue.GetComponent<Dialogue>().InitializeDialogue(lines, spriteName);
                }
            }
            else
            {
                circle.SetActive(false);
            }
        }
        else
        {
            circle.SetActive(false);
        }
    }



    IEnumerator IterateTalk()
    {
        talking = true;
        text.text = "";

        for (int i = 0; i < len; i++)
        {
            text.text += lines[i];
            yield return new WaitForSeconds(i+1);
            text.text += "\n";
        }
        if (talked == false) //First time Haiku
        {
            while (Vector3.Distance(player.transform.position, text.transform.position) > 10)
            {
                yield return new WaitForSeconds(0.0005f);
                text.transform.position = Vector3.MoveTowards(text.transform.position, player.transform.position, .1f);
            }

            text.transform.SetParent(player.transform);
            yield return new WaitForSeconds(len + 1);

            var col = text.faceColor;
            while (col.a > 0)
            {
                yield return new WaitForSeconds(0.0005f);
                col = new Color32(col.r, col.g, col.b, (byte)(col.a - 1));
                text.color = col;
            }


            text.transform.SetParent(transform);
            text.transform.position = home;
            talked = true;
            text.color = originalColor;
            player.GetComponent<PlayerController>().AddHaiku(name, lines);

        }
        text.text = "";
        talking = false;

    }
}
