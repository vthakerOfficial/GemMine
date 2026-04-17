using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControlTutorialCanvas : MonoBehaviour
{
    public Text centralDisplay; // large text in the middle of the screen
    protected InterfaceManager im;

    void Awake()
    {
        GameObject mgr = GameObject.Find("InterfaceManager");
        if (mgr)
        {
            im = (InterfaceManager)mgr.GetComponent("InterfaceManager");
        }
    }

    public void SetCentralDisplay(string msg)
    {
        im.scriptedInput.ReportScriptedEvent("canvasDisplay", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "canvasChildObjectName", "CentralDisplay" }, { "textDisplayed", msg } });
        centralDisplay.text = msg;
    }

    public void ResetCentralDisplay()
    {
        im.scriptedInput.ReportScriptedEvent("canvasDisplay", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "canvasChildObjectName", "CentralDisplay" }, { "textDisplayed", "" } });
        centralDisplay.text = "";
    }
}
