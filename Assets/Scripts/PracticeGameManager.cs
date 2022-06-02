using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading;

class PracticeGameManager : GameManager {

    public GameObject tutorialCanvas;
    public GameObject gold1Location;
    public GameObject gold2Location;
    public GameObject gold3Location;
    public GameObject gold1Box;
    public bool gold1Found = false;

    private ControlTutorialCanvas controlTutorialCanvas;
    private GameObject gold1;
    private GameObject gold2;
    private GameObject gold3;

    private Dictionary<string, string> tutorial = new Dictionary<string, string>
    {
        {"welcome gold", "Welcome to Goldmine!\n\nIn this game, you will be searching and then digging for gold over a number of rounds.\n\nPlease ask questions as we go through this tutorial.\n\n(Press space to continue)"},
        {"welcome items", "Welcome to Goldmine!\n\nIn this game, you will be searching and then digging for items over a number of rounds.\n\nPlease ask questions as we go through this tutorial.\n\n(Press space to continue)"},
        {"instruct pickup gold", "Each round begins here in the mine base. First, you will have 30 seconds to search for " + (pickupSystemEnabled ? "and pickup " : "") + "one or more pieces of gold that appear on the ground in the mine ('Search' trials). Try to remember where each gold piece is, as you will need to return there later.\n\nAfter 30 seconds, the gold will disappear, and you will be asked to go back to the base."},
        {"instruct pickup items", "Each round begins here in the mine base. First, you will have 30 seconds to search for " + (pickupSystemEnabled ? "and pickup " : "") + "one or more items that appear on the ground in the mine ('Search' trials). Also, try to remember where each item is, as you will need to return there later.\n\nAfter 30 seconds, the item will disappear, and you will be asked to go back to the base."},
        {"instruct timeline gold", "Next, you will have another 18 seconds to place the gold that you picked up onto a timeline ('Timeline' trials). Try to place the gold pieces on the timeline at the time you found them."},
        {"instruct timeline items", "Next, you will have another 18 seconds to place the items that you picked up onto a timeline ('Timeline' trials). Try to place the items on the timeline at the time you found them. You should try to only place the items that you picked up on the timeline."},
        {"instruct digging gold", "Finally, you will have another 30 seconds to go back into the mine and try to dig up the gold that you just found ('Digging' trials). This time the gold will be hidden from view, so you have to dig at the place where you remember it being."},
        {"instruct digging items", "Finally, you will have another 30 seconds to go back into the mine and try to dig up the items that you just found ('Digging' trials). This time the items will be hidden from view, so you have to dig at the place where you remember it being."},
        {"instruct delay gold", "Before each trial, there is a short waiting time in the base, during which you are asked to prepare for the next trial.\n\nIn the waiting time before Digging trials, please try to visualize a path back to the gold that you are intending to dig.\n\nAfter the waiting time, a door will open to your left, right, or center and let you out into the mine. Only one door will open to let you out of the base, but you can re-enter through any door."},
        {"instruct delay items", "Before each trial, there is a short waiting time in the base, during which you are asked to prepare for the next trial.\n\nIn the waiting time before Digging trials, please try to visualize a path back to the items that you are intending to dig.\n\nAfter the waiting time, a door will open to your left, right, or center and let you out into the mine. Only one door will open to let you out of the base, but you can re-enter through any door."},
        {"instruct scoring gold", "SCORING\n\nThe goal of this game is to get as many points as you can.\n\n" + (timelineSystemEnabled ? ">  Placing a gold piece on the timeline that was picked up is worth +10 points\n>  Placing a gold piece on the timeline that was not picked up costs -2 points. \n>  Not placing a gold piece on the timeline that was picked up costs -2 points.\n\n" : "") + ">  Digging at a correct gold location is worth +10 points\n>  Digging at an incorrect location costs -2 points\n\n"},
        {"instruct scoring items", "SCORING\n\nThe goal of this game is to get as many points as you can.\n\n" + (timelineSystemEnabled ? ">  Placing an item on the timeline that was picked up is worth +10 points\n>  Placing an item on the timeline that was not picked up costs -2 points. \n>  Not placing an item on the timeline that was picked up costs -2 points.\n\n" : "") + ">  Digging at a correct item location is worth +10 points\n>  Digging at an incorrect location costs -2 points\n\n"},
        {"instruct controls gold", "GAME CONTROLS\n\n>  Rotate your view by moving the mouse\n>  Move forward by clicking and holding the left mouse button\n" + (pickupSystemEnabled ? ">  Pickup a gold piece by pressing the spacebar\n" : "") + ">  Dig by pressing the spacebar (1 keypress = 1 dig)"},
        {"instruct controls items", "GAME CONTROLS\n\n>  Rotate your view by moving the mouse\n>  Move forward by clicking and holding the left mouse button\n" + (pickupSystemEnabled ? ">  Pickup an item by pressing the spacebar\n" : "") + ">  Dig by pressing the spacebar (1 keypress = 1 dig)"},
        {"instruct hud", "TASK TOOLBAR\n\nA toolbar at the top of your screen shows your current instructions (top left) and score (top right)."},
        {"instruct final", "Let's do one round now for practice. Remember, we will start with the searching trial."},
        {"pickup gold", "Pickup trial: There is a crosshair on the ground in front of you that shows where you are aiming, and you can press the spacebar when you are ready to pickup a piece of gold."},
        {"pickup items", "Pickup trial: There is a crosshair on the ground in front of you that shows where you are aiming, and you can press the spacebar when you are ready to pickup an item."},
        {"encoding 1 end", "30 seconds are up. Now you should return to the base."},
        {"timeline gold", "Timeline trial: Now you will try to place on the timeline the item that you found on the last trial."},
        {"timeline items", "Timeline trial: Now you will try to place on the timeline the item that you found on the last trial."},
        {"digging gold", "Digging trial: Now you will try to dig up the gold that you found on the last trial. There is a crosshair on the ground in front of you that shows where you are aiming, and you can press the spacebar when you are ready to dig."},
        {"digging items", "Digging trial: Now you will try to dig up the items that you found on the last trial. There is a crosshair on the ground in front of you that shows where you are aiming, and you can press the spacebar when you are ready to dig."},
        {"trial 1 end gold", "You completed one round! Let's try one more. This time, a right arrow will appear on your screen at the end of the waiting time to tell you that the right door has opened for this round.\n\nNote: you don’t need to remember the location of gold from the previous round to the next one."},
        {"trial 1 end items", "You completed one round! Let's try one more. This time, a right arrow will appear on your screen at the end of the waiting time to tell you that the right door has opened for this round.\n\nNote: you don’t need to remember the location of items from the previous round to the next one."},
        {"trial 2 end", "Well done! We will do a final practice round now before starting the real game.\n\n"},
        {"time penalty", "There's one last thing to know. Some rounds (like this one) have a time penalty. This means that if you are not back at the base after 30 seconds, you will lose 5 points.\n\nSince there is no explicit timer, you will have to keep track of how much time has passed in your head."},
        {"no time penalty", "On rounds that don't have a time penalty, you can spend the whole 30 seconds searching or digging without worrying about the time.\n\n"},
        {"repeat or not", "Great job! Press space to move on or r to repeat this tutorial."},
        {"tutorial end 1", "The full game lasts for 36 rounds, with breaks after 12 and 24 rounds. You can press p to pause at any point if you need to."},
        {"tutorial end 2", "Please try not to talk during the experiment. You should keep your head and arms as still as possible, and stay focused on the game even during the waiting periods in the base.\n\nThank you for your participation, and good luck!"},
        {"gold not found", "You have not found the gold yet! Let's try this again.\n\n(Press space to go back)"},
        {"gold not dug", "You did not dig up the gold! Let's try this again.\n\n(Press space to go back)"}
    };

    protected override void Start() {
        im.scriptedInput.ReportScriptedEvent("loadScene", new Dictionary<string, object> { { "sceneName", (string)im.GetSetting("tutorialScene") } });

        base.Start();

        controlTutorialCanvas = tutorialCanvas.GetComponent<ControlTutorialCanvas>();
        gold1Box.SetActive(true);

        FreezeAtBase();

        string itemTypeStr = itemType == ItemType.gold ? "gold" : "items";

        // Setup "Run" state machine
        stateMachine["Run"] = new List<Action> {
            RunIndexWrapper(InstantiateGold),

            // Instructions
            WriteToCanvas("welcome " + itemTypeStr),
            WriteToCanvas("instruct pickup " + itemTypeStr),
            ConditionalAction(timelineSystemEnabled,
                WriteToCanvas("instruct timeline " + itemTypeStr)),
            WriteToCanvas("instruct digging " + itemTypeStr),
            WriteToCanvas("instruct delay " + itemTypeStr),
            WriteToCanvas("instruct scoring " + itemTypeStr),
            WriteToCanvas("instruct controls " + itemTypeStr),
            WriteToCanvas("instruct hud"),
            WriteToCanvas("instruct final"),

            RunIndexWrapper(PreEncodingDelayMsg),
            RunIndexWrapper(Delay),
            ConditionalAction(pickupSystemEnabled,
                WriteToCanvas("pickup " + itemTypeStr)),

            // Practice trial 1
            RunIndexWrapper(TutorialEncoding1),
            WriteToCanvas("encoding 1 end"),
            RunIndexWrapper(ReturnToBase),
            DoWaitForReturn,
            ConditionalActions(timelineSystemEnabled, new List<Action> {
                WriteToCanvas("timeline " + itemTypeStr),
                RunIndexWrapper(TutorialTimeline)}),
            RunIndexWrapper(PreRetrievalDelayMsg),
            RunIndexWrapper(Delay),
            WriteToCanvas("digging " + itemTypeStr),
            RunIndexWrapper(Retrieval),
            RunIndexWrapper(ReturnToBase),
            DoWaitForReturn,
            WriteToCanvas("trial 1 end " + itemTypeStr),

            // Practice trial 2
            RunIndexWrapper(PreEncodingDelayMsg),
            RunIndexWrapper(Delay),
            RunIndexWrapper(TutorialEncoding2),
            RunIndexWrapper(ReturnToBase),
            DoWaitForReturn,
            ConditionalAction(timelineSystemEnabled,
                RunIndexWrapper(TutorialTimeline)),
            RunIndexWrapper(PreRetrievalDelayMsg),
            RunIndexWrapper(Delay),
            RunIndexWrapper(Retrieval),
            RunIndexWrapper(ReturnToBase),
            DoWaitForReturn,

            ConditionalActions(timedTrialSystemEnabled, new List<Action> {
                // Practice trial 3
                WriteToCanvas("trial 2 end"),
                RunIndexWrapper(PreEncodingDelayMsg),
                RunIndexWrapper(Delay),
                WriteToCanvas("time penalty"),
                WriteToCanvas("no time penalty"),
                RunIndexWrapper(TutorialEncoding3),
                RunIndexWrapper(ReturnToBase),
                DoWaitForReturn,
                ConditionalAction(timelineSystemEnabled,
                    RunIndexWrapper(TutorialTimeline)),
                RunIndexWrapper(PreRetrievalDelayMsg),
                RunIndexWrapper(Delay),
                RunIndexWrapper(Retrieval),
                RunIndexWrapper(ReturnToBase),
                DoWaitForReturn,
            }),

            // End tutorial
            DoRepeatOrContinue,
            WriteToCanvas("tutorial end 1"),
            WriteToCanvas("tutorial end 2"),
            LaunchExperiment
        };

        // Setup "loop" state machine
        state.loopIndex = 0;
        stateMachine["loop"] = new List<Action> {
            RunIndexWrapperLoop(InitTrial),
            WriteToCanvasLoop("welcome"),
            RunIndexWrapperLoop(Delay),
            WriteToCanvasLoop("navigation"),
            WriteToCanvasLoop("detector goggles"),
            WriteToCanvasLoop("instructions display"),
            WriteToCanvasLoop("task timing"),
            RunIndexWrapperLoop(TutorialEncoding1),
            WriteToCanvasLoop("end of encoding"),
            RunIndexWrapperLoop(ReturnToBase),
            DoWaitForReturnLoop,
            //RunIndexWrapperLoop(CheckGold1Found),
            RunIndexWrapperLoop(Delay),
            WriteToCanvasLoop("pickaxe"),
            WriteToCanvasLoop("remembering gold"),
            WriteToCanvasLoop("digging"),
            RunIndexWrapperLoop(Retrieval),
            RunIndexWrapperLoop(ReturnToBase),
            DoWaitForReturnLoop,
            //RunIndexWrapperLoop(CheckGold1Dug),
            BreakOutOfLoop
        };

        // Setup the starting displays
        mainCanvas.SetActive(true);
        controlMainCanvas.SetScoreDisplay(state.score.ToString(), "default", 0, false);
        tutorialCanvas.SetActive(false);
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "MainCanvas" }, { "isActive", true } });
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", false } });

        Run();
    }

    public void RunLoop()
    {
        stateMachine["loop"][state.loopIndex].Invoke();
    }

    public void BreakOutOfLoop()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "BreakOutOfLoop" } });

        state.runIndex++;
        gameEvents.Do(new EventBase(Run));
    }

    public override Action RunIndexWrapper(Action todo)
    {
        return () => {
            tutorialCanvas.SetActive(false);
            im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", false } });
            controlTutorialCanvas.ResetCentralDisplay();
            state.runIndex++;
            todo();
        };
    }

    public Action RunIndexWrapperLoop(Action todo)
    {
        return () => {
            tutorialCanvas.SetActive(false);
            im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", false } });
            controlTutorialCanvas.ResetCentralDisplay();
            state.loopIndex++;
            todo();
        };
    }

    public Action WriteToCanvas(string key_)
    {
        return () =>
        {
            // Log
            im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "WriteToCanvas" } });

            Pause(true);
            tutorialCanvas.SetActive(true);
            im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", true } });
            controlTutorialCanvas.SetCentralDisplay(tutorial[key_]);
            state.runIndex++;
            im.RegisterKeyHandler(PressSpace);
        };
    }

    public Action WriteToCanvasLoop(string key_)
    {
        return () =>
        {
            // Log
            im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "WriteToCanvasLoop" } });

            Pause(true);
            tutorialCanvas.SetActive(true);
            im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", true } });
            controlTutorialCanvas.SetCentralDisplay(tutorial[key_]);
            state.loopIndex++;
            im.RegisterKeyHandler(PressSpace);
        };
    }

    public void DoWaitForReturn()
    {
        // Log
        //im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "DoWaitForReturn" } });

        if (!controlPlayer.playerInMine)
        {
            state.runIndex++;
        }
        gameEvents.Do(new EventBase(Run));
    }

    public void DoWaitForReturnLoop()
    {
        // Log
        //im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "DoWaitForReturnLoop" } });

        if (!controlPlayer.playerInMine)
        {
            state.loopIndex++;
        }
        gameEvents.Do(new EventBase(Run));
    }

    public void CheckGold1Found()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "CheckGold1Found" } });

        if (!gold1Found)
        {
            state.loopIndex = 0;
            Pause(true);
            tutorialCanvas.SetActive(true);
            im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", true } });
            controlTutorialCanvas.SetCentralDisplay(tutorial["gold not found"]);
            im.RegisterKeyHandler(PressSpace);
        }
        else 
        {
            gameEvents.Do(new EventBase(Run));
        }
    }

    public void CheckGold1Dug()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "CheckGold1Dug" } });

        if (state.itemsFoundLastTrial == 0)
        {
            state.loopIndex = 0;
            gold1Box.SetActive(true);
            Pause(true);
            tutorialCanvas.SetActive(true);
            im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", true } });
            controlTutorialCanvas.SetCentralDisplay(tutorial["gold not dug"]);
            im.RegisterKeyHandler(PressSpace);
        }
        else 
        {
            gameEvents.Do(new EventBase(Run));
        }

    }

    public void DoWaitForSpace() // works but no longer in use
    {
        if (Input.GetKeyDown("space"))
        {
            state.runIndex++;
        }
        gameEvents.DoIn(new EventBase(Run), 1);
    }

    public void PressSpaceDontRun(string key, bool down)
    {
        key = key.ToLower();

        if ((down) && (key == "space"))
        {
            tutorialCanvas.SetActive(false);
            im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", false } });
            controlTutorialCanvas.ResetCentralDisplay();
            Pause(false);
        }
        else
        {
            im.RegisterKeyHandler(PressSpaceDontRun);
        }
    }

    public void InstantiateGold()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "InstantiateGold" } });

        // Destroy any existing gold objects
        var items = GameObject.FindGameObjectsWithTag("Pickups");
        for (int iGold = 0; iGold < items.Length; iGold++)
        {
            Destroy(items[iGold]);
        }

        // Create the new gold objects
        gold1 = Instantiate(spawnItems.goldObject, gold1Location.transform.position, gold1Location.transform.rotation) as GameObject;
        gold2 = Instantiate(spawnItems.goldObject, gold2Location.transform.position, gold2Location.transform.rotation) as GameObject;
        gold3 = Instantiate(spawnItems.goldObject, gold3Location.transform.position, gold3Location.transform.rotation) as GameObject;
        gold1.name = "gold";
        gold2.name = "gold";
        gold3.name = "gold";
        gold1.SetActive(false);
        gold2.SetActive(false);
        gold3.SetActive(false);
        itemsToFind = 1;
        gameEvents.Do(new EventBase(Run));
    }

    public void GoldFound() { 
        Pause(true);
        tutorialCanvas.SetActive(true);
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", true } });
        controlTutorialCanvas.SetCentralDisplay(tutorial["gold found"]); 
        im.RegisterKeyHandler(PressSpaceDontRun);
    }

    public void TutorialEncoding1()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "TutorialEncoding1" } });

        playerActive = true;
        state.doorIndex = 2;
        state.isTimedTrial = false;

        // Show the dig crosshair
        if (pickupSystemEnabled)
        {
            state.pickupEnabled = true;
            digCrosshair.SetActive(true);
        }

        // Unfreeze the player
        controlPlayer.Freeze(false);

        // Open one door
        bool[] iDoors = { false, false, false };
        iDoors[state.doorIndex] = true;
        controlBase.OpenDoors(iDoors, true, true);

        // Reveal gold in the environment
        gold1.SetActive(true);
        spawnItems.UnhideItems();

        // Update canvas displays
        string itemTypeStr = Enum.GetName(itemType.GetType(), itemType);
        if (pickupSystemEnabled)
        {
            controlMainCanvas.SetTopDisplay("PICKUP 1 " + itemTypeStr.ToUpper(), "default", 0.75f);
            controlMainCanvas.SetTaskDirectionsDisplay("PICKUP 1 " + itemTypeStr.ToUpper() + " LEFT");
        }
        else
        {
            controlMainCanvas.SetTopDisplay("FIND 1 " + itemTypeStr.ToUpper(), "default", 0.75f);
            controlMainCanvas.SetTaskDirectionsDisplay("FIND 1 " + itemTypeStr.ToUpper());
        }

        if (state.isTimedTrial)
        {
            controlMainCanvas.SetTimedTrialDisplay("TIME PENALTY", "negative");
            controlMainCanvas.SetBottomDisplay("TIME PENALTY", "negative", 0.75f);
        }

        gameEvents.DoIn(new EventBase(Run), taskDuration);
    }

    public void TutorialEncoding2()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "TutorialEncoding2" } });

        playerActive = true;
        state.doorIndex = 0;
        state.isTimedTrial = false;

        // Show the dig crosshair
        if (pickupSystemEnabled)
        {
            state.pickupEnabled = true;
            digCrosshair.SetActive(true);
        }

        // Unfreeze the player
        controlPlayer.Freeze(false);

        // Open one door
        bool[] iDoors = { false, false, false };
        iDoors[state.doorIndex] = true;
        controlBase.OpenDoors(iDoors, true, true);

        // Reveal gold in the environment
        gold2.SetActive(true);

        // Update canvas displays
        string itemTypeStr = Enum.GetName(itemType.GetType(), itemType);
        if (pickupSystemEnabled)
        {
            controlMainCanvas.SetTopDisplay("PICKUP 1 " + itemTypeStr.ToUpper(), "default", 0.75f);
            controlMainCanvas.SetTaskDirectionsDisplay("PICKUP 1 " + itemTypeStr.ToUpper() + " LEFT");
        }
        else
        {
            controlMainCanvas.SetTopDisplay("FIND 1 " + itemTypeStr.ToUpper(), "default", 0.75f);
            controlMainCanvas.SetTaskDirectionsDisplay("FIND 1 " + itemTypeStr.ToUpper());
        }

        if (state.isTimedTrial)
        {
            controlMainCanvas.SetTimedTrialDisplay("TIME PENALTY", "negative");
            controlMainCanvas.SetBottomDisplay("TIME PENALTY", "negative", 0.75f);
        }

        gameEvents.DoIn(new EventBase(Run), taskDuration);
    }

    public void TutorialEncoding3()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "TutorialEncoding3" } });

        playerActive = true;
        state.doorIndex = 1;
        state.isTimedTrial = true;

        // Show the dig crosshair
        if (pickupSystemEnabled)
        {
            state.pickupEnabled = true;
            digCrosshair.SetActive(true);
        }

        // Unfreeze the player
        controlPlayer.Freeze(false);

        // Open one door
        bool[] iDoors = { false, false, false };
        iDoors[state.doorIndex] = true;
        controlBase.OpenDoors(iDoors, true, true);

        // Reveal gold in the environment
        gold3.SetActive(true);

        // Update canvas displays
        string itemTypeStr = Enum.GetName(itemType.GetType(), itemType);
        if (pickupSystemEnabled)
        {
            controlMainCanvas.SetTopDisplay("PICKUP 1 " + itemTypeStr.ToUpper(), "default", 0.75f);
            controlMainCanvas.SetTaskDirectionsDisplay("PICKUP 1 " + itemTypeStr.ToUpper() + " LEFT");
        }
        else
        {
            controlMainCanvas.SetTopDisplay("FIND 1 " + itemTypeStr.ToUpper(), "default", 0.75f);
            controlMainCanvas.SetTaskDirectionsDisplay("FIND 1 " + itemTypeStr.ToUpper());
        }

        if (state.isTimedTrial)
        {
            controlMainCanvas.SetTimedTrialDisplay("TIME PENALTY", "negative");
            controlMainCanvas.SetBottomDisplay("TIME PENALTY", "negative", 0.75f);
        }

        gameEvents.DoIn(new EventBase(Run), taskDuration);
    }

    // Timeline
    protected void TutorialTimeline()
    {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "TutorialTimeline" } });

        // Reset the player
        FreezeAtBase();

        // Show the timeline
        timelineCanvas.SetActive(true);

        // Spawn the timeline items
        foreach (var item in new List<GameObject> { spawnItems.goldObject, spawnItems.gemObjects.ToList().GetRange(0, 7) })
        {
            SpawnTimelineItem(item);
        }
        MoveItemsToTimeline();

        // Unlock the mouse
        im.LockCursor(CursorLockMode.None);

        gameEvents.DoIn(new EventBase(
            () => {
                TutorialTimelineEnd();
                Run();
            }),
            timelineDuration);
    }

    protected void TutorialTimelineEnd()
    {
        // Report item times
        var timelineItems = timelineCanvas.transform.Find("Timeline").GetComponent<ControlTimeline>().GetItemTimes(timelineDuration / 1000);
        im.scriptedInput.ReportScriptedEvent("timeline", new Dictionary<string, object> { { "items", timelineItems } });
        //Debug.Log(JsonConvert.SerializeObject(new Dictionary<string, object> { { "items", timelineItems } }));

        // Update the score
        var spawnedItems = new List<GameObject> { spawnItems.goldObject };
        foreach (var item in new List<GameObject> { spawnItems.goldObject, spawnItems.gemObjects.ToList().GetRange(0,7) })
        {
            bool isItemInTimeline = timelineItems.Any(x => (string)x["name"] == item.name);
            bool isItemSpawned = spawnedItems.Any(x => x.name == item.name);

            if (isItemSpawned && isItemInTimeline)
            {
                // Item correctly placed on timeline
                UpdateScore(correctTimelineReward);
            }
            else if (!isItemSpawned && isItemInTimeline)
            {
                // Item incorrectly placed on timeline
                UpdateScore(wrongTimelinePenalty);
            }
            else if (isItemSpawned && !isItemInTimeline)
            {
                // Item not placed on timeline when it should be
                UpdateScore(wrongTimelinePenalty);
            }
        }

        // Lock the mouse
        im.LockCursor(CursorLockMode.Locked);

        // Show the new score
        Thread.Sleep(1);

        // Delete timeline items
        foreach (var item in GetTimelineItems())
        {
            Destroy(item);
        }

        // Hide the timeline
        timelineCanvas.SetActive(false);
    }

    public void DoRepeatOrContinue() {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "DoRepeatOrContinue" } });

        playerActive = false;
        tutorialCanvas.SetActive(true);
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "TutorialCanvas" }, { "isActive", true } });
        controlTutorialCanvas.SetCentralDisplay(tutorial["repeat or not"]);
        im.RegisterKeyHandler(RepeatOrContinue);
    }

    public void RepeatOrContinue(string key, bool down) {
        key = key.ToLower();
        if(down && key == "r") {
            //state.runIndex = 0;
            //state.loopIndex = 0;
            //gameEvents.Do(new EventBase(Run));
            im.LaunchExperiment();
        }
        else if(down && key == "space") {
            state.runIndex++;
            gameEvents.Do(new EventBase(Run));
        } 
        else {
            im.RegisterKeyHandler(RepeatOrContinue);
        }
    }

    public void LaunchExperiment() {
        // Log
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "LaunchExperiment" } });
        im.ChangeSetting("sceneToLaunch", (string)im.GetSetting("experimentScene"));

        im.Do(new EventBase<string>(im.LaunchScene, (string)im.GetSetting("sceneToLaunch")));
    }
}