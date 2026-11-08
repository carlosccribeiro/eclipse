#if UNITY_EDITOR
using EntityId = Eclipse.Gameplay.Combat.EntityId;
using System.Collections.Generic;
using Eclipse.Core.Stats;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests.PlayMode.Combat
{
    /// <summary>Loaded sandbox with the player facing a dummy 1.8 m away and counter-attacks disabled; steps ticks by hand.</summary>
    public sealed class SandboxCombatRun
    {
        public CombatWorld World;
        public GameplayServices Services;
        public CombatActor Player, Dummy;
        public Vector3 DummyPos;
        public readonly List<OnHitDealt> PlayerHits = new List<OnHitDealt>(8192);
        public bool SawAttackerFrozen, SawTargetFrozen, SawStaggered;
        private UnityEngine.Camera _cam;
        private float _savedTimeScale;

        public void Setup(string dummyName, bool noCrit)
        {
            World = CombatWorld.Current;
            Assert.IsNotNull(World, "CombatWorld missing");
            Services = Object.FindFirstObjectByType<GameplayServices>();
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            Player = World.Player;
            Dummy = SandboxKit.FindDummy(World, dummyName);
            Assert.IsNotNull(Dummy, dummyName);
            var overlay = Object.FindFirstObjectByType<CombatDebugOverlay>();
            if (overlay != null) overlay.Visible = false;
            foreach (var a in Object.FindObjectsByType<TrainingAttacker>(FindObjectsSortMode.None)) a.AttackEnabled = false;
            Dummy.ArmorPercent = 0f;
            if (noCrit) Player.Entity.Stats.SetBase(StatId.CriticalChance, 0f);

            DummyPos = Dummy.transform.position;
            motor.Teleport(DummyPos + new Vector3(0f, 0f, -1.8f));
            motor.SetFacing(Vector3.forward);
            _cam = UnityEngine.Camera.main;
            Assert.IsNotNull(_cam);
            // The real FixedUpdate must not add ticks on top of the manual ones.
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        public void Restore() => Time.timeScale = _savedTimeScale;

        /// <summary>One simulation tick plus the per-rendered-frame work (feedback processing and event flush).</summary>
        public void Tick()
        {
            Services.InputReader.Aim = _cam.WorldToScreenPoint(DummyPos);
            Services.Scheduler.StepManual(1);
            var hits = World.Events.Read<OnHitDealt>();
            for (int i = 0; i < hits.Count; i++)
                if (hits[i].Source == Player.Entity.Id) PlayerHits.Add(hits[i]);
            World.ProcessEvents();
            SawAttackerFrozen |= Player.Entity.Clock.IsFrozen;
            SawTargetFrozen |= Dummy.Entity.Clock.IsFrozen;
            SawStaggered |= Dummy.Stagger != null && Dummy.Stagger.IsStaggered;
            World.Events.EndFrame();
        }

        public void Press(InputActionId id) => Services.InputReader.Press(id);
        public void Release(InputActionId id) => Services.InputReader.Release(id);
    }
}
#endif
