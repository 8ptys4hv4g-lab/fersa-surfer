using System.Collections.Generic;
using UnityEngine;
using WorldRush.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace WorldRush.Runtime
{
    /// <summary>
    /// Übersetzt Tastatur, Touch/Maus-Wischen und Gamepad in <see cref="RunnerCommand"/>s.
    /// Nutzt das neue Input System, fällt sonst auf den alten Input Manager zurück.
    /// </summary>
    public sealed class RunnerInput
    {
        private readonly SwipeDetector _swipe = new SwipeDetector();
        private readonly List<RunnerCommand> _buffer = new List<RunnerCommand>(4);

        public bool PausePressed { get; private set; }
        public bool ConfirmPressed { get; private set; }

        public RunnerInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
#endif
        }

        public IReadOnlyList<RunnerCommand> Poll()
        {
            _buffer.Clear();
            PausePressed = false;
            ConfirmPressed = false;
            _swipe.MinDistance = SwipeDetector.DistanceForScreen(Screen.width, Screen.height);
            float now = Time.unscaledTime;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) _buffer.Add(RunnerCommand.MoveLeft);
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) _buffer.Add(RunnerCommand.MoveRight);
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) _buffer.Add(RunnerCommand.Jump);
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) _buffer.Add(RunnerCommand.Slide);
                if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) PausePressed = true;
                if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) ConfirmPressed = true;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) _buffer.Add(RunnerCommand.MoveLeft);
                if (pad.dpad.right.wasPressedThisFrame) _buffer.Add(RunnerCommand.MoveRight);
                if (pad.buttonSouth.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame) _buffer.Add(RunnerCommand.Jump);
                if (pad.buttonEast.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame) _buffer.Add(RunnerCommand.Slide);
                if (pad.startButton.wasPressedThisFrame) PausePressed = true;
                if (pad.buttonSouth.wasPressedThisFrame) ConfirmPressed = true;
            }

            foreach (var touch in Touch.activeTouches)
            {
                Vector2 p = touch.screenPosition;
                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        _swipe.Begin(p.x, p.y, now);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        Add(_swipe.Update(p.x, p.y, now));
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                        Add(_swipe.End(p.x, p.y, now));
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        _swipe.Cancel();
                        break;
                }
            }

            // Maus-Wischen (praktisch zum Testen am PC ohne Touchscreen).
            var mouse = Mouse.current;
            if (mouse != null && Touch.activeTouches.Count == 0)
            {
                Vector2 p = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame) _swipe.Begin(p.x, p.y, now);
                else if (mouse.leftButton.isPressed) Add(_swipe.Update(p.x, p.y, now));
                else if (mouse.leftButton.wasReleasedThisFrame) Add(_swipe.End(p.x, p.y, now));
            }
#else
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) _buffer.Add(RunnerCommand.MoveLeft);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) _buffer.Add(RunnerCommand.MoveRight);
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) _buffer.Add(RunnerCommand.Jump);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) _buffer.Add(RunnerCommand.Slide);
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) PausePressed = true;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ConfirmPressed = true;

            if (Input.GetMouseButtonDown(0)) _swipe.Begin(Input.mousePosition.x, Input.mousePosition.y, now);
            else if (Input.GetMouseButton(0)) Add(_swipe.Update(Input.mousePosition.x, Input.mousePosition.y, now));
            else if (Input.GetMouseButtonUp(0)) Add(_swipe.End(Input.mousePosition.x, Input.mousePosition.y, now));
#endif
            return _buffer;
        }

        private void Add(RunnerCommand cmd)
        {
            if (cmd != RunnerCommand.None) _buffer.Add(cmd);
        }
    }
}
