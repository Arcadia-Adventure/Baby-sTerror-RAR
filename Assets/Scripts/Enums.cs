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
    PickFacewash = 7,

    // Drop item tasks  
    DropBabyCradle = 8,
    DropFacewash = 9,
    DropTalisman = 10,
    DropFeederOnBaby = 11,
    DropClothOnBaby = 12,
    DropFireExtinguisher = 13,
    // Location tasks
    GoOutside = 14,
    ReachRoom = 15,
    FindBaby = 16,

    // Special tasks
    BreakDoor = 17,
    Survive = 18,
    DropBabyWashroom = 19,
    DropFacewashOnBaby = 20,
    PickToy = 21,
    DropToyOnBaby = 22,
    CheckBabyRoom = 23,
    FireEnded = 24,
    BedroomDoorBreak = 25,
    FollowBabyVoice = 26,
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
    Facewash = 7,
    Toy = 8,
    BabyRoomDoor = 9,
    HouseExitDoor = 10,
}
public enum PlayerAnimation
{
    None = 0,
    Unconscious = 1,
    GettingUp = 2,
}