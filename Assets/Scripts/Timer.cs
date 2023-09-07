using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class Timer : MonoBehaviour
{
    public PlayerController playerController;
    public Stopwatch timer;

    // Use this later: timer.End();


    TextMeshProUGUI text;

    // Start is called before the first frame update
    void Start()
    {
        timer = new Stopwatch();

        timer.Start();
        text = this.gameObject.GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            timer = new Stopwatch();
            timer.Start();

        }
        text.SetText(timer.Elapsed.ToString("mm\\:ss\\.ff"));
    }
}
