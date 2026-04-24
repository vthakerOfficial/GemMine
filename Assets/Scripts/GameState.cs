using System;

[Serializable]
public class GameState
{
    public int score;
    public int pickupsAttempted;
    public int digsAttempted;
    public int itemsFoundTotal;
    public int itemsFoundLastTrial;
    public int itemsSpawnedTotal;

    public int trialsCompleted;
    public int runIndex;
    public bool isTimedTrial;
    public bool pickupEnabled;
    public int doorIndex;
    public bool digEnabled;
    public bool showCountdown;
    public bool paused;
    public bool controlsFrozen;
    public int loopIndex;
    public float timeLeft;
}
