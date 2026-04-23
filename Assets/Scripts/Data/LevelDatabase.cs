using System;

[Serializable]
public class LevelDatabase
{
    public LevelData[] levels;
}

[Serializable]
public class LevelData
{
    public int level;
    public string missionName;
    public BabySetup baby;
    public DoorSetup doors;
    public FeatureFlags features;
    public TaskData[] tasks;
}

[Serializable]
public class BabySetup
{
    public bool active = true;
    public string requireItem = "None";
    public bool canPickBaby = true;
    public string initialAnimation = "CrySit";
    public bool possessed;
    public bool dirtyFace;
}

[Serializable]
public class DoorSetup
{
    public bool houseExitLocked = true;
    public bool upperRoomLocked;
    public DoorKnockSetup doorKnocking;
    public bool doorBell;
}

[Serializable]
public class DoorKnockSetup
{
    public bool enabled;
    public float initialDelay;
    public float interval;
}

[Serializable]
public class FeatureFlags
{
    public bool cradleActive;
    public bool fireActive;
    public bool flyingFurniture;
    public string playerStartAnimation = "None";
    public bool showNextButton = true;
    public bool showRateUsButton;
}

[Serializable]
public class TaskData
{
    public string taskType;
    public string description;
    public bool completePreviousTasks;
}
