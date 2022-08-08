using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class InterfaceManager : MonoBehaviour {

    const string SYSTEM_CONFIG = "config.json";    
    private static string pauseKey = "p"; // p to pause

    //////////
    // Singleton Boilerplate
    // makes sure that only one Experiment Manager
    // can exist in a scene and that this object
    // is not destroyed when changing scenes
    //////////

    private static InterfaceManager _instance;

    public static InterfaceManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            throw new System.InvalidOperationException("Cannot create multiple InterfaceManager Objects");
        } 
        else {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            DontDestroyOnLoad(pauseCanvas);
            DontDestroyOnLoad(quitCanvas);
            DontDestroyOnLoad(warningCanvas);
        }
    }

    //////////
    // Non-unity event handling for scripts to 
    // activate InterfaceManager functions
    //////////
    private EventQueue mainEvents = new EventQueue();

    private bool paused = false;
    private CursorLockMode cursorLockState;

    // queue to store key handlers before key event
    private ConcurrentQueue<Action<string, bool>> onKey;

    //////////
    // Experiment Settings and Experiment object
    // that is instantiated once launch is called
    ////////// 
    // global random number source
    public static System.Random rnd = new System.Random();

    // system configurations, generated on the fly by
    // FlexibleConfig
    public JObject systemConfig = null;
    public JObject experimentConfig = null;


    // FIXME
    // dummy code to appease imports on unused scripts
    public FileManager fileManager;
    public ScriptedEventReporter scriptedInput;
    public UpdatedWriteToDiskHandler writeToDiskHandler;
    public GameManager game = null;

    public GameObject warningCanvas;
    public GameObject pauseCanvas;
    public GameObject quitCanvas;

    public NonUnitySyncbox syncBox;

    private int eventsPerFrame = 5;

    //public 
    public InterfaceManager() {}

    public void Update() {
         if(Input.GetKeyDown(pauseKey)) {
            Pause();
        }

        int i = 0;
        while(mainEvents.Process() && (i < eventsPerFrame)) {
            i++;
        }
    }

    public void Start() {
        LockCursor(CursorLockMode.None);

        warningCanvas.SetActive(false);
        pauseCanvas.SetActive(false);
        quitCanvas.SetActive(false);

        fileManager = new FileManager(this);
        onKey = new ConcurrentQueue<Action<string, bool>>();

        fileManager = new FileManager(this);
        SceneManager.sceneLoaded += onSceneLoaded;

        string text = System.IO.File.ReadAllText(System.IO.Path.Combine(fileManager.ConfigPath(), SYSTEM_CONFIG));
        systemConfig = FlexibleConfig.LoadFromText(text);

        // Get all configuration files
        string configPath = fileManager.ConfigPath();
        string[] configs = Directory.GetFiles(configPath, "*.json");
        if(configs.Length < 2) {
            ShowWarning("Configuration File Error", 5000);
            DoIn(new EventBase(Quit), 5000);            
        }

        JArray exps = new JArray();

        for(int i=0; i<configs.Length; i++) {
            Debug.Log(configs[i]);
            if(!configs[i].Contains(SYSTEM_CONFIG))
                exps.Add(Path.GetFileNameWithoutExtension(configs[i]));
        }
        Debug.Log("changing setting");
        ChangeSetting("availableExperiments", exps);

        Debug.Log("checkpoint");

        Debug.Log("launch launcher");

        // Configure sync pulses
        switch ((string)GetSetting("syncBox"))
        {
            case "none":
                break;
            case "ucla":
                Debug.Log("found ucla syncbox");
                syncBox = new UCLASyncbox(this);
                syncBox.Init();
                break; // add ucla synching later!
            case "cml":
                syncBox = new UPennSyncbox(this);
                syncBox.Init();
                break;
            default:
                break; // could add error checking here
        }

        // Start experiment Launcher scene
        mainEvents.Do(new EventBase(LaunchLauncher));
        eventsPerFrame = (int)(GetSetting("eventsPerFrame") ?? 5);
    }

    void OnDisable()
    {
        if (syncBox.Running())
        {
            syncBox.Do(new EventBase(syncBox.StopPulse));
        }
    }

    //////////
    // Function that provides a clean interface for accessing
    // experiment and system settings. Settings in experiment
    // override those in system. Attempts to read non-existent
    // settings return null.
    //////////

    public dynamic GetSetting(string setting) {
        JToken value = null;

        if(experimentConfig != null) {
            if(experimentConfig.TryGetValue(setting, out value)) {
                if(value != null) {
                    return value;
                }
            }
        }

        if(systemConfig != null) {
            if(systemConfig.TryGetValue(setting, out value)) {
                return value;
            }
        }

        return null;
    }

    // returns true if value updated, false if new value added
    public bool ChangeSetting(string setting, dynamic value) {
        JToken existing = GetSetting(setting);
        if(existing == null) {

            // even if setting belongs to systemConfig, experimentConfig setting overrides
            if(experimentConfig == null) {
                (systemConfig).Add(setting, value);
            }
            else {
                (experimentConfig).Add(setting, value);
            }
            return false;
        }
        else {
            // even if setting belongs to systemConfig, experimentConfig setting overrides
            if(experimentConfig == null) {
                (systemConfig)[setting] = value;
            }
            else {
                (experimentConfig)[setting] = value;
            }
            return true;
        }
    }

    public void onSceneLoaded(Scene scene, LoadSceneMode mode) {
        GameObject _game =  GameObject.Find("GameManager");
        if(_game != null) {
            game = _game.GetComponent<GameManager>();
            Debug.Log("found game manager");
        }

        GameObject _events = GameObject.Find("EventReporters");
        if (_events != null)
        {
            scriptedInput = _events.GetComponent<ScriptedEventReporter>();
            Debug.Log("found scripted event reporter");

            //writeToDiskHandler = (UpdatedWriteToDiskHandler)eventReporters.GetComponent("UpdatedWriteToDiskHandler");
            writeToDiskHandler = _events.GetComponent<UpdatedWriteToDiskHandler>();
            Debug.Log("found updated write to disk handler");
        }

        mainEvents.Pause(false);
    }

    public void TestSyncbox(Action callback) {
        syncBox.Do(new EventBase(syncBox.StartPulse));
        DoIn(new EventBase(syncBox.StopPulse), (int)GetSetting("syncBoxTestLength"));
        DoIn(new EventBase(callback), (int)GetSetting("syncBoxTestLength"));
    }

    public void LaunchLauncher() {
        mainEvents.Pause(true);
        Debug.Log("Launching: " + (string)GetSetting("sceneToLaunch"));

        LoadExperimentConfig("Goldmine");
        ChangeSetting("participantCode", "U001");
        ChangeSetting("session", 0);

        //SceneManager.LoadScene("Scenes/"+ (string)GetSetting("launcherScene"));
        LaunchExperiment();
    }

    public void LaunchExperiment() {
        // launch scene with exp, 
        // instantiate experiment,
        // call start function

        // Check if settings are loaded
        if(experimentConfig != null) {

            mainEvents.Pause(true);

            SceneManager.LoadScene((string)GetSetting("sceneToLaunch"));

            Cursor.visible = false;
            LockCursor(CursorLockMode.Locked);

            Application.runInBackground = true;

            // Make the game run as fast as possible
            QualitySettings.vSyncCount = 1;//(int)GetSetting("vSync");
            Application.targetFrameRate = 60;//(int)GetSetting("frameRate");

            // create path for current participant/session
            fileManager.CreateSession();

            // Start syncbox
            if (syncBox != null) 
            {
                if (!syncBox.IsRunning())
                {
                    syncBox.Do(new EventBase(syncBox.StartPulse));
                }
            }

            // won't execute until mgr is ready
            //Do(new EventBase(LogExperimentInfo));
        }
        else {
            throw new Exception("No experiment configuration loaded");
        }
    }

    public void LaunchScene(string scene) {
        //loggable
        mainEvents.Pause(true);
        SceneManager.LoadScene(scene); 
    }

    public void LoadExperimentConfig(string name) {
        string text = System.IO.File.ReadAllText(System.IO.Path.Combine(fileManager.ConfigPath(), name + ".json"));
        experimentConfig = FlexibleConfig.LoadFromText(text); 
    }

    public void ShowWarning(string warnMsg, int duration) {
        warningCanvas.SetActive(true);
        if (scriptedInput)
        {
            scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "WarningCanvas" }, { "isActive", true } });
        }
        TextDisplayer warnText = warningCanvas.GetComponent<TextDisplayer>();
        warnText.DisplayText("warning", warnMsg);

        DoIn(new EventBase(() => { warnText.ClearText();
                                   warningCanvas.SetActive(false);
                                   scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "WarningCanvas" }, { "isActive", false } }); }), duration);
    }


    public void Pause() {
        paused = mainEvents.Running();
        if (scriptedInput)
        {
            scriptedInput.ReportScriptedEvent("gamePaused", new Dictionary<string, object> { { "isPaused", paused }, { "pauseType", "manualPause" } });
        }

        Debug.Log(paused);
        mainEvents.Pause(paused);

        pauseCanvas.SetActive(paused);
        quitCanvas.SetActive(false);

        if (scriptedInput)
        {
            scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "PauseCanvas" }, { "isActive", paused } });
            scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "QuitCanvas" }, { "isActive", false } });
        }

        if (paused)
        {
            cursorLockState = Cursor.lockState;
            LockCursor(CursorLockMode.None);
        }
        else
        {
            LockCursor(cursorLockState);
        }

        if(game != null) {
            game.Pause(paused);
        }
    }

    public void PauseToQuit()
    {
        pauseCanvas.SetActive(false);
        quitCanvas.SetActive(true);

        if (scriptedInput)
        {
            scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "PauseCanvas" }, { "isActive", false } });
            scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "QuitCanvas" }, { "isActive", true } });
        }
    }

    public void LockCursor(CursorLockMode isLocked)
    {
        Cursor.lockState = isLocked;
        if (isLocked == CursorLockMode.None)
        {
            Cursor.visible = true;
        }
        else{
            Cursor.visible = false;
        }
    }

    public void Quit() 
    {
        if (scriptedInput)
        {
            Debug.Log("Quitting");
            scriptedInput.ReportScriptedEvent("quitExperiment", new Dictionary<string, object> { { "quitExperiment", true } });
            Invoke("DoDoWrite", 0);
        }
        syncBox.Close();
        Invoke("DoQuit", 0.5f);
    }

    public void DoDoWrite()
    {
        if (writeToDiskHandler)
        {
            writeToDiskHandler.DoWrite();
        }
    }

	public void DoQuit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    //////////
    // Key handling code that receives key inputs from
    // an external script and modifies program behavior
    // accordingly
    //////////
    
    public void Key(string key, bool pressed) {
        if(paused || key == pauseKey) {
            return;
        }

        Action<string, bool> action;
        while(onKey.Count != 0) {
            if(onKey.TryDequeue(out action)) {
                Do(new EventBase<string, bool>(action, key, pressed));
            }
        }
    }

    public void RegisterKeyHandler(Action<string, bool> handler) {
        Debug.Log("Registered Key");
        onKey.Enqueue(handler);
    }

    //////////
    // Wrappers to make event management API consistent
    //////////

    public void Do(EventBase thisEvent) {
        mainEvents.Do(thisEvent);
    }

    public void DoIn(EventBase thisEvent, int delay) {
        mainEvents.DoIn(thisEvent, delay);
    }

    public void DoRepeating(RepeatingEvent thisEvent) {
        mainEvents.DoRepeating(thisEvent);
    }
}

public class FileManager {

    InterfaceManager manager;

    public FileManager(InterfaceManager _manager) {
        manager = _manager;
    }

    public virtual string ExperimentRoot() {

        #if UNITY_EDITOR
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        #else
            return System.IO.Path.GetFullPath(".");
        #endif
    }

    public string ExperimentPath() {
        string root = ExperimentRoot();
        string dir = System.IO.Path.Combine(root, "data", (string)manager.GetSetting("experimentName"));
        return dir;
    }
    public string ParticipantPath(string participant) {
        string dir = ExperimentPath();
        dir = System.IO.Path.Combine(dir, participant);
        return dir;
    }

    public string ParticipantPath() {
        string dir = ExperimentPath();
        string participant = (string)manager.GetSetting("participantCode");

        if(participant == null) {
            throw new Exception("No participant selected");
        }

        dir = System.IO.Path.Combine(dir, participant);
        return dir;
    }
    
    public string SessionPath(string participant, int session) {
        string dir = ParticipantPath(participant);
        dir = System.IO.Path.Combine(dir, session.ToString());
        return dir;
    }

    public string SessionPath() {
        string session = (string)manager.GetSetting("session");
        if(session == null) {
            return null;
        }
        string dir = ParticipantPath();
        dir = System.IO.Path.Combine(dir, session.ToString());
        return dir;
    }

    public bool isValidParticipant(string code) {
        return true;
        //if((bool)manager.GetSetting("isTest")) {
        //    return true;
        //}

        //if((string)manager.GetSetting("experimentName") == null) {
        //    return false;
        //}

        //return !String.IsNullOrEmpty(code);
        //Regex rx = new Regex(@"^" + (string)manager.GetSetting("prefix") + @"\d{1,4}$");
        //return rx.IsMatch(code);
    }

    public void CreateSession() {
        Directory.CreateDirectory(SessionPath());
    }

    public void CreateParticipant() {
        Directory.CreateDirectory(ParticipantPath());
    }
    public void CreateExperiment() {
        Directory.CreateDirectory(ExperimentPath());
    }

    public string ConfigPath() {
        string root = ExperimentRoot();
        return System.IO.Path.Combine(root, "configs");
    }

    public int CurrentSession(string participant) {
        int nextSessionNumber = 0;
        while (System.IO.Directory.Exists(manager.fileManager.SessionPath(participant, nextSessionNumber)))
        {
            nextSessionNumber++;
        }
        return nextSessionNumber;
    }
}
