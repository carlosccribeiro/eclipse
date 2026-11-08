using Eclipse.Data.Enemies;
using UnityEngine;

namespace Eclipse.AI.Telegraphs
{
    /// <summary>Geometry and look of one telegraph to show.</summary>
    public struct TelegraphRequest
    {
        public TelegraphVisual visual;
        public TelegraphShape shape;
        /// <summary>Ground position of the shape origin (circle/ring center, cone apex, line start).</summary>
        public Vector3 origin;
        /// <summary>Yaw in degrees around Y; 0 points to +Z.</summary>
        public float yawDegrees;
        public float radius;
        public float arcDegrees;
        public float length;
        public float width;
        public float innerRadius;
    }

    /// <summary>
    /// Greybox telegraph on the ground: a quad with the Eclipse/TelegraphFill shader (fallback of the plan: no Decal Projector,
    /// same API). Fill grows from center to border; impact is when progress reaches 1. Renders above player VFX:
    /// queue Transparent+100 (3100) and sorting order 500; player VFX materials must stay below queue 3100 (06 section 3.10).
    /// Views are created once by <see cref="TelegraphViewPool"/>; nothing is instantiated per telegraph.
    /// </summary>
    public sealed class TelegraphView : MonoBehaviour
    {
        public const int SortingOrder = 500;
        private const float GroundLift = 0.03f;

        private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        private static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int ShapeId = Shader.PropertyToID("_Shape");
        private static readonly int HalfArcCosId = Shader.PropertyToID("_HalfArcCos");
        private static readonly int InnerRatioId = Shader.PropertyToID("_InnerRatio");
        private static readonly int DashedId = Shader.PropertyToID("_Dashed");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private float _progress;

        public bool InUse { get; internal set; }
        public float Progress => _progress;

        internal void Init(Mesh quad, Material material)
        {
            gameObject.AddComponent<MeshFilter>().sharedMesh = quad;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.sortingOrder = SortingOrder;
            _block = new MaterialPropertyBlock();
            _renderer.enabled = false;
        }

        public void Show(in TelegraphRequest request, TelegraphPalette palette)
        {
            float sizeX = request.radius * 2f;
            float sizeZ = sizeX;
            Vector3 centerOffset = Vector3.zero;
            if (request.shape == TelegraphShape.Line)
            {
                sizeX = request.width;
                sizeZ = request.length;
                centerOffset = new Vector3(0f, 0f, request.length * 0.5f);
            }

            Quaternion rot = Quaternion.Euler(0f, request.yawDegrees, 0f);
            transform.SetPositionAndRotation(request.origin + rot * centerOffset + new Vector3(0f, GroundLift, 0f), rot);
            transform.localScale = new Vector3(sizeX, 1f, sizeZ);

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(FillColorId, palette.Fill(request.visual));
            _block.SetColor(BorderColorId, palette.Border(request.visual));
            _block.SetFloat(ShapeId, (float)(int)request.shape);
            _block.SetFloat(HalfArcCosId, Mathf.Cos(request.arcDegrees * 0.5f * Mathf.Deg2Rad));
            _block.SetFloat(InnerRatioId, request.radius > 0f ? Mathf.Clamp01(request.innerRadius / request.radius) : 0f);
            _block.SetFloat(DashedId, request.visual == TelegraphVisual.Annihilation ? 1f : 0f);
            _block.SetFloat(PulseId, request.visual == TelegraphVisual.UnstoppableOutline ? 1f : 0f);
            _progress = 0f;
            _block.SetFloat(ProgressId, 0f);
            _renderer.SetPropertyBlock(_block);
            _renderer.enabled = true;
        }

        public void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(ProgressId, _progress);
            _renderer.SetPropertyBlock(_block);
        }

        public void Hide()
        {
            if (_renderer != null) _renderer.enabled = false;
        }
    }
}
