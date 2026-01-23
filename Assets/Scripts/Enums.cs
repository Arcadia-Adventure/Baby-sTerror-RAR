public enum BabyAnimationType
{
    Cry,
    Fly,
    Happy,
    Sit,
    Idle,
    AngrySit,
    None,
}
public enum TaskType
{
    /// <summary>No task - does nothing when triggered</summary>
    None = 0,
    // Pick item tasks
    PickBaby,
    PickFeeder,
    PickCloth,
    PickTalisman,
    PickAxe,
    PickFireExtinguisher,
    PickFacewash,

    // Drop item tasks  
    DropBabyCradle,
    DropFacewash,
    DropTalisman,
    DropFeederOnBaby,
    DropClothOnBaby,
    DropFireExtinguisher,

    // Location tasks
    GoOutside,
    ReachRoom,
    FindBaby,

    // Special tasks
    BreakDoor,
    Survive,
    DropBabyWashroom,
    DropFacewashOnBaby,
}
public enum ItemType
{
    Baby,
    Feeder,
    Cloth,
    Talisman,
    Axe,
    FireExtinguisher,
    Facewash,
    None,
}