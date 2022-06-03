using System;
using System.Dynamic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Threading;

public class GameManager : MonoBehaviour
{
    // Version number
    const string TASK_VERSION = "Goldmine-2.0.0";

    // Singleton Boilerplate
    public static GameManager gm;
    protected InterfaceManager im;

    // External Compile Time Collected Game Objects
    public GameObject player; // the player
    public GameObject playerAnimationSpawnPoint; // spawn point for animations that appear on the ground in front of the player
    public GameObject digCrosshair; // object with the dig crosshair image
    public GameObject spawner; // the item spawner
    public GameObject mineBase; // base where the player spawns
    public GameObject mainCanvas; // the main canvas on which text is displayed
    public GameObject itemFoundEffect; // particle system that plays when player digs an item is found
    public GameObject itemNotFoundEffect; // particle system that plays when player digs an item is not found
    public GameObject timelineCanvas;
    public AudioClip pointGainSFX; // sound that plays when points are added
    public AudioClip pointLossSFX; // sound that plays when points are subtracted

    // TODO: move to config file?
    public int itemsToFind = 4; // how many items are placed in the environment
    public int delayDuration = 10000; // duration of the delay phases
    public int timelineDuration = 18000; // duration of the timeline phase
    public int timelineScoreDuration = 2000; // duration of the timeline score display
    public int taskDuration = 30000; // duration of the task phases (encoding, retrieval)
    public int returnToBasePenalty = -5; // penalty for not eturning to the base in time
    public int wrongDigPenalty = -2; // penalty for digging in the wrong place
    public int goldFoundReward = 10; // points for each gold piece found
    public int gemFoundReward = 10; // points for each gem found
    public int correctTimelineReward = 10; // points for each item correctly placed on timeline
    public int wrongTimelinePenalty = -2; // penalty for each item incorrectly placed on timeline or items not placed on timeline when they should be
    public float maxDigDistance = 4f; // max distance player can dig from items to get points
    public int eventsPerFrame = 5;
    public bool playerActive = false; // whether the player is in an active task state or not
    // HUD text displays
    public Text timerDisplay; // text that says how much time is left in the current game phase
    public Text trialDisplay; // text that says how many trials have elapsed

    // External Runtime Collected Game Objects
    protected ControlPlayer controlPlayer;
    protected SpawnItems spawnItems;
    protected ControlBase controlBase;
    protected ControlCanvas controlMainCanvas;
    protected ControlTimeline controlTimeline;

    private GameObject minDistanceItem;
    private AudioSource pickupAudioSource;
    private AudioSource digAudioSource;
    //private WorldDataReporter baseReporter;
    private bool[] pastTrialPerformance = { false, false }; // player performance info over the past two trials
    private bool lastActiveState;

    public EventQueue gameEvents; // We want this class to inherit from game object, since we have so
                                  // many objects to keep track of, and multiple inheritance isn't 
                                  // allowed, so use composition instead

    protected Dictionary<string, List<Action>> stateMachine;

    protected dynamic state;



    public enum ItemType {
        gold,
        gems
    }
    protected ItemType itemType = ItemType.gems;
    public string GetItemTypeStr() { return itemType == ItemType.gold ? "gold" : "items"; }
    //public string GetItemTypeStr() { return Enum.GetName(itemType.GetType(), itemType); }

    public static bool timelineSystemEnabled { get; private set; } = true;
    public static bool pickupSystemEnabled { get; private set; } = true;
    public static bool timedTrialSystemEnabled { get; private set; } = false;
    public static bool scaleDifficultySystem { get; private set; } = false;


    protected void Awake()
    {
        // get references to game objects for access by this and other objects
        CollectReferences();
    }

    protected virtual void Start() {
        gameEvents = new EventQueue();
        stateMachine = new Dictionary<string, List<Action>>();
        state = new ExpandoObject();

        // set up initial game state
        state.runIndex = 0;
        state.trialsCompleted = 0;
        state.isTimedTrial = false;
        state.doorIndex = 0;
        state.digEnabled = false;
        state.pickupEnabled = false;
        state.paused = false;
        state.showCountdown = false;
        state.controlsFrozen = true;
        state.score = 0; // starting score
        state.itemsFoundLastTrial = 0; // how many gold items were found on the most recent trial
        state.itemsFoundTotal = 0; // how many gold items were found across all trials
        state.itemsSpawnedTotal = 0; // how many gold items were spawned across all trials
        state.pickupsAttempted = 0; // how many times the player tried to pickup an item
        state.digsAttempted = 0; // how many times the player has dug for an item

        // Report version info
        im.scriptedInput.ReportScriptedEvent("versions", new Dictionary<string, object> { {"taskVersion", TASK_VERSION} });

        gameEvents.Pause(false);
    }

    protected void Update() {
        int i = 0;
        while(gameEvents.Process() && (i < eventsPerFrame)) {
            i++;
        }

        // Track how much time is left in the current game state
        if(state.showCountdown) {
            state.timeLeft -= Time.deltaTime;
            timerDisplay.text = state.timeLeft.ToString("0.00"); 
        }

        // Track whether the player is in an active game state
        //baseReporter.reportView = playerActive;

        // See if we need to toggle the door open/close states
        if (playerActive)
        {
            if ((controlPlayer.playerInMine) && (!controlBase.allDoorsOpen))
            {
                // Open all doors
                controlBase.OpenDoors(new bool[] { true, true, true });
            }
            else if ((controlPlayer.playerAtBase) && (controlBase.allDoorsOpen))
            {
                // Open the trial door
                bool[] iDoors = { false, false, false };
                iDoors[state.doorIndex] = true;
                controlBase.OpenDoors(iDoors);
            }
        }
    }

    public void Run() {
        if(state.runIndex >= stateMachine["Run"].Count) {
            return;
        }

        stateMachine["Run"][state.runIndex].Invoke();
    }

    public virtual Action RunIndexWrapper(Action todo) {
        return () => { state.runIndex++;
                       todo(); };
    }

    public Action ConditionalAction(bool condition, Action todo) {
        if (condition)
        {
            return todo;
        }
        else
        {
            return () => {
                state.runIndex++;
                gameEvents.Do(new EventBase(Run));
            };
        }
    }

    public List<Action> ConditionalActions(bool condition, List<Action> todos)
    {
        if (condition)
        {
            return todos;
        }
        else // NOP
        {
            return new List<Action> {
                () => {
                    state.runIndex++;
                    gameEvents.Do(new EventBase(Run));},
            };
        }
    }

    public void CollectReferences() {
        if (gm == null)
        {
            gm = gameObject.GetComponent<GameManager>();
        }

        
        GameObject mgr = GameObject.Find("InterfaceManager");
        if (mgr) 
        {
            im = (InterfaceManager)mgr.GetComponent("InterfaceManager");
        }

        // Get quick access to other object functions
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        controlPlayer = player.GetComponent<ControlPlayer>();
        spawnItems = spawner.GetComponent<SpawnItems>();
        controlBase = mineBase.GetComponent<ControlBase>();
        controlMainCanvas = mainCanvas.GetComponent<ControlCanvas>();
        pickupAudioSource = gameObject.GetComponents<AudioSource>()[0];
        digAudioSource = gameObject.GetComponents<AudioSource>()[1];
        //baseReporter = mineBase.GetComponent<WorldDataReporter>();
        controlTimeline = timelineCanvas.transform.Find("Timeline").GetComponent<ControlTimeline>();
    }

    // Actions that occur at the beginning of a trial
    protected void InitTrial() {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "InitTrial" } });

        // Reset the player
        FreezeAtBase();

        // Reset displays
        controlMainCanvas.ResetCentralDisplay();
        
        // Recent trial-wise gold found count
        state.itemsFoundLastTrial = 0;

        gameEvents.Do(new EventBase(Run));
    }
   
    // Execute the pre-encoding delay message 
    protected void PreEncodingDelayMsg()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "PreEncodingDelayMsg" } });

        // Update canvas display
        controlMainCanvas.ShowBackground(2f);
        switch (itemType)
        {
            case ItemType.gold:
                controlMainCanvas.SetCentralDisplay2("Get ready to\nsearch for gold", "default", 2f);
                break;
            case ItemType.gems:
                controlMainCanvas.SetCentralDisplay2("Get ready to\nsearch for items", "default", 2f);
                break;
        }
        

        gameEvents.DoIn(new EventBase(Run), 2000);
    }

    // Execute the pre-encoding message 
    protected void PreTimelineMsg()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "PreTimelineMsg" } });

        // Update canvas display
        controlMainCanvas.ShowBackground(2f);
        switch (itemType)
        {
            case ItemType.gold:
                controlMainCanvas.SetCentralDisplay2("Get ready to\nplace gold on timeline", "default", 2f);
                break;
            case ItemType.gems:
                controlMainCanvas.SetCentralDisplay2("Get ready to\nplace items on timeline", "default", 2f);
                break;
        }
        

        gameEvents.DoIn(new EventBase(Run), 2000);
    }

    // Execute the pre-retrieval delay message
    protected void PreRetrievalDelayMsg()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "PreRetrievalDelayMsg" } });

        // Reset the player
        FreezeAtBase();

        // Reset displays
        controlMainCanvas.ResetCentralDisplay();

        // Update canvas display
        controlMainCanvas.ShowBackground(2f);
        switch (itemType)
        {
            case ItemType.gold:
                controlMainCanvas.SetCentralDisplay2("Visualize a path\nto the gold", "default", 2f);
                break;
            case ItemType.gems:
                controlMainCanvas.SetCentralDisplay2("Visualize a path\nto the items", "default", 2f);
                break;
        }
        

        gameEvents.DoIn(new EventBase(Run), 2000);
    }

   // Execute the delay interval 
    protected void Delay() {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "Delay" } });

        FreezeAtBase();
        gameEvents.DoIn(new EventBase(Run), delayDuration);
    }
    
    
    protected void FreezeAtBase() {
        playerActive = false;

        // Respawn and freeze the player
        controlPlayer.Respawn();
        controlPlayer.Freeze(true);

        // Close all doors
        controlBase.OpenDoors(new bool[] {false, false, false});

        // Update canvas displays
        controlMainCanvas.SetTaskDirectionsDisplay("WAIT");
        controlMainCanvas.SetTimedTrialDisplay("");
    }
    
    // Execute the encoding interval
    protected void Encoding() {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "Encoding" } });

        playerActive = true;

        // Show the dig crosshair
        if (pickupSystemEnabled)
        {
            state.pickupEnabled = true;
            digCrosshair.SetActive(true);
        }

        // Unfreeze the player
        controlPlayer.Freeze(false);

        // Determine if the current trial is timed
        im.scriptedInput.ReportScriptedEvent("timedTrial", new Dictionary<string, object> { { "isTimedTrial", state.isTimedTrial } });

        // Open one door at random
        bool[] iDoors = {false, false, false};
        iDoors[state.doorIndex] = true;
        controlBase.OpenDoors(iDoors, true, true);

        // Spawn items in the environment
        switch (itemType)
        {
            case ItemType.gold:
                spawnItems.SpawnGold(itemsToFind);
                break;
            case ItemType.gems:
                spawnItems.SpawnGems(itemsToFind);
                break;
        }

        // Update canvas displays
        string itemTypeStr = GetItemTypeStr();
        if (pickupSystemEnabled)
        {
            controlMainCanvas.SetTopDisplay("PICK UP" + itemTypeStr.ToUpper(), "default", 0.75f);
            controlMainCanvas.SetTaskDirectionsDisplay("PICK UP " + itemTypeStr.ToUpper() + ": " + itemsToFind.ToString() + " LEFT");
        }
        else
        {
            controlMainCanvas.SetTopDisplay("FIND " + itemsToFind.ToString() + " " + itemTypeStr.ToUpper(), "default", 0.75f);
            controlMainCanvas.SetTaskDirectionsDisplay("FIND " + itemsToFind.ToString() + " " + itemTypeStr.ToUpper());
        }
        
        if (state.isTimedTrial)
        {
            controlMainCanvas.SetTimedTrialDisplay("TIME PENALTY", "negative");
            controlMainCanvas.SetBottomDisplay("TIME PENALTY", "negative", 0.75f);
        }
        //else
        //{
        //    controlMainCanvas.SetTimedTrialDisplay("NO TIME PENALTY", "positive");
        //}

        // Display countdown
        state.timeLeft = taskDuration;
        state.showCountdown = true;

        gameEvents.DoIn(new EventBase(Run), taskDuration);
    }

    // Timeline
    protected void Timeline()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "Timeline" } });

        // Reset the player
        FreezeAtBase();

        // Show the timeline
        timelineCanvas.SetActive(true);
        // The following two lines are a hack because unity wasn't displaying the camera correctly
        timelineCanvas.GetComponent<Canvas>().worldCamera.enabled = false;
        timelineCanvas.GetComponent<Canvas>().worldCamera.enabled = true;

        // Spawn the timeline items
        switch (itemType)
        {
            case ItemType.gold:
                controlTimeline.SpawnTimelineItems(spawnItems.goldObject, 8);
                break;
            case ItemType.gems:
                controlTimeline.SpawnTimelineItems(spawnItems.gemObjects);
                break;
        }

        // Unlock the mouse
        im.LockCursor(CursorLockMode.None);

        // Update canvas displays
        string itemTypeStr = GetItemTypeStr();
        controlMainCanvas.SetTaskDirectionsDisplay("PLACE " + itemTypeStr.ToUpper() + " ON TIMELINE");

        // Display countdown
        state.timeLeft = taskDuration;
        state.showCountdown = true;

        //gameEvents.DoIn(new EventBase(
        //    () => {
        //        TimelineEnd();
        //        Run();
        //    }),
        //    timelineDuration);
        gameEvents.DoIn(new EventBase(Run), timelineDuration);
    }

    protected void TimelineEnd()
    {
        // Report item times
        var timelineItems = controlTimeline.GetItemTimes(timelineDuration / 1000);
        im.scriptedInput.ReportScriptedEvent("timeline", new Dictionary<string, object> { { "items", timelineItems } });
        //Debug.Log(JsonConvert.SerializeObject(new Dictionary<string, object> { { "items", timelineItems } }));

        // Update the score
        var spawnedItems = spawnItems.GetItems();
        // TODO: JPB: (bug) Change this to handle more than gem objects
        //                  There would be a bug in the gold version for points
        // TODO: JPB: (feature) Add scoring for how close item is to actual time
        //                      +5 on timeline, +1 to +5 for closeness, -2 not on timeline, -2 incorrect on timeline
        // TODO: JPB: (feature) Make score puff up after timeline
        // Note: make sure changes here happen in TutorialTimelineEnd too
        int scoreDelta = 0;
        foreach (var item in spawnItems.gemObjects)
        {
            bool isItemInTimeline = timelineItems.Any(x => (string)x["name"] == item.name);
            bool isItemSpawned = spawnedItems.Any(x => x.name == item.name);

            if (isItemSpawned && isItemInTimeline)
            {
                // Item correctly placed on timeline
                scoreDelta += correctTimelineReward;
            }
            else if (!isItemSpawned && isItemInTimeline)
            {
                // Item incorrectly placed on timeline
                scoreDelta += wrongTimelinePenalty;
            }
            else if (isItemSpawned && !isItemInTimeline)
            {
                // Item not placed on timeline when it should be
                scoreDelta += wrongTimelinePenalty;
            }
        }
        UpdateScore(scoreDelta);

        // Lock the mouse
        im.LockCursor(CursorLockMode.Locked);

        gameEvents.DoIn(new EventBase(
            () => {
                // Delete timeline items
                foreach (var item in controlTimeline.GetTimelineItems())
                {
                    Destroy(item);
                }

                // Hide the timeline 
                timelineCanvas.SetActive(false);
                Run();
            }),
            timelineScoreDuration);
    }

    // Execute the retrieval interval
    protected void Retrieval() {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "Retrieval" } });

        playerActive = true;
        state.digEnabled = true;

        // Unfreeze the player
        controlPlayer.Freeze(false);

        // Show the dig crosshair
        digCrosshair.SetActive(true);

        // Open one door at random
        bool[] iDoors = { false, false, false };
        iDoors[state.doorIndex] = true;
        controlBase.OpenDoors(iDoors, true, true);

        // Update canvas displays
        string itemTypeStr = GetItemTypeStr();
        controlMainCanvas.SetTopDisplay("DIG FOR " + itemTypeStr.ToUpper(), "default", 0.75f);
        controlMainCanvas.SetTaskDirectionsDisplay("DIG FOR " + itemTypeStr.ToUpper() + ": " + itemsToFind.ToString() + " LEFT");
        
        if (state.isTimedTrial)
        {
            controlMainCanvas.SetTimedTrialDisplay("TIME PENALTY", "negative");
            controlMainCanvas.SetBottomDisplay("TIME PENALTY", "negative", 0.75f);
        }
        //else
        //{
        //    controlMainCanvas.SetTimedTrialDisplay("NO TIME PENALTY", "positive");
        //}

        gameEvents.DoIn(new EventBase(Run), taskDuration);
    }
    
    // Execute the return to base interval
    protected void ReturnToBase() {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "ReturnToBase" } });

        playerActive = true;

        // Find and hide gold pieces in the environment
        spawnItems.HideItems();
        state.pickupEnabled = false;
        state.digEnabled = false;
        digCrosshair.SetActive(false);

        if (controlPlayer.playerInMine)
        {
            controlMainCanvas.SetTaskDirectionsDisplay("RETURN TO BASE");
            controlMainCanvas.SetCentralDisplay("RETURN TO BASE", "default", 1.5f);
            if (state.isTimedTrial)
            {
                UpdateScore(returnToBasePenalty);
            }
        }
        gameEvents.Do(new EventBase(Run));
    }

    // Actions that occur at the end of a trial
    protected void EndOfTrial() {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "EndOfTrial" } });

        // Find and destroy gold pieces in the environment
        spawnItems.DestroyItems();

        // Update trial tracking info
        state.trialsCompleted++;
        im.scriptedInput.ReportScriptedEvent("trialComplete", new Dictionary<string, object> { { "trialsCompleted", state.trialsCompleted } });
        trialDisplay.text = "TRIAL " + (state.trialsCompleted + 1).ToString();
        state.itemsFoundTotal += state.itemsFoundLastTrial;
        state.itemsSpawnedTotal += itemsToFind;

        // Update performance tracker over the past 2 trials
        pastTrialPerformance[0] = pastTrialPerformance[1];
        if (state.itemsFoundLastTrial == itemsToFind)
        {
            pastTrialPerformance[1] = true;
        }
        else
        {
            pastTrialPerformance[1] = false;
        }

        // Decide how many items will be spawned on the next trial
        if (scaleDifficultySystem)
        {
            if ((pastTrialPerformance[0]) & (pastTrialPerformance[1]))
            {
                itemsToFind++;
            }
            else if ((!pastTrialPerformance[0]) & (!pastTrialPerformance[1]) & (itemsToFind > 1))
            {
                itemsToFind--;
            }
        }
        
        gameEvents.Do(new EventBase(Run));
    }

    // Notify player that task period will end shortly
    public void TimeLeftWarning() {
        // Log
        im.scriptedInput.ReportScriptedEvent("timeLeftWarning", new Dictionary<string, object>());

        // Open all doors
        controlBase.OpenDoors(new bool[] {true, true, true});
        
        // Warn player how much time is left
        controlMainCanvas.SetCentralDisplay("FIVE SECONDS LEFT", "default", 2f);
    }

    public void Pause(bool pause) {
        // Log
        //im.scriptedInput.ReportScriptedEvent("gamePaused", new Dictionary<string, object> { { "isPaused", pause }, {"pauseType", "withinGame"} });
        if (pause)
        {
            lastActiveState = playerActive;
            playerActive = false;
        }
        else
        {
            playerActive = lastActiveState;
        }
        state.paused = pause; 
        gameEvents.Pause(pause);
        controlPlayer.Pause(pause);
    }
    
    public void AnyKey(string key, bool down) {
        if(down) {
            gameEvents.Do(new EventBase(Run));
        }
        else  {
            im.RegisterKeyHandler(AnyKey);
        }
    }
    
    public void PressSpace(string key, bool down) {
        key = key.ToLower();
        
        if ((down) && (key=="space")) {
            Pause(false);
            gameEvents.Do(new EventBase(Run));
        }
        else  {
            im.RegisterKeyHandler(PressSpace);
        }
    }

    // Perform a pickup action (during encoding period only)
    public void PickupItem() {
        if (!state.pickupEnabled || !pickupSystemEnabled)
        {
            return;
        }

        float minDistance = float.MaxValue;

        // Register a dig
        state.pickupsAttempted++;

        // Play the dig sound
        // TODO: JPB: Change pickupAudio Source in unity GameManager
        if (pickupAudioSource)
        {
            pickupAudioSource.Play();
        }

        // Find closest item in the environment
        var items = spawnItems.GetVisibleItems();
        foreach (var item in items)
        {
            float distance = ControlPlayer.EuclideanDistance(digCrosshair.transform, item.transform);
            if (distance < minDistance)
            {
                minDistance = distance;
                minDistanceItem = item;
            }
        }
        string minDistanceItemName = char.ToLowerInvariant(minDistanceItem.name[0]) + minDistanceItem.name.Substring(1);

        // Add or subtract points depending on whether dig was successful
        if (minDistance <= maxDigDistance)
        {
            im.scriptedInput.ReportScriptedEvent("pickup", new Dictionary<string, object> {{"successful", true},
                                                                                           {"distanceFromNearestItem", minDistance},
                                                                                           {"nearestItemPositionX", minDistanceItem.transform.position.x},
                                                                                           {"nearestItemPositionZ", minDistanceItem.transform.position.z},
                                                                                           {"nearestItemName", minDistanceItemName}});
            state.itemsFoundLastTrial++;
            controlMainCanvas.SetTaskDirectionsDisplay("PICK UP " + GetItemTypeStr().ToUpper() + ": " + (items.Length - 1).ToString() + " LEFT");
            //if (itemFoundEffect)
            //{
            //    Instantiate(itemFoundEffect, minDistanceItem.transform.position, Quaternion.identity);
            //}
            spawnItems.HideItem(minDistanceItem);
        }
        else if (items.Count() == 0) // i.e. all items have been dug
        {
            im.scriptedInput.ReportScriptedEvent("pickup", new Dictionary<string, object> {{"successful", false},
                                                                                           {"distanceFromNearestItem", -1}, // these -1s are for finding instances but should be removed from analysis
                                                                                           {"nearestItemPositionX", -1},
                                                                                           {"nearestItemPositionZ", -1},
                                                                                           {"nearestItemName", -1}});
            if (itemNotFoundEffect)
            {
                Vector3 spawnPosition = gameObject.transform.position + new Vector3(0f, -1.18f, 0f);
                Instantiate(itemNotFoundEffect, playerAnimationSpawnPoint.transform.position, Quaternion.identity); // +new Vector3(0f, -1.18f, 1f)
            }
        }
        else
        {
            im.scriptedInput.ReportScriptedEvent("pickup", new Dictionary<string, object> {{"successful", false},
                                                                                           {"distanceFromNearestItem", minDistance},
                                                                                           {"nearestItemPositionX", minDistanceItem.transform.position.x},
                                                                                           {"nearestItemPositionZ", minDistanceItem.transform.position.z},
                                                                                           {"nearestItemName", minDistanceItemName}});
            if (itemNotFoundEffect)
            {
                Vector3 spawnPosition = gameObject.transform.position + new Vector3(0f, -1.18f, 0f);
                Instantiate(itemNotFoundEffect, playerAnimationSpawnPoint.transform.position, Quaternion.identity); // +new Vector3(0f, -1.18f, 1f)
            }
        }
    }

    // Perform a dig action (during retrieval period only)
    public void DigForItem() {
        if (!state.digEnabled)
        {
            return;
        }

        int itemFoundReward = 0;
        switch (itemType)
        {
            case ItemType.gold:
                itemFoundReward = goldFoundReward;
                break;
            case ItemType.gems:
                itemFoundReward = gemFoundReward;
                break;
        }

        float minDistance = float.MaxValue;

        // Register a dig
        state.digsAttempted++;

        // Play the dig sound
        if (digAudioSource)
        {
            digAudioSource.Play();
        }

        // Find closest item in the environment
        var items = spawnItems.GetItems();
        foreach (var item in items)
        {
            float distance = ControlPlayer.EuclideanDistance(digCrosshair.transform, item.transform);
            if (distance < minDistance)
            {
                minDistance = distance;
                minDistanceItem = item;
            }
        }
        string minDistanceItemName = char.ToLowerInvariant(minDistanceItem.name[0]) + minDistanceItem.name.Substring(1);

        // Add or subtract points depending on whether dig was successful
        if (minDistance <= maxDigDistance)
        {
            UpdateScore(itemFoundReward);
            im.scriptedInput.ReportScriptedEvent("dig", new Dictionary<string, object> {{"successful", true},
                                                                                        {"distanceFromNearestItem", minDistance},
                                                                                        {"nearestItemPositionX", minDistanceItem.transform.position.x},
                                                                                        {"nearestItemPositionZ", minDistanceItem.transform.position.z},
                                                                                        {"nearestItemName", minDistanceItemName}});
            state.itemsFoundLastTrial++;
            controlMainCanvas.SetTaskDirectionsDisplay("DIG FOR " + GetItemTypeStr().ToUpper() + ": " + (items.Length - 1).ToString() + " LEFT");
            if (itemFoundEffect)
            {
                Instantiate(itemFoundEffect, minDistanceItem.transform.position, Quaternion.identity);
            }
            Destroy(minDistanceItem);
        }
        else if (items.Count() == 0) // i.e. all items have been dug
        {
            UpdateScore(wrongDigPenalty);
            im.scriptedInput.ReportScriptedEvent("dig", new Dictionary<string, object> {{"successful", false},
                                                                                        {"distanceFromNearestItem", -1}, // these -1s are for finding instances but should be removed from analysis
                                                                                        {"nearestItemPositionX", -1},
                                                                                        {"nearestItemPositionZ", -1},
                                                                                        {"nearestItemName", -1}});
            if (itemNotFoundEffect)
            {
                Vector3 spawnPosition = gameObject.transform.position + new Vector3(0f, -1.18f, 0f);
                Instantiate(itemNotFoundEffect, playerAnimationSpawnPoint.transform.position, Quaternion.identity); // +new Vector3(0f, -1.18f, 1f)
            }
        }
        else
        {
            UpdateScore(wrongDigPenalty);
            im.scriptedInput.ReportScriptedEvent("dig", new Dictionary<string, object> {{"successful", false},
                                                                                        {"distanceFromNearestItem", minDistance},
                                                                                        {"nearestItemPositionX", minDistanceItem.transform.position.x},
                                                                                        {"nearestItemPositionZ", minDistanceItem.transform.position.z},
                                                                                        {"nearestItemName", minDistanceItemName}});
            if (itemNotFoundEffect)
            {
                Vector3 spawnPosition = gameObject.transform.position + new Vector3(0f, -1.18f, 0f);
                Instantiate(itemNotFoundEffect, playerAnimationSpawnPoint.transform.position, Quaternion.identity); // +new Vector3(0f, -1.18f, 1f)
            }
        }
    }
    
    // Update the score and notify player
    public void UpdateScore(int scoreChange) {
        state.score += scoreChange;
        im.scriptedInput.ReportScriptedEvent("score", new Dictionary<string, object> {{"scoreChange", scoreChange}, {"scoreTotal", state.score}});

        if (scoreChange > 0)
        {
            controlMainCanvas.SetScoreDisplay(state.score.ToString(), "positive", 1f);
            if (pointGainSFX)
            {
                AudioSource.PlayClipAtPoint(pointGainSFX, player.transform.position, 0.15f);
            }
        }
        else if (scoreChange < 0)
        {
            controlMainCanvas.SetScoreDisplay(state.score.ToString(), "negative", 1f);
            if (pointLossSFX)
            {
                AudioSource.PlayClipAtPoint(pointLossSFX, player.transform.position, 0.15f);
            }
        }
    }
}

public static class IListExtensions
{

    /// <summary>
    /// Knuth (Fisher-Yates) Shuffle
    /// Shuffles the element order of the specified list.
    /// </summary>
    public static IList<T> Shuffle<T>(this IList<T> list, System.Random rng)
    {
        var count = list.Count;
        for (int i = 0; i < count; ++i)
        {
            int r = rng.Next(i, count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
        return list;
    }
}

public static class CollectionExtensions
{
    /// <summary>
    /// Allows List constructor to take a items or a list of items that gets expanded
    /// https://stackoverflow.com/a/63374611
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="itemsToAdd"></param>
    public static void Add<T>(this ICollection<T> collection, IEnumerable<T> itemsToAdd)
    {
        foreach (var item in itemsToAdd)
        {
            collection.Add(item);
        }
    }
}
