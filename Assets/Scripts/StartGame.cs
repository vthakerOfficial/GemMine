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

        string[] scenes = (string[])im.GetSetting("availableScenes").ToObject<string[]>();

        sceneSelection.AddOptions(new List<string>(scenes));
        SetScene();
    }

    public void Start()
    {
      ShowConfirmation();
    }

    public void LoadParticipant()
    {
        Dropdown dropdown = GetComponent<Dropdown>();
        string selectedParticipant = participantCode.text;

        participantCode.text = selectedParticipant;

        int nextSessionNumber = im.fileManager.CurrentSession(selectedParticipant);

        session.text = nextSessionNumber.ToString();
    }

   public void SetScene()
    {
        string value = sceneSelection.options[sceneSelection.value].text;
        im.ChangeSetting("experimentScene", value);
    }

    public bool SetParticipantData() {
        // Get participant code
        // get session number
        int sessionNum;
        if(im.fileManager.isValidParticipant(participantCode.text) && int.TryParse(session.text, out sessionNum)) {

            im.ChangeSetting("participantCode", participantCode.text);
            im.ChangeSetting("session", session.text);

            return true;
        }
        return false;
    }

    public void LoadTutorial()
    {
        if(SetParticipantData() ) {
            im.ChangeSetting("sceneToLaunch", (string)im.GetSetting("tutorialScene"));
            im.LaunchExperiment();
            //ShowConfirmation();
        }
        else {
            im.Do(new EventBase<string, int>(im.ShowWarning, "Please set participant code and session", 5000));
        }
    }

    public void LoadExperiment()
    {
        if(SetParticipantData()) {
            im.ChangeSetting("sceneToLaunch", (string)im.GetSetting("experimentScene"));
            //ShowConfirmation();
            im.LaunchExperiment();
        }
        else {
            im.Do(new EventBase<string, int>(im.ShowWarning, "Please set participant code and session", 5000));
        }
    }

    private void ShowConfirmation() {
      startCanvas.SetActive(false);
      confirmationCanvas.SetActive(true);
    }

    private void ShowStart() {
      if (titleSong)
      {
          AudioSource.PlayClipAtPoint(titleSong, gameObject.transform.position, 1f);
      }

      startCanvas.SetActive(true);
      confirmationCanvas.SetActive(false);
    }

    public void Quit() {
      im.Pause();
    }

    public void Continue() {
      ShowStart();
    }
}
