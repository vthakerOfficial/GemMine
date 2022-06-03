using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class ExperimentGameManager : GameManager {
    public int numTrialsInGame = 36; // must be divisible by 6
    public GameObject scheduledPauseCanvas; 
    public GameObject endOfGameCanvas;
    private ControlEndOfGameCanvas controlEndOfGameCanvas;
    private byte[] bytes;

    protected override void Start() {
        im.scriptedInput.ReportScriptedEvent("loadScene", new Dictionary<string, object> { { "sceneName", (string)im.GetSetting("experimentScene") } });
        im.scriptedInput.ReportScriptedEvent("startMainExperiment", new Dictionary<string, object> { { "experimentScene", (string)im.GetSetting("experimentScene") } });
        base.Start();

        // Randomize half of the trials to timed and half to untimed.
        // Then, separately for timed and untimed trials, randomly assign 1/3 to each door index.
        bytes = DoorShuffle.TrialParameters(numTrialsInGame);
        Debug.Log(bytes);

        // Setup random variables for the first trial
        if (timedTrialSystemEnabled)
        {
            state.isTimedTrial = DoorShuffle.IsTimed(bytes[0]);
        }
        state.doorIndex = DoorShuffle.DoorIndex(bytes[0]);

        // List the trial events, in order
        stateMachine["Run"] = new List<Action> {
            RunIndexWrapper(InitTrial),
            RunIndexWrapper(PreEncodingDelayMsg),
            RunIndexWrapper(Delay),
            RunIndexWrapper(Encoding),
            RunIndexWrapper(ReturnToBase),
            DoWaitForReturn,
            RunIndexWrapper(PreTimelineMsg),
            ConditionalActions(timelineSystemEnabled, new List<Action> {
                RunIndexWrapper(Timeline),
                RunIndexWrapper(TimelineEnd)}),
            RunIndexWrapper(PreRetrievalDelayMsg),
            RunIndexWrapper(Delay),
            RunIndexWrapper(Retrieval),
            RunIndexWrapper(ReturnToBase),
            DoWaitForReturn,
            RunIndexWrapper(EndOfTrial),
            DoNextTrial
        };

        // Setup the starting displays
        mainCanvas.SetActive(true);
        scheduledPauseCanvas.SetActive(false);
        endOfGameCanvas.SetActive(false);
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "MainCanvas" }, { "isActive", true } });
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "ScheduledPauseCanvas" }, { "isActive", false } });
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "EndOfGameCanvas" }, { "isActive", false } });

        controlMainCanvas.SetScoreDisplay(state.score.ToString(), "default", 0, false);
        controlEndOfGameCanvas = endOfGameCanvas.GetComponent<ControlEndOfGameCanvas>();
        trialDisplay.text = "TRIAL " + (state.trialsCompleted + 1).ToString();

        Run();
    }

    public void DoWaitForReturn() {
        //im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "DoWaitForReturn" } });

        if(!controlPlayer.playerInMine) {
            state.runIndex++;
        }
        gameEvents.Do(new EventBase(Run));
    }

    public void DoNextTrial() {
        im.scriptedInput.ReportScriptedEvent("gameState", new Dictionary<string, object> { { "stateName", "DoNextTrial" } });

        // Set up random variables for the next trial
        if (state.trialsCompleted < numTrialsInGame)
        {
            if (timedTrialSystemEnabled)
            {
                state.isTimedTrial = DoorShuffle.IsTimed(bytes[state.trialsCompleted]);
            }
            
            state.doorIndex = DoorShuffle.DoorIndex(bytes[state.trialsCompleted]);
        }
        else
        {
            if (timedTrialSystemEnabled)
            {
                if (UnityEngine.Random.value > 0.5f)
                {
                    state.isTimedTrial = true;
                }
                else
                {
                    state.isTimedTrial = false;
                }
            }

            state.doorIndex = UnityEngine.Random.Range(0, 3);
        }

        // Decide what the next action will be (end game, pause game, or continue to the next trial)
        if(state.trialsCompleted == numTrialsInGame) {
            gameEvents.Do(new EventBase(EndOfGame));
        }
        else if ((state.trialsCompleted > 0) && (state.trialsCompleted % 12 == 0))
        {
            state.runIndex = 0;
            state.paused = true;
            im.scriptedInput.ReportScriptedEvent("gamePaused", new Dictionary<string, object> { { "isPaused", true }, { "pauseType", "scheduledPause" } });
            FreezeAtBase();
            im.LockCursor(CursorLockMode.None);
            mainCanvas.SetActive(false);
            scheduledPauseCanvas.SetActive(true);
            im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "MainCanvas" }, { "isActive", false } });
            im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "ScheduledPauseCanvas" }, { "isActive", true } });
        }
        else {
            state.runIndex = 0;
            gameEvents.Do(new EventBase(Run));
        }
    }

    public void ContinueFromPause()
    {
        state.paused = false;
        im.scriptedInput.ReportScriptedEvent("gamePaused", new Dictionary<string, object> { { "isPaused", false }, { "pauseType", "scheduledPause" } });
        mainCanvas.SetActive(true);
        scheduledPauseCanvas.SetActive(false);
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "MainCanvas" }, { "isActive", true } });
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "ScheduledPauseCanvas" }, { "isActive", false } });
        im.LockCursor(CursorLockMode.Locked);
        gameEvents.Do(new EventBase(Run));
    }

    public void EndOfGame()
    {
        string msg;
        gameEvents.Do(new EventBase(FreezeAtBase));
        mainCanvas.SetActive(false);
        endOfGameCanvas.SetActive(true);

        im.LockCursor(CursorLockMode.None);
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "MainCanvas" }, { "isActive", false } });
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "EndOfGameCanvas" }, { "isActive", true } });
        // Print end of game stats to the canvas
        msg = (state.itemsFoundTotal.ToString() + " (" + Math.Round(100f * state.itemsFoundTotal / state.itemsSpawnedTotal).ToString() + "%)\n" + // gold found (% of spawned gold found)
               Math.Round(100f * state.itemsFoundTotal / state.digsAttempted).ToString() + "%\n" + // digging accuracy
               state.score); // final score
        controlEndOfGameCanvas.SetStatDisplay(msg);
        controlEndOfGameCanvas.playAudio(true);
    }

    public void KeepPlaying()
    {
        im.LockCursor(CursorLockMode.Locked);
        controlEndOfGameCanvas.playAudio(false);
        mainCanvas.SetActive(true);
        endOfGameCanvas.SetActive(false);
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "MainCanvas" }, { "isActive", true } });
        im.scriptedInput.ReportScriptedEvent("canvasActive", new Dictionary<string, object> { { "canvasName", "EndOfGameCanvas" }, { "isActive", false } });
        state.runIndex = 0;
        gameEvents.Do(new EventBase(Run));
    }
}