using UnityEngine;

namespace Eclipse.Gameplay.Entities
{
    /// <summary>Trivial <see cref="IMassBody"/> for sandbox dummies and tests (real enemies get their own implementation in M09).</summary>
    public sealed class MassBodyMarker : MonoBehaviour, IMassBody
    {
        [SerializeField] private MassClass massClass = MassClass.Medium;
        [SerializeField] private bool isElite;

        public MassClass MassClass { get => massClass; set => massClass = value; }
        public bool IsElite { get => isElite; set => isElite = value; }
    }
}
