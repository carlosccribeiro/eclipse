using Eclipse.Gameplay.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Gameplay.Input
{
    /// <summary>
    /// Turns raw press/release events into <see cref="InputBuffer"/> entries stamped with simulation frames.
    /// Pure logic (no device, no Unity types in the core): events are fed through <see cref="Press"/>/<see cref="Release"/>
    /// (by <see cref="Bind"/> at runtime, or synthetically in tests) and resolved in <see cref="Tick"/>.
    /// Knows only slot IDs, never abilities.
    ///
    /// Gamepad Ultimate (LB+RB, 06 §3.4) disambiguation: a gamepad Skill1 (LB) or Skill2 (RB) press is held back for
    /// <see cref="ChordWindowFrames"/> frames. If the other shoulder arrives inside the window, a single Ultimate is
    /// buffered and neither LB nor RB is emitted; otherwise the held press is emitted with its original frame stamp
    /// (so the 9-frame buffer age stays honest). Cost: gamepad LB/RB skills are delayed by up to 3 frames (50 ms),
    /// absorbed by the 9-frame buffer. Keyboard Q/E are never delayed.
    /// </summary>
    public sealed class PlayerInputReader : IFixedTickable
    {
        /// <summary>Chord window in frames (50 ms at 60 Hz). Engineering choice, not a GDD balance number.</summary>
        public const int ChordWindowFrames = 3;
        private const int MaxQueued = 16;

        private struct RawPress
        {
            public InputActionId Id;
            public bool Gamepad;
        }

        private readonly RawPress[] _queue = new RawPress[MaxQueued];
        private int _queued;
        private readonly bool[] _held = new bool[16];

        private InputActionId _pendingShoulder;
        private int _pendingFrame;

        public InputBuffer Buffer { get; } = new InputBuffer();

        /// <summary>Latest Move value (device space, not normalized by the reader). Set by <see cref="Bind"/> or tests.</summary>
        public Vector2 Move { get; set; }

        /// <summary>Latest Aim value: pointer position in pixels (M+K) or right stick (gamepad). Consumed by PlayerAim (T4).</summary>
        public Vector2 Aim { get; set; }

        /// <summary>True when the last Aim update came from a gamepad stick.</summary>
        public bool AimIsGamepad { get; set; }

        /// <summary>Queues a press; it is stamped with the frame of the next <see cref="Tick"/>.</summary>
        public void Press(InputActionId id, bool fromGamepad = false)
        {
            if (id == InputActionId.None) return;
            _held[(int)id] = true;
            if (_queued < MaxQueued) _queue[_queued++] = new RawPress { Id = id, Gamepad = fromGamepad };
        }

        public void Release(InputActionId id)
        {
            _held[(int)id] = false;
        }

        public bool IsHeld(InputActionId id) => _held[(int)id];

        public void Tick(in SimulationFrame frame)
        {
            int f = frame.Index;
            for (int i = 0; i < _queued; i++) Resolve(_queue[i], f);
            _queued = 0;

            if (_pendingShoulder != InputActionId.None && f - _pendingFrame >= ChordWindowFrames)
            {
                Buffer.Push(_pendingShoulder, _pendingFrame);
                _pendingShoulder = InputActionId.None;
            }

            // Holding LMB repeats the combo (06 §3.4): keep one Basic entry alive while held.
            if (_held[(int)InputActionId.Basic] && !Buffer.Contains(InputActionId.Basic, f))
                Buffer.Push(InputActionId.Basic, f);
        }

        private void Resolve(RawPress p, int f)
        {
            bool shoulder = p.Gamepad && (p.Id == InputActionId.Skill1 || p.Id == InputActionId.Skill2);
            if (!shoulder)
            {
                Buffer.Push(p.Id, f);
                return;
            }
            if (_pendingShoulder != InputActionId.None && _pendingShoulder != p.Id)
            {
                _pendingShoulder = InputActionId.None;
                Buffer.Push(InputActionId.Ultimate, f);
                return;
            }
            if (_pendingShoulder != InputActionId.None) Buffer.Push(_pendingShoulder, _pendingFrame); // same shoulder re-pressed
            _pendingShoulder = p.Id;
            _pendingFrame = f;
        }

        /// <summary>Subscribes this reader to the Gameplay map of <paramref name="input"/>. Returns an unbind callback target via <see cref="Unbind"/>.</summary>
        public void Bind(EclipseInput input)
        {
            var g = input.Gameplay;
            Hook(g.Basic, InputActionId.Basic);
            Hook(g.Heavy, InputActionId.Heavy);
            Hook(g.Dodge, InputActionId.Dodge);
            Hook(g.Skill1, InputActionId.Skill1);
            Hook(g.Skill2, InputActionId.Skill2);
            Hook(g.Skill3, InputActionId.Skill3);
            Hook(g.Skill4, InputActionId.Skill4);
            Hook(g.Ultimate, InputActionId.Ultimate);
            Hook(g.Flask, InputActionId.Flask);
            Hook(g.Interact, InputActionId.Interact);
            g.Move.performed += c => Move = c.ReadValue<Vector2>();
            g.Move.canceled += _ => Move = Vector2.zero;
            g.Aim.performed += c =>
            {
                Aim = c.ReadValue<Vector2>();
                AimIsGamepad = c.control.device is Gamepad;
            };
        }

        private void Hook(InputAction action, InputActionId id)
        {
            action.started += c => Press(id, c.control.device is Gamepad);
            action.canceled += _ => Release(id);
        }
    }
}
