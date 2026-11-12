using UnityEngine;

namespace RedMoon.Player
{
    /// <summary>
    /// One frame of player input. Gameplay consumes this instead of reading devices.
    /// </summary>
    public struct PlayerInputFrame
    {
        public readonly Vector2 Move;
        public readonly Vector2 Look;
        public readonly bool SprintPressed;
        public readonly bool JumpPressed;
        public readonly bool JumpHeld;

        public PlayerInputFrame(Vector2 move, Vector2 look, bool sprintPressed, bool jumpPressed, bool jumpHeld)
        {
            Move = move;
            Look = look;
            SprintPressed = sprintPressed;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
        }

        public static PlayerInputFrame Empty => new PlayerInputFrame(Vector2.zero, Vector2.zero, false, false, false);
    }
}
