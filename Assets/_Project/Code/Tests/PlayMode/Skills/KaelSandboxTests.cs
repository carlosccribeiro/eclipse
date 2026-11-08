#if UNITY_EDITOR
using System.Collections;
using Eclipse.Data.Skills;
using Eclipse.Gameplay.Combat;
using Eclipse.Gameplay.Combat.Integration;
using Eclipse.Gameplay.Heroes.Kael;
using Eclipse.Gameplay.Input;
using Eclipse.Gameplay.Player;
using Eclipse.Gameplay.Simulation;
using Eclipse.Gameplay.Skills;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eclipse.Tests.PlayMode.Skills
{
    /// <summary>M08 T7: the Combat Sandbox loads with the complete Kael (Fury, five skills, five weapons, Ashen Guard, VFX, test attacker).</summary>
    public class KaelSandboxTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox/CombatSandbox.unity";

        private static IEnumerator LoadSandbox()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_loads_with_complete_Kael()
        {
            yield return LoadSandbox();

            var world = CombatWorld.Current;
            Assert.IsNotNull(world, "CombatWorld missing");
            var kael = Object.FindFirstObjectByType<KaelHeroRig>();
            Assert.IsNotNull(kael, "KaelHeroRig missing");
            Assert.IsTrue(kael.Skills.IsReady, "SkillCasterRig not set up");
            Assert.IsNotNull(kael.Fury, "Fury missing");
            Assert.IsNotNull(kael.Passives, "passives missing");
            Assert.IsNotNull(kael.Guard, "Ashen Guard receiver not bound to the hurtbox");
            Assert.AreEqual(5, kael.WeaponCount);
            foreach (SkillSlot slot in new[] { SkillSlot.Q, SkillSlot.E, SkillSlot.R, SkillSlot.Shift, SkillSlot.F })
                Assert.IsNotNull(kael.Skills.Specs.Get(slot), "no skill spec in slot " + slot);
            Assert.IsNotNull(Object.FindFirstObjectByType<KaelSkillVfx>(), "VFX placeholder component missing");
            Assert.IsNotNull(GameObject.Find("TrainingAttacker_AshenGuard"), "Ashen Guard test attacker missing");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Five_skills_and_five_weapons_run_without_errors()
        {
            yield return LoadSandbox();

            var world = CombatWorld.Current;
            var services = Object.FindFirstObjectByType<GameplayServices>();
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            var kael = Object.FindFirstObjectByType<KaelHeroRig>();
            var cam = UnityEngine.Camera.main;
            Assert.IsNotNull(cam);
            var overlay = Object.FindFirstObjectByType<CombatDebugOverlay>();
            if (overlay != null) overlay.Visible = false;
            foreach (var a in Object.FindObjectsByType<TrainingAttacker>(FindObjectsSortMode.None)) a.AttackEnabled = false;

            Vector3 target = new Vector3(0f, 0f, 6f); // TrainingDummy_Medium
            motor.Teleport(target + new Vector3(0f, 0f, -2f));
            motor.SetFacing(Vector3.forward);

            // Each weapon can be equipped (and the swap rebuilds the skill specs with its kit bonus).
            for (int w = 0; w < kael.WeaponCount; w++)
            {
                kael.Equip(w, force: true);
                Assert.AreEqual(w, kael.WeaponIndex);
                for (int i = 0; i < 4; i++) { services.Scheduler.StepManual(1); world.Events.EndFrame(); }
            }
            kael.Equip(0, force: true);

            // One cast of every slot.
            var slots = new[]
            {
                (InputActionId.Skill1, "Q"), (InputActionId.Skill2, "E"), (InputActionId.Skill4, "Shift"),
                (InputActionId.Skill3, "R"), (InputActionId.Ultimate, "F"),
            };
            foreach (var (input, label) in slots)
            {
                kael.Fury.Pool.Add(200f);
                motor.Teleport(target + new Vector3(0f, 0f, -2f));
                motor.SetFacing(Vector3.forward);
                int casts = 0;
                services.InputReader.Press(input);
                for (int i = 0; i < 400; i++)
                {
                    services.InputReader.Aim = cam.WorldToScreenPoint(target);
                    if (i == 10) services.InputReader.Release(input); // R is a held stance: release ends it
                    services.Scheduler.StepManual(1);
                    int started = world.Events.Count<OnSkillCast>();
                    if (started > 0)
                    {
                        casts += started;
                        yield return null; // a real frame: LateUpdate lets the VFX read the cast and the flusher clear the stream
                        continue;
                    }
                    world.ProcessEvents();
                    world.Events.EndFrame();
                    if (i > 5 && casts > 0 && !kael.Skills.Controller.IsCasting) break;
                }
                services.InputReader.Release(input);
                Assert.AreEqual(1, casts, "skill " + label + " should cast exactly once");
            }
            Assert.Greater(kael.GetComponent<KaelSkillVfx>().PlayedCount, 0, "skill VFX should have played");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
