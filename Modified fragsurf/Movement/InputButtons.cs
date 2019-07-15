
namespace Fragsurf.Movement {

    [System.Flags]
    public enum InputButtons {
        None = 0,
        Jump = 1 << 1,
        Duck = 1 << 2,
        Speed = 1 << 3,
        MoveLeft = 1 << 4,
        MoveRight = 1 << 5,
        MoveForward = 1 << 6,
        MoveBack = 1 << 7
    }

}
