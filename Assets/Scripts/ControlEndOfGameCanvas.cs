using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControlEndOfGameCanvas : MonoBehaviour
{
    public Text statDisplay; // large text in the middle of the screen
    protected InterfaceManager im;
    private AudioSource audio;

    void Awake()
    {
        GameObject mgr = GameObject.Find("InterfaceManager");
        if (mgr)
        {
            im = (InterfaceManager)mgr.GetComponent("InterfaceManager");
        }

        audio = GetComponent<AudioSource>();
    }

    public void SetStatDisplay(string msg)
    {
        im.scriptedInput.ReportScriptedEvent("canvasDisplay", new Dictionary<string, object> { { "canvasName", "EndOfGameCanvas" }, { "canvasChildObjectName", "StatDisplay" }, { "textDisplayed", msg } });
        statDisplay.text = msg;
    }

    public void playAudio(bool play)
    {
        if (audio)
        {
            if (play)
            {
                audio.Play();
            }
            else
            {
                audio.Stop();
            }
        }
    }
}