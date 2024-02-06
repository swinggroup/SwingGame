using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dialogue : MonoBehaviour
{

    public enum State {PLAYING_TEXT, FINISHED_LINE, WAITING_FOR_DIALOGUE};
    public State state;
    public TextMesh text;

    // Start is called before the first frame update
    void Start()
    {
        state = State.WAITING_FOR_DIALOGUE;
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case State.PLAYING_TEXT:
                if (Input.GetKeyDown(KeyCode.E))
                {
                    // finish the line and go to finished line state
                }
                break;
            case State.FINISHED_LINE:
                if (Input.GetKeyDown(KeyCode.E))
                {
                    // go next line or end
                }
                break;
            case State.WAITING_FOR_DIALOGUE:
                break;
            default:
                break;
        } 
    }

    public void InitializeDialogue(string[] text)
    {
        if (!state.Equals(State.WAITING_FOR_DIALOGUE))
        {
            return;
        }

        state = State.PLAYING_TEXT;
        // play first line of text
        StartCoroutine(PlayLine(text[0]));
        // show dialogue and text

        // while has lines of text
        //  play line of text
        //  wait for e button
        // hide dialogue and text
    }

    IEnumerator PlayLine(string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            yield return new WaitForSeconds(0.2f);
            text.text = line.Substring(0, i);
        }
    }

    
}
