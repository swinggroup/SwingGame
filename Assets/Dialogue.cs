using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class Dialogue : MonoBehaviour
{

    public enum State {PLAYING_TEXT, FINISHED_LINE, WAITING_FOR_DIALOGUE};
    public State state;

    public TextMesh text;
    public GameObject dialogueBox;
    public GameObject eButton;
    public GameObject portrait;

    private int currLine;
    private string[] currText;
    private Coroutine lineCoroutine;
    private SpriteLibrary lib;

    // Start is called before the first frame update
    void Start()
    {
        state = State.WAITING_FOR_DIALOGUE;
        HideDialogue();
        currLine = 0;
        lib = this.GetComponent<SpriteLibrary>();
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
                    StopCoroutine(lineCoroutine);
                    text.text = currText[currLine];
                    state = State.FINISHED_LINE;
                    ShowEButton();
                    currLine++;
                }
                break;
            case State.FINISHED_LINE:
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (currLine == currText.Length)
                    {
                        state = State.WAITING_FOR_DIALOGUE;
                        text.text = "";
                        HideDialogue();
                    } else
                    {
                        state = State.PLAYING_TEXT;
                        lineCoroutine = StartCoroutine(PlayLine());
                    }
                }
                break;
            case State.WAITING_FOR_DIALOGUE:
                break;
            default:
                break;
        } 
    }
    
    private void HideDialogue()
    {
        eButton.GetComponent<Renderer>().enabled = false;
        dialogueBox.GetComponent<Renderer>().enabled = false;
        text.GetComponent<Renderer>().enabled = false;
        portrait.GetComponent<Renderer>().enabled = false;
    }

    private void ShowDialogue()
    {
        eButton.GetComponent<Renderer>().enabled = true;
        dialogueBox.GetComponent<Renderer>().enabled = true;
        text.GetComponent<Renderer>().enabled = true;
        portrait.GetComponent<Renderer>().enabled = true;
    }

    private void HideEButton()
    {
        eButton.GetComponent<Renderer>().enabled = false;
    }

    private void ShowEButton()
    {
        eButton.GetComponent<Renderer>().enabled = true;
    }

    public void InitializeDialogue(string[] text, string characterName)
    {
        if (!state.Equals(State.WAITING_FOR_DIALOGUE))
        {
            return;
        }

        portrait.GetComponent<SpriteRenderer>().sprite = lib.GetSprite("Portraits", characterName);
        ShowDialogue();
        HideEButton();
        currText = text;
        currLine = 0;
        lineCoroutine = StartCoroutine(PlayLine());
    }

    IEnumerator PlayLine()
    {
        string line = currText[currLine];
        HideEButton();
        Debug.Log(line);
        for (int i = 0; i <= line.Length; i++)
        {
            yield return new WaitForSeconds(0.05f);
            state = State.PLAYING_TEXT; // to avoid the initial e press also triggering the dialogue animation skip
            text.text = line.Substring(0, i);
            Debug.Log(text.text);
        }
        state = State.FINISHED_LINE;
        ShowEButton();
        currLine++;
    }
}
