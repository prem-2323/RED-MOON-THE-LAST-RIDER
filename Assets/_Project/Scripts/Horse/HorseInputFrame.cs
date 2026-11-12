using UnityEngine;

namespace RedMoon.Horse
{
    /// <summary>
    /// Immutable value representation of a single frame of horse control input.
    /// Eliminates direct hardware dependencies inside the locomotion motor.
    /// </summary>
    public readonly struct HorseInputFrame
    {
        public readonly Vector2 Move;
        public readonly Vector2 Look;
        public readonly bool Sprint;
        public readonly bool JumpPressed;
        public readonly bool DismountPressed;

        public HorseInputFrame(Vector2 move, Vector2 look, bool sprint, bool jumpPressed, bool dismountPressed)
        {
            Move = move;
            Look = look;
            Sprint = sprint;
            JumpPressed = jumpPressed;
            DismountPressed = dismountPressed;
        }

        public static HorseInputFrame Empty => new HorseInputFrame(Vector2.zero, Vector2.zero, false, false, false);
    }
}

