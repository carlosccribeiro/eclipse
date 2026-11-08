using UnityEngine;

namespace Eclipse.Gameplay.Combat.Integration
{
    /// <summary>
    /// Sandbox read-out (HUD is M06): HP, Stagger bar and state above every combatant (IMGUI text + gizmo bar).
    /// Strings are built by <see cref="CombatActor"/> only when a value changes. Toggle with F3 (see SandboxDebugControls).
    /// </summary>
    public sealed class CombatDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private float labelHeight = 2.4f;

        private GUIStyle _style;

        public bool Visible { get => visible; set => visible = value; }

        private void OnGUI()
        {
            var world = CombatWorld.Current;
            if (!visible || world == null || Camera.main == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 12, richText = false };
                _style.normal.textColor = Color.white;
            }
            var cam = Camera.main;
            var actors = world.Actors;
            for (int i = 0; i < actors.Count; i++)
            {
                CombatActor a = actors[i];
                if (a.Entity == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(a.transform.position + Vector3.up * labelHeight);
                if (sp.z <= 0f) continue;
                GUI.Label(new Rect(sp.x - 80f, Screen.height - sp.y - 36f, 160f, 40f), a.DebugLine, _style);
            }
        }

        private void OnDrawGizmos()
        {
            var world = CombatWorld.Current;
            if (!visible || world == null) return;
            var actors = world.Actors;
            for (int i = 0; i < actors.Count; i++)
            {
                CombatActor a = actors[i];
                if (a.Entity == null || a.Stagger == null || a.Stagger.StaggerMax <= 0f) continue;
                Vector3 p = a.transform.position + Vector3.up * (labelHeight - 0.15f);
                float f = Mathf.Clamp01(a.Stagger.StaggerCurrent / a.Stagger.StaggerMax);
                Gizmos.color = Color.black;
                Gizmos.DrawLine(p - Vector3.right * 0.6f, p + Vector3.right * 0.6f);
                Gizmos.color = a.Stagger.IsStaggered ? Color.cyan : Color.yellow;
                Gizmos.DrawLine(p - Vector3.right * 0.6f, p - Vector3.right * 0.6f + Vector3.right * (1.2f * f));
            }
        }
    }
}
