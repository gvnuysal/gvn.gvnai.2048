using System;
using Game2048.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game2048
{
    /// <summary>
    /// Arrow keys / WASD and swipe (touch or mouse drag) → one <see cref="Direction"/> per gesture.
    /// Uses the Input System package.
    /// </summary>
    public sealed class InputHandler : MonoBehaviour
    {
        [Tooltip("Minimum swipe distance in inches (converted with Screen.dpi).")]
        [SerializeField] private float swipeInches = 0.25f;

        [Tooltip("Fallback swipe distance in pixels when Screen.dpi is unknown.")]
        [SerializeField] private float fallbackSwipePixels = 50f;

        public event Action<Direction> DirectionPressed;

        private bool _tracking;
        private Vector2 _start;

        private float SwipeThreshold => Screen.dpi > 0f ? Screen.dpi * swipeInches : fallbackSwipePixels;

        private void Update()
        {
            ReadKeyboard();
            ReadSwipe();
        }

        private void ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Raise(Direction.Up);
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Raise(Direction.Down);
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Raise(Direction.Left);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Raise(Direction.Right);
        }

        private void ReadSwipe()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame)
            {
                _tracking = true;
                _start = pointer.position.ReadValue();
                return;
            }

            if (!_tracking) return;

            if (!pointer.press.isPressed)
            {
                _tracking = false;
                return;
            }

            // Fire as soon as the finger passes the threshold; one move per gesture.
            var delta = pointer.position.ReadValue() - _start;
            if (delta.magnitude < SwipeThreshold) return;

            _tracking = false;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                Raise(delta.x > 0f ? Direction.Right : Direction.Left);
            else
                Raise(delta.y > 0f ? Direction.Up : Direction.Down); // screen y grows upwards
        }

        private void Raise(Direction direction) => DirectionPressed?.Invoke(direction);
    }
}
