namespace Game.State;

internal enum InteractiveProgramStatePhase : ushort
{
    PrimaryControlSelection = 0,
    PrimarySceneSelection = 1,
    SecondarySceneSelection = 2,
    ConfirmPrimary = 3,
    ConfirmSecondary = 4,
    Completed = 5
}
