using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
    public AudioClip titleSong;
    private InterfaceManager im;

    public Dropdown sceneSelection;
    public InputField participantCode;
    public InputField session;
    public GameObject confirmationCanvas;
    public GameObject startCanvas;


    public void Awake()
    {
        Debug.Log("Waking up");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        GameObject mgr = GameObject.Find("InterfaceManager");
        if (mgr)
        {
            im = (InterfaceManager)mgr.GetComponent("InterfaceManager");
        }

        if (im == null)
        {
            Debug.LogWarning("StartGame could not find InterfaceManager.");
            return;
        }

        string[] scenes = im.GetSetting<string[]>("availableScenes", new string[0]);
        if (sceneSelection != null && scenes != null && scenes.Length > 0)
        {
            sceneSelection.ClearOptions();
            sceneSelection.AddOptions(new List<string>(scenes));
            SetScene();
        }

        // Pre-populate input fields from config defaults set by LaunchLauncher
        string savedParticipant = im.GetSetting<string>("participantCode");
        if (!string.IsNullOrEmpty(savedParticipant) && participantCode != null)
            participantCode.text = savedParticipant;

        string savedSession = im.GetSetting<string>("session");
        if (!string.IsNullOrEmpty(savedSession) && session != null)
            session.text = savedSession;
    }

    public void Start()
    {
      ShowConfirmation();
    }

    public void LoadParticipant()
    {
        if (im == null || im.fileManager == null || participantCode == null || session == null)
        {
            Debug.LogWarning("Cannot load participant because startup references are not ready.");
            return;
        }

        string selectedParticipant = participantCode.text;

        participantCode.text = selectedParticipant;

        int nextSessionNumber = im.fileManager.CurrentSession(selectedParticipant);

        session.text = nextSessionNumber.ToString();
    }

   public void SetScene()
    {
        if (im == null || sceneSelection == null || sceneSelection.options.Count == 0)
        {
            return;
        }

        string value = sceneSelection.options[sceneSelection.value].text;
        im.ChangeSetting("experimentScene", value);
    }

    public bool SetParticipantData() {
        if (im == null || im.fileManager == null)
        {
            Debug.LogWarning("Cannot set participant data because startup references are not ready.");
            return false;
        }

        // Get participant code
        // get session number
        string participant = participantCode != null ? participantCode.text : im.GetSetting<string>("participantCode");
        string sessionText = session != null ? session.text : im.GetSetting<string>("session");

        if (string.IsNullOrEmpty(participant))
        {
            participant = "U001";
        }

        if (string.IsNullOrEmpty(sessionText))
        {
            sessionText = "0";
        }

        int sessionNum;
        if(im.fileManager.isValidParticipant(participant) && int.TryParse(sessionText, out sessionNum)) {

            im.ChangeSetting("participantCode", participant);
            im.ChangeSetting("session", sessionText);

            return true;
        }
        return false;
    }

    public void LoadTutorial()
    {
        Debug.Log("[GemMine] Detected click: Start Tutorial");

        if (im == null)
        {
            Debug.LogWarning("[GemMine] LoadTutorial: InterfaceManager is null — cannot proceed.");
            return;
        }

        Debug.Log("[GemMine] LoadTutorial: validating participant data...");
        bool dataOk = SetParticipantData();
        Debug.Log("[GemMine] LoadTutorial: SetParticipantData() = " + dataOk);

        if (dataOk) {
            string tutorialScene = im.GetSetting<string>("tutorialScene");
            Debug.Log("[GemMine] LoadTutorial: tutorialScene = '" + tutorialScene + "'");
            im.ChangeSetting("sceneToLaunch", tutorialScene);
            Debug.Log("[GemMine] LoadTutorial: calling im.LaunchExperiment()...");
            im.LaunchExperiment();
        }
        else {
            Debug.LogWarning("[GemMine] LoadTutorial: participant data invalid — showing warning.");
            im.Do(new EventBase<string, int>(im.ShowWarning, "Please set participant code and session", 5000));
        }
    }

    public void LoadExperiment()
    {
        if (im == null)
        {
            Debug.LogWarning("Cannot load experiment because InterfaceManager is not ready.");
            return;
        }

        if(SetParticipantData()) {
            im.ChangeSetting("sceneToLaunch", im.GetSetting<string>("experimentScene"));
            //ShowConfirmation();
            im.LaunchExperiment();
        }
        else {
            im.Do(new EventBase<string, int>(im.ShowWarning, "Please set participant code and session", 5000));
        }
    }

    private void ShowConfirmation() {
      if (startCanvas != null)
          startCanvas.SetActive(false);
      if (confirmationCanvas != null)
          confirmationCanvas.SetActive(true);
    }

    private void ShowStart() {
      if (titleSong)
      {
          AudioSource.PlayClipAtPoint(titleSong, gameObject.transform.position, 1f);
      }

      if (startCanvas != null)
          startCanvas.SetActive(true);
      if (confirmationCanvas != null)
          confirmationCanvas.SetActive(false);
    }

    public void Quit() {
      if (im != null)
          im.Pause();
    }

    public void Continue() {
      ShowStart();
    }
}
