using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene : MonoBehaviour
{
    public static NextScene Instance;

    private void Awake()
    {
        Instance = this; 
        DontDestroyOnLoad(gameObject);
    }
    public void SceneTwo()
    {
        SceneManager.LoadScene("Level2");
    }
}
