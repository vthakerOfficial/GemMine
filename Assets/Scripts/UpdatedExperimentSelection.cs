using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is attached to the dropdown menu which selects experiments.
/// 
/// It only needs to call UnityEPL.SetExperimentName().
/// </summary>
public class UpdatedExperimentSelection : MonoBehaviour
{
    public InterfaceManager manager;

    void Awake()
    {
        GameObject mgr = GameObject.Find("InterfaceManager");
        if (mgr == null)
        {
            Debug.LogWarning("UpdatedExperimentSelection could not find InterfaceManager.");
            return;
        }

        manager = (InterfaceManager)mgr.GetComponent("InterfaceManager");
        if (manager == null)
        {
            Debug.LogWarning("UpdatedExperimentSelection could not get InterfaceManager component.");
            return;
        }

        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();
        if (dropdown == null)
        {
            Debug.LogWarning("UpdatedExperimentSelection requires a Dropdown component.");
            return;
        }

        string[] experiments = manager.GetSetting<string[]>("availableExperiments", new string[] { "Goldmine" });
        if (experiments == null || experiments.Length == 0)
        {
            experiments = new string[] { "Goldmine" };
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(new string[] {"Select Task..."}));
        dropdown.AddOptions(new List<string>(experiments));
        SetExperiment();
    }

    public void SetExperiment()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        if (manager == null || dropdown == null || dropdown.captionText == null)
        {
            return;
        }

        if(dropdown.captionText.text != "Select Task...") {
            manager.Do(new EventBase<string>(manager.LoadExperimentConfig, 
                dropdown.captionText.text));
        }
    }
}
