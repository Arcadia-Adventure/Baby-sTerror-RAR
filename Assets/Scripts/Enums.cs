public enum BabyAnimationType
{
    None = 0,
    CrySit = 1,
    Fly = 2,
    Happy = 3,
    Idle = 4,
    AngrySit = 5,
    CryLay = 6,
    Drop = 7,
    CryStand = 8,
}
public enum TaskType
{
    /// <summary>No task - does nothing when triggered</summary>
    None = 0,
    // Pick item tasks
    PickBaby = 1,
    PickFeeder = 2,
    PickCloth = 3,
    PickTalisman = 4,
    PickAxe = 5,
    PickFireExtinguisher = 6,
    PickDiaper = 7,

    // Drop item tasks  
    DropBabyCradle = 8,
    DropDiaper = 9,
    DropTalisman = 10,
    DropFeederOnBaby = 11,
    DropClothOnBaby = 12,
    DropFireExtinguisher = 13,
    // Location tasks
    DropOnRockingChair = 14,
    ReachRoom = 15,
    FindBaby = 16,

    // Special tasks
    BreakDoor = 17,
    Survive = 18,
    DropBabyWashroom = 19,
    DropDiaperOnBaby = 20,
    PickToy = 21,
    DropToyOnBaby = 22,
    CheckBabyRoom = 23,
    FireEnded = 24,
    BedroomDoorBreak = 25,
    FollowBabyVoice = 26,
    BanishNanny = 27,
}
public enum ItemType
{
    None = 0,
    Any = -1,
    Baby = 1,
    Feeder = 2,
    Cloth = 3,
    Talisman = 4,
    Axe = 5,
    FireExtinguisher = 6,
    Diaper = 7,
    Toy = 8,
    UpperRoomDoor = 9,
    HouseExitDoor = 10,
}
public enum PlayerAnimation
{
    None = 0,
    Unconscious = 1,
    GettingUp = 2,
}

public enum NannyState
{
    Idle = 0,
    Patrol = 1,
    Chase = 2,
    Attack = 3,
    Search = 4,
    Dead = 5,
    Glimpse = 6,
    BangDoor = 7,
    Banished = 8,
    Repelled = 9,
    KnockedDown = 10,
}

public enum NannyMode
{
    /// <summary>Not in the level.</summary>
    None = 0,
    /// <summary>Harmless: stands still, screams and vanishes once the player sees her.</summary>
    Glimpse = 1,
    /// <summary>Patrols, then chases and attacks on sight.</summary>
    Patrol = 2,
    /// <summary>Patrol, plus periodically drifting toward where the player is.</summary>
    Hunter = 3,
}

public enum NannySound
{
    None = 0,
    Breathing = 1,
    Chasing = 2,
    Scream = 3,
    Attack = 4,
    Death = 5,
}

public enum CrosshairState
{
    None,
    Pick,
    Drop,
    DoorOpen,
    DoorClose
}