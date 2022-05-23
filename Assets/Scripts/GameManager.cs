using System;
using System.Dynamic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

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
    public AudioClip pointGainSFX; // sound that plays when points are added
    public AudioClip pointLossSFX; // sound that plays when points are subtracted

    // TODO: move to config file?
    public int itemsToFind = 1; // how many items are placed in the environment
    public int delayDuration = 10000; // duration of the delay phases
    public int taskDuration = 30000; // duration of the task phases (encoding, retrieval)
    public int returnToBasePenalty = -5; // penalty for not returning to the base in time
    public int wrongDigPenalty = -2; // penalty for digging in the wrong place
    public int goldFoundReward = 10; // points for each gold piece found
    public int gemFoundReward = 10; // points for each gem found
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
    private ItemType itemType = ItemType.gems;
    public bool pickupEnabled { get; private set; } = false;

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
        return () => {state.runIndex++;
                      todo(); };
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
   
    // Execute the delay interval 
    protected void PreEncodingDelayMsg()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "PreEncodingDelayMsg" } });

        // Update canvas display
        controlMainCanvas.ShowBackground(2f);
        controlMainCanvas.SetCentralDisplay2("Get ready to\nsearch for gold", "default", 2f);

        gameEvents.DoIn(new EventBase(Run), 2000);
    }

    // Execute the delay interval 
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
        controlMainCanvas.SetCentralDisplay2("Visualize a path\nto the gold", "default", 2f);

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
        state.pickupEnabled = true;

        // Unfreeze the player
        controlPlayer.Freeze(false);

        // Show the dig crosshair
        digCrosshair.SetActive(true);

        // Determine if the current trial is timed
        im.scriptedInput.ReportScriptedEvent("timedTrial", new Dictionary<string, object> { { "isTimedTrial", state.isTimedTrial } });

        // Open one door at random
        bool[] iDoors = {false, false, false};
        iDoors[state.doorIndex] = true;
        controlBase.OpenDoors(iDoors, true, true);

        // Spawn gold in the environment
        spawnItems.SpawnGold(itemsToFind);

        // Update canvas displays
        string itemTypeStr = Enum.GetName(itemType.GetType(), itemType);
        if (pickupEnabled)
        {
            controlMainCanvas.SetTopDisplay("PICKUP" + itemTypeStr.ToUpper(), "default", 0.75f);
            controlMainCanvas.SetTaskDirectionsDisplay("PICKUP " + itemTypeStr.ToUpper() + ": " + itemsToFind.ToString() + " LEFT");
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

        state.timeLeft = taskDuration;
        state.showCountdown = true;

        gameEvents.DoIn(new EventBase(Run), taskDuration);
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
        string itemTypeStr = Enum.GetName(itemType.GetType(), itemType);
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
        im.scriptedInput.ReportScriptedEvent("trialComplete", new Dictionary<string, object> {{ "trialsCompleted", state.trialsCompleted }});
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
        if ((pastTrialPerformance[0]) & (pastTrialPerformance[1]))
        {
            itemsToFind++;
        }
        else if ((!pastTrialPerformance[0]) & (!pastTrialPerformance[1]) & (itemsToFind > 1))
        {
            itemsToFind--;
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
        if (!state.pickupEnabled)
        {
            return;
        }

        const float invalidDistance = -1;

        float minDistance = invalidDistance;
        float distance;

        // Register a dig
        state.pickupsAttempted++;

        // Play the dig sound
        // TODO: JPB: Change pickupAudio Source in unity GameManager
        if (pickupAudioSource)
        {
            pickupAudioSource.Play();
        }

        // Find closest item in the environment
        var items = GameObject.FindGameObjectsWithTag("Pickups");
        foreach (var item in items)
        {
            distance = ControlPlayer.EuclideanDistance(digCrosshair.transform, item.transform);
            if (distance < minDistance)
            {
                minDistance = distance;
                minDistanceItem = item;
            }
        }

        // Add or subtract points depending on whether dig was successful
        if (minDistance >= 0 && minDistance <= maxDigDistance)
        {
            // TODO: JPB: Should there be a gold reward for collection?
            //UpdateScore(goldFoundReward);
            im.scriptedInput.ReportScriptedEvent("pickup", new Dictionary<string, object> {{"successful", true},
                                                                                           {"distanceFromNearestItem", minDistance},
                                                                                           {"nearestItemPositionX", minDistanceItem.transform.position.x},
                                                                                           {"nearestItemPositionZ", minDistanceItem.transform.position.z}});
            state.itemsFoundLastTrial++;
            string itemTypeStr = Enum.GetName(itemType.GetType(), itemType);
            controlMainCanvas.SetTaskDirectionsDisplay("PICKUP "+ itemTypeStr.ToUpper() + ": " + (items.Length - 1).ToString() + " LEFT");
            if (itemFoundEffect)
            {
                Instantiate(itemFoundEffect, minDistanceItem.transform.position, Quaternion.identity);
            }
            spawnItems.HideItem(minDistanceItem);
        }
        else if (minDistance == invalidDistance) // i.e. all items have been dug
        {
            //UpdateScore(wrongDigPenalty);
            im.scriptedInput.ReportScriptedEvent("pickup", new Dictionary<string, object> {{"successful", false},
                                                                                           {"distanceFromNearestItem", -1}, // these -1s are for finding instances but should be removed from analysis
                                                                                           {"nearestItemPositionX", -1},
                                                                                           {"nearestItemPositionZ", -1}});
            if (itemNotFoundEffect)
            {
                Vector3 spawnPosition = gameObject.transform.position + new Vector3(0f, -1.18f, 0f);
                Instantiate(itemNotFoundEffect, playerAnimationSpawnPoint.transform.position, Quaternion.identity); // +new Vector3(0f, -1.18f, 1f)
            }
        }
        else
        {
            //UpdateScore(wrongDigPenalty);
            im.scriptedInput.ReportScriptedEvent("pickup", new Dictionary<string, object> {{"successful", false},
                                                                                           {"distanceFromNearestItem", minDistance},
                                                                                           {"nearestItemPositionX", minDistanceItem.transform.position.x},
                                                                                           {"nearestItemPositionZ", minDistanceItem.transform.position.z}});
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


        const float invalidDistance = -1;

        float minDistance = invalidDistance;
        float distance;

        // Register a dig
        state.digsAttempted++;

        // Play the dig sound
        if (digAudioSource)
        {
            digAudioSource.Play();
        }

        // Find closest item in the environment
        var items = GameObject.FindGameObjectsWithTag("Pickups");
        foreach (var item in items)
        {
            distance = ControlPlayer.EuclideanDistance(digCrosshair.transform, item.transform);
            if (distance < minDistance)
            {
                minDistance = distance;
                minDistanceItem = item;
            }
        }
        string minDistanceItemName = char.ToLowerInvariant(minDistanceItem.name[0]) + minDistanceItem.name.Substring(1);

        // Add or subtract points depending on whether dig was successful
        if (minDistance >= 0 && minDistance <= maxDigDistance)
        {
            UpdateScore(itemFoundReward);
            im.scriptedInput.ReportScriptedEvent("dig", new Dictionary<string, object> {{"successful", true},
                                                                                        {"distanceFromNearestItem", minDistance},
                                                                                        {"nearestItemPositionX", minDistanceItem.transform.position.x},
                                                                                        {"nearestItemPositionZ", minDistanceItem.transform.position.z},
                                                                                        {"nearestItem", minDistanceItemName}});
            state.itemsFoundLastTrial++;
            string itemTypeStr = Enum.GetName(itemType.GetType(), itemType);
            controlMainCanvas.SetTaskDirectionsDisplay("DIG FOR " + itemTypeStr.ToUpper() + ": " + (items.Length - 1).ToString() + " LEFT");
            if (itemFoundEffect)
            {
                Instantiate(itemFoundEffect, minDistanceItem.transform.position, Quaternion.identity);
            }
            Destroy(minDistanceItem);
        }
        else if (minDistance == invalidDistance) // i.e. all items have been dug
        {
            UpdateScore(wrongDigPenalty);
            im.scriptedInput.ReportScriptedEvent("dig", new Dictionary<string, object> {{"successful", false},
                                                                                        {"distanceFromNearestItem", -1}, // these -1s are for finding instances but should be removed from analysis
                                                                                        {"nearestItemPositionX", -1},
                                                                                        {"nearestItemPositionZ", -1},
                                                                                        {"nearestItem", -1}});
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
                                                                                        {"nearestItem", minDistanceItemName}});
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
