using System.Collections.Generic;
using Eclipse.Gameplay.Physics;
using NUnit.Framework;
using UnityEngine;
using UPhysics = UnityEngine.Physics;

namespace Eclipse.Tests.EditMode.Gameplay.Physics
{
    public class PhysicsLayersTests
    {
        private static readonly int[] Custom =
        {
            PhysicsLayers.Character, PhysicsLayers.Environment, PhysicsLayers.Wall, PhysicsLayers.LowCover,
            PhysicsLayers.HurtboxPlayer, PhysicsLayers.HurtboxEnemy, PhysicsLayers.HitboxPlayer,
            PhysicsLayers.HitboxEnemy, PhysicsLayers.Projectile, PhysicsLayers.Interactable,
        };

        [Test]
        public void Constants_MatchTagManagerNames()
        {
            for (int i = 0; i < Custom.Length; i++)
            {
                Assert.AreEqual(Custom[i], LayerMask.NameToLayer(PhysicsLayers.Names[i]), PhysicsLayers.Names[i]);
                Assert.GreaterOrEqual(LayerMask.NameToLayer(PhysicsLayers.Names[i]), 0);
            }
        }

        [Test]
        public void Matrix_OnlyCharacterPairsCollide()
        {
            var expected = new HashSet<(int, int)>
            {
                (PhysicsLayers.Character, PhysicsLayers.Character),
                (PhysicsLayers.Character, PhysicsLayers.Environment),
                (PhysicsLayers.Character, PhysicsLayers.Wall),
                (PhysicsLayers.Character, PhysicsLayers.LowCover),
                (0, PhysicsLayers.Character),
            };
            foreach (int a in Custom)
            {
                foreach (int b in Custom)
                {
                    bool collides = expected.Contains((a, b)) || expected.Contains((b, a));
                    Assert.AreEqual(!collides, UPhysics.GetIgnoreLayerCollision(a, b), $"{a} vs {b}");
                }
            }
        }

        [Test]
        public void Hitboxes_NeverCollidePhysically()
        {
            foreach (int h in new[] { PhysicsLayers.HitboxPlayer, PhysicsLayers.HitboxEnemy, PhysicsLayers.HurtboxPlayer, PhysicsLayers.HurtboxEnemy })
                for (int other = 0; other < 32; other++)
                    Assert.IsTrue(UPhysics.GetIgnoreLayerCollision(h, other), $"{h} vs {other}");
        }

        [Test]
        public void Masks_LowCoverBlocksOnlyNonArea()
        {
            Assert.AreEqual(0, PhysicsLayers.ProjectileBlockersMask & PhysicsLayers.LowCoverMask);
            Assert.AreNotEqual(0, PhysicsLayers.ProjectileBlockersNonAreaMask & PhysicsLayers.LowCoverMask);
            Assert.AreEqual(PhysicsLayers.WallMask, PhysicsLayers.LineOfSightMask);
        }
    }
}
