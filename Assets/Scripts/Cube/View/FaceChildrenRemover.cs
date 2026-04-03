using System.Collections.Generic;
using UnityEngine;

public class FaceChildrenRemover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CubeManager cubeManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField, Tooltip("Root that contains the 8 cube pieces (e.g., CubeHolder/Cube).")]
    private Transform cubeRoot;
    [Header("Platform Templates (Per Face)")]
    [SerializeField] private Transform platformTemplateFront;
    // --- ADDED ---
    [SerializeField] private Transform[] platformTemplatesFrontRandom;
    [SerializeField] private Transform platformTemplateBack;
    // --- ADDED ---
    [SerializeField] private Transform[] platformTemplatesBackRandom;
    [SerializeField] private Transform platformTemplateLeft;
    // --- ADDED ---
    [SerializeField] private Transform[] platformTemplatesLeftRandom;
    [SerializeField] private Transform platformTemplateRight;
    // --- ADDED ---
    [SerializeField] private Transform[] platformTemplatesRightRandom;
    [SerializeField] private Transform platformTemplateUp;
    [SerializeField] private Transform[] platformTemplatesUpRandom;
    [SerializeField] private Transform platformTemplateDown;
    // --- ADDED ---
    [SerializeField] private Transform[] platformTemplatesDownRandom;
    // --- ADDED ---
    [Header("Platform Mask (Per Face)")]
    [SerializeField, Tooltip("Optional: spawned as a child under each face transform.")]
    private Transform platformMaskTemplate;
    [SerializeField] private string spawnedMaskName = "PlatformMask";
    [SerializeField] private bool spawnMaskOnRespawn = true;

    [Header("Face Names")]
    [SerializeField] private string[] faceNames = new[] { "front", "back", "left", "right", "up", "down" };
    [SerializeField] private bool includeInactive = true;

    [Header("Behavior")]
    [SerializeField] private bool removeOnRotation = true;
    [SerializeField, Tooltip("Seconds to wait before respawning platform children after removal.")]
    private float respawnDelaySeconds = 0.05f;
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    /// <summary>Invoked after platforms are respawned for a rotation.</summary>
    public event System.Action OnPlatformsRespawned;

    private void Awake()
    {
        if (cubeManager == null) cubeManager = GetComponent<CubeManager>();
        if (cubeRoot == null) cubeRoot = transform;
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
    }

    private void OnEnable()
    {
        if (cubeManager != null && removeOnRotation)
        {
            cubeManager.OnRotationStart += HandleRotationStart;
            cubeManager.OnRotationComplete += HandleRotationComplete;
        }
    }

    private void OnDisable()
    {
        if (cubeManager != null)
        {
            cubeManager.OnRotationStart -= HandleRotationStart;
            cubeManager.OnRotationComplete -= HandleRotationComplete;
        }
    }

    [ContextMenu("Remove Face Children (All)")]
    public void RemoveAllFaceChildren()
    {
        if (cubeRoot == null)
        {
            Log("Cube root is not assigned.");
            return;
        }

        int removed = RemoveFaceChildren(null);
        Log($"Removed {removed} face children (all pieces).");
    }

    private void HandleRotationStart(CubeManager.Axis axis, int layerIndex, bool clockwise)
    {
        playerController?.Freeze();
        HashSet<CubePiece> affected = CollectPiecesInLayer(axis, layerIndex);
        int removed = RemoveFaceChildren(affected);
        Log($"Removed {removed} face children for rotated layer.");
    }

    private void HandleRotationComplete(CubeManager.Axis axis, int layerIndex, bool clockwise)
    {
        StartCoroutine(RespawnAfterDelay(axis, layerIndex));
    }

    private System.Collections.IEnumerator RespawnAfterDelay(CubeManager.Axis axis, int layerIndex)
    {
        if (respawnDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(respawnDelaySeconds);
        }

        HashSet<CubePiece> affected = CollectPiecesInLayer(axis, layerIndex);
        int spawned = RespawnPlatforms(affected);
        Log($"Respawned {spawned} platform groups for rotated layer.");
        OnPlatformsRespawned?.Invoke();
        playerController?.Unfreeze();
    }

    private int RemoveFaceChildren(HashSet<CubePiece> onlyPieces)
    {
        int removed = 0;
        Transform[] all = cubeRoot.GetComponentsInChildren<Transform>(includeInactive);
        foreach (Transform t in all)
        {
            if (t == null) continue;
            if (!IsFaceTransform(t.name)) continue;

            if (onlyPieces != null)
            {
                CubePiece owner = t.GetComponentInParent<CubePiece>();
                if (owner == null || !onlyPieces.Contains(owner)) continue;
            }

            // Remove all children under this face (but not the face itself)
            List<Transform> toRemove = new List<Transform>();
            foreach (Transform child in t)
            {
                toRemove.Add(child);
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                if (toRemove[i] == null) continue;
                Destroy(toRemove[i].gameObject);
                removed++;
            }
        }
        return removed;
    }

    private int RespawnPlatforms(HashSet<CubePiece> onlyPieces)
    {
        int spawned = 0;
        Transform[] all = cubeRoot.GetComponentsInChildren<Transform>(includeInactive);
        foreach (Transform t in all)
        {
            if (t == null) continue;
            if (!IsFaceTransform(t.name)) continue;

            if (onlyPieces != null)
            {
                CubePiece owner = t.GetComponentInParent<CubePiece>();
                if (owner == null || !onlyPieces.Contains(owner)) continue;
            }

            Transform template = GetTemplateForFace(t.name);
            if (template == null) continue;

            // --- ADDED ---
            if (spawnMaskOnRespawn && platformMaskTemplate != null)
            {
                SpawnMaskUnderFace(t);
            }

            // --- CHANGED ---
            GameObject clone = Instantiate(template.gameObject, t);
            clone.name = template.name;
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            clone.SetActive(true);
            spawned++;
        }

        return spawned;
    }

    // --- ADDED ---
    private void SpawnMaskUnderFace(Transform face)
    {
        if (face == null || platformMaskTemplate == null) return;

        Transform existing = face.Find(spawnedMaskName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject clone = Instantiate(platformMaskTemplate.gameObject, face);
        clone.name = spawnedMaskName;
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.localScale = Vector3.one;
        clone.SetActive(true);
    }

    private Transform GetTemplateForFace(string faceName)
    {
        if (string.Equals(faceName, "front", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesFrontRandom, platformTemplateFront);
        if (string.Equals(faceName, "back", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesBackRandom, platformTemplateBack);
        if (string.Equals(faceName, "left", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesLeftRandom, platformTemplateLeft);
        if (string.Equals(faceName, "right", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesRightRandom, platformTemplateRight);
        if (string.Equals(faceName, "up", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesUpRandom, platformTemplateUp);
        if (string.Equals(faceName, "down", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesDownRandom, platformTemplateDown);
        return null;
    }

    // --- ADDED ---
    private Transform GetRandomTemplate(Transform[] pool, Transform fallback)
    {
        if (pool != null && pool.Length > 0)
        {
            int index = Random.Range(0, pool.Length);
            return pool[index];
        }

        return fallback;
    }

    private HashSet<CubePiece> CollectPiecesInLayer(CubeManager.Axis axis, int layerIndex)
    {
        HashSet<CubePiece> result = new HashSet<CubePiece>();
        if (cubeRoot == null) return result;

        CubePiece[] pieces = cubeRoot.GetComponentsInChildren<CubePiece>(includeInactive);
        for (int i = 0; i < pieces.Length; i++)
        {
            CubePiece piece = pieces[i];
            if (piece == null) continue;
            Vector3Int pos = piece.GridPosition;
            bool match = axis == CubeManager.Axis.X ? pos.x == layerIndex
                : axis == CubeManager.Axis.Y ? pos.y == layerIndex
                : pos.z == layerIndex;
            if (match) result.Add(piece);
        }
        return result;
    }


    private bool IsFaceTransform(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        for (int i = 0; i < faceNames.Length; i++)
        {
            if (string.Equals(name, faceNames[i], System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }
}
