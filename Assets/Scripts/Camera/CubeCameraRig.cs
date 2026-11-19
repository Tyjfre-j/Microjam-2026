using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Smooth camera for the cube platformer.
/// - Frames one cube face (keeps the authored tilt from the scene camera).
/// - Gently follows the player inside the face plane with damping + look-ahead.
/// - Orbits smoothly to any other face (GoToFace) and drops the player onto a platform there.
/// - Small screen-shake on every slice rotation.
/// </summary>
[DefaultExecutionOrder(100)]
public class CubeCameraRig : MonoBehaviour
{
    public enum Face { Front, Back, Right, Left, Top, Bottom }

    [Header("References")]
    [SerializeField] private Transform cube;            // CubeHolder
    [SerializeField] private Transform platformsRoot;   // Cube_platforms
    [SerializeField] private PlayerController player;
    [SerializeField] private RotationAnimator rotationAnimator;

    [Header("Framing")]
    [SerializeField] private float distance = 3.9f;
    [SerializeField] private float followStrength = 0.3f;
    [SerializeField] private float maxFollowOffset = 0.35f;
    [SerializeField] private float lookAhead = 0.12f;
    [SerializeField] private float followSmoothTime = 0.28f;
    [SerializeField] private float fieldOfView = 50f;

    [Header("Face Transition")]
    [SerializeField] private float transitionDuration = 0.9f;

    [Header("Juice")]
    [SerializeField] private float idleSway = 0.012f;
    [SerializeField] private float shakeOnRotate = 0.035f;

    public Face CurrentFace { get; private set; }
    public bool IsTransitioning => transitionT < 1f;
    public event System.Action<Face> OnFaceChanged;

    // Authored camera offset expressed in face space (right, up, normal).
    private Vector3 faceSpaceOffset;
    private Quaternion fromRot, toRot;
    private float transitionT = 1f;
    private Vector3 followOffset, followVel;
    private float shake;
    private Camera cam;
    private Rigidbody playerRb;

    private static readonly Vector3[] LocalNormals =
        { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cube == null) { var go = GameObject.Find("CubeHolder"); if (go) cube = go.transform; }
        if (platformsRoot == null && cube != null) platformsRoot = cube.Find("Cube_platforms");
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (rotationAnimator == null) rotationAnimator = FindAnyObjectByType<RotationAnimator>();
        if (player != null) playerRb = player.GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (cube == null) { enabled = false; return; }
        if (cam != null) cam.fieldOfView = fieldOfView;

        // Detect which face the authored camera looks at, keep its tilt as the house style.
        Vector3 toCam = transform.position - cube.position;
        CurrentFace = BestFace(toCam);
        Quaternion basis = FaceBasis(CurrentFace, Vector3.up);
        Vector3 local = Quaternion.Inverse(basis) * toCam.normalized;
        local.z = Mathf.Max(local.z, 0.6f);
        faceSpaceOffset = local.normalized;
        fromRot = toRot = basis;
        transitionT = 1f;
    }

    private void OnEnable()
    {
        if (rotationAnimator != null) rotationAnimator.OnRotationStart += Shake;
    }

    private void OnDisable()
    {
        if (rotationAnimator != null) rotationAnimator.OnRotationStart -= Shake;
    }

    private void Shake() => shake = shakeOnRotate;

    private void LateUpdate()
    {
        if (cube == null) return;

        Quaternion basis;
        if (transitionT < 1f)
        {
            transitionT = Mathf.Min(1f, transitionT + Time.unscaledDeltaTime / transitionDuration);
            float e = transitionT < 0.5f ? 4f * transitionT * transitionT * transitionT
                                         : 1f - Mathf.Pow(-2f * transitionT + 2f, 3f) / 2f;
            basis = Quaternion.Slerp(fromRot, toRot, e);
        }
        else basis = toRot;

        // Follow target inside the face plane.
        Vector3 desired = Vector3.zero;
        if (player != null && transitionT >= 1f)
        {
            Vector3 rel = player.transform.position - cube.position;
            Vector3 right = basis * Vector3.right, up = basis * Vector3.up;
            Vector2 p = new Vector2(Vector3.Dot(rel, right), Vector3.Dot(rel, up));
            if (playerRb != null)
            {
                Vector3 v = playerRb.linearVelocity;
                p += new Vector2(Vector3.Dot(v, right), Vector3.Dot(v, up) * 0.4f) * lookAhead;
            }
            p = Vector2.ClampMagnitude(p * followStrength, maxFollowOffset);
            desired = right * p.x + up * p.y;
        }
        followOffset = Vector3.SmoothDamp(followOffset, desired, ref followVel, followSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        Vector3 focus = cube.position + followOffset;
        Vector3 pos = focus + basis * faceSpaceOffset * distance;

        // Idle sway + shake.
        float t = Time.unscaledTime;
        Vector3 sway = (basis * new Vector3(Mathf.Sin(t * 0.37f), Mathf.Sin(t * 0.53f) * 0.6f, 0f)) * idleSway;
        if (shake > 0f)
        {
            sway += Random.insideUnitSphere * shake;
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 0.15f);
        }

        transform.position = pos + sway;
        transform.rotation = Quaternion.LookRotation(focus - transform.position, basis * Vector3.up);
    }

    // ── Face teleport ─────────────────────────────────────────────
    public bool GoToFace(Face face)
    {
        if (cube == null || IsTransitioning) return false;
        if (rotationAnimator != null && rotationAnimator.isAnimating) return false;

        Vector3 normalW = cube.TransformDirection(LocalNormals[(int)face]).normalized;
        Vector3 upW = FindFaceUp(normalW, out Collider target);

        // Face basis: forward = outward face normal, up = gravity-up for that face.
        fromRot = toRot;
        toRot = Quaternion.LookRotation(normalW, upW);
        transitionT = 0f;
        followOffset = Vector3.zero; followVel = Vector3.zero;

        if (player != null && target != null)
        {
            Bounds b = target.bounds;
            float halfUp = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(upW.x), Mathf.Abs(upW.y), Mathf.Abs(upW.z))));
            Vector3 spawn = b.center + upW * (halfUp + 0.05f);
            player.TeleportTo(spawn, Quaternion.LookRotation(normalW, upW));
        }

        CurrentFace = face;
        OnFaceChanged?.Invoke(face);
        return true;
    }

    private Face BestFace(Vector3 dirWorld)
    {
        int best = 0; float bestDot = -2f;
        for (int i = 0; i < 6; i++)
        {
            float d = Vector3.Dot(cube.TransformDirection(LocalNormals[i]), dirWorld.normalized);
            if (d > bestDot) { bestDot = d; best = i; }
        }
        return (Face)best;
    }

    private Quaternion FaceBasis(Face face, Vector3 preferredUp)
    {
        Vector3 n = cube.TransformDirection(LocalNormals[(int)face]).normalized;
        Vector3 up = Vector3.ProjectOnPlane(preferredUp, n);
        if (up.sqrMagnitude < 1e-3f) up = Vector3.ProjectOnPlane(cube.forward, n);
        return Quaternion.LookRotation(n, up.normalized);
    }

    private readonly List<Collider> faceColliders = new List<Collider>(64);

    /// <summary>
    /// Picks the "gravity up" for a face from how its platforms are oriented (majority vote),
    /// and returns the platform closest to the face centre as the spawn target.
    /// </summary>
    private Vector3 FindFaceUp(Vector3 normalW, out Collider target)
    {
        target = null;
        faceColliders.Clear();
        Transform root = platformsRoot != null ? platformsRoot : cube;
        foreach (Collider c in root.GetComponentsInChildren<Collider>(false))
        {
            if (c.isTrigger || !c.name.StartsWith("platform")) continue;
            if (Vector3.Dot(c.bounds.center - cube.position, normalW) > 0.85f) faceColliders.Add(c);
        }

        Vector3 ref1 = Vector3.ProjectOnPlane(Vector3.up, normalW);
        if (ref1.sqrMagnitude < 1e-3f) ref1 = Vector3.ProjectOnPlane(cube.forward, normalW);
        ref1.Normalize();
        Vector3 ref2 = Vector3.Cross(normalW, ref1);
        Vector3[] candidates = { ref1, -ref1, ref2, -ref2 };
        float[] votes = new float[4];
        foreach (Collider c in faceColliders)
        {
            Vector3 u = c.transform.up;
            for (int k = 0; k < 4; k++) if (Vector3.Dot(u, candidates[k]) > 0.7f) votes[k] += 1f;
        }
        int bestK = 0;
        for (int k = 1; k < 4; k++) if (votes[k] > votes[bestK]) bestK = k;
        Vector3 up = candidates[bestK];

        // Spawn on a wide-enough platform near the middle-low part of the face.
        float bestScore = float.MaxValue;
        foreach (Collider c in faceColliders)
        {
            Vector3 rel = Vector3.ProjectOnPlane(c.bounds.center - cube.position, normalW);
            float width = c.bounds.size.magnitude;
            float score = rel.magnitude - width * 0.5f + Vector3.Dot(rel, up) * 0.3f;
            if (score < bestScore) { bestScore = score; target = c; }
        }
        return up;
    }
}
