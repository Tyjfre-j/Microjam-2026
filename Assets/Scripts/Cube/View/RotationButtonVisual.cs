using UnityEngine;

/// <summary>
/// Neon holographic look for a rotation button: a glowing pad plus an arrow icon that
/// points in the direction the slice will move. Lives next to the button (unscaled parent)
/// so the button's non-uniform collider scale never skews the visuals.
/// </summary>
public class RotationButtonVisual : MonoBehaviour
{
    private static Material s_PadMat;
    private static Material s_ArrowMat;
    private static Mesh s_Quad;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ActiveId = Shader.PropertyToID("_Active");

    [SerializeField] private float padSize = 0.085f;
    [SerializeField] private float arrowSize = 0.06f;
    [SerializeField] private float hoverHeight = 0.012f;

    private Transform root;
    private Transform pad;
    private Transform arrow;
    private MeshRenderer padRenderer;
    private MeshRenderer arrowRenderer;
    private MaterialPropertyBlock mpb;
    private CubeRotationButtonTrigger trigger;
    private Color color;
    private float active;
    private float spawnT;

    public void Setup(Transform visualParent, Vector3 arrowDirWorld, Vector3 faceNormalWorld, Color tint)
    {
        EnsureShared();
        if (trigger == null) trigger = GetComponent<CubeRotationButtonTrigger>();
        if (mpb == null) mpb = new MaterialPropertyBlock();

        // Hide the old grey primitive, keep its trigger collider.
        MeshRenderer own = GetComponent<MeshRenderer>();
        if (own != null) own.enabled = false;

        if (root == null)
        {
            root = new GameObject(name + "_Visual").transform;
            pad = CreateQuad("Pad", s_PadMat, out padRenderer);
            arrow = CreateQuad("Arrow", s_ArrowMat, out arrowRenderer);
        }

        root.SetParent(visualParent, true);
        root.position = transform.position + faceNormalWorld.normalized * hoverHeight;
        // Quad front faces -Z, so look into the cube; +Y = arrow direction.
        root.rotation = Quaternion.LookRotation(-faceNormalWorld, arrowDirWorld);
        root.localScale = Vector3.one;

        pad.localPosition = Vector3.zero;
        pad.localRotation = Quaternion.identity;
        pad.localScale = Vector3.one * padSize;
        arrow.localPosition = Vector3.back * 0.002f;
        arrow.localRotation = Quaternion.identity;
        arrow.localScale = Vector3.one * arrowSize;

        color = tint;
        spawnT = 0f;
        root.gameObject.SetActive(true);
        Apply();
    }

    private Transform CreateQuad(string n, Material m, out MeshRenderer mr)
    {
        var go = new GameObject(n);
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = s_Quad;
        mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = m;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return go.transform;
    }

    private static void EnsureShared()
    {
        if (s_Quad == null)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            s_Quad = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
        }
        if (s_PadMat == null)
        {
            var sh = Shader.Find("Polish/NeonIcon");
            s_PadMat = new Material(sh) { name = "NeonPad (runtime)" };
            s_PadMat.SetFloat("_Mode", 0f);
            s_ArrowMat = new Material(sh) { name = "NeonArrow (runtime)" };
            s_ArrowMat.SetFloat("_Mode", 1f);
        }
    }

    private void OnEnable() { if (root != null) root.gameObject.SetActive(true); }
    private void OnDisable() { if (root != null) root.gameObject.SetActive(false); }
    private void OnDestroy() { if (root != null) Destroy(root.gameObject); }

    private void LateUpdate()
    {
        if (root == null) return;
        bool near = trigger != null && trigger.IsPlayerNearby;
        float target = near ? 1f : 0f;
        float prev = active;
        active = Mathf.MoveTowards(active, target, Time.deltaTime * 6f);

        // Pop-in on (re)spawn + gentle squash when the player steps on.
        spawnT = Mathf.Min(1f, spawnT + Time.deltaTime * 4f);
        float pop = 1f - Mathf.Pow(1f - spawnT, 3f);
        float overshoot = 1f + Mathf.Sin(spawnT * Mathf.PI) * 0.15f;
        root.localScale = Vector3.one * (pop * overshoot * (1f + active * 0.12f));

        if (!Mathf.Approximately(prev, active) || spawnT < 1f) Apply();
    }

    private void Apply()
    {
        Color c = color * (1f + active * 0.8f);
        mpb.SetColor(ColorId, c);
        mpb.SetFloat(ActiveId, active);
        padRenderer.SetPropertyBlock(mpb);
        arrowRenderer.SetPropertyBlock(mpb);
    }
}
