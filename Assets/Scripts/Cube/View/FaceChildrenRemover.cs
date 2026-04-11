using System.Collections.Generic;
using UnityEngine;

public class FaceChildrenRemover : MonoBehaviour
{
    [System.Serializable]
    private class TemplateColorSet
    {
        public string templateName;
        public Material white;
        public Material yellow;
        public Material pink;
        public Material blue;
        public Material green;
        public Material orange;
    }
    [Header("References")]
    [SerializeField] private CubeManager cubeManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField, Tooltip("Root that contains the 8 cube pieces (e.g., CubeHolder/Cube).")]
    private Transform cubeRoot;
    // --- ADDED ---
    [SerializeField, Tooltip("Visible cube used to read face material names.")]
    private CubeVisual visibleCubeVisual;
    [SerializeField] private bool matchFaceMaterialByName = true;
    [SerializeField, Tooltip("All background materials that can be matched by name.")]
    private Material[] materialLibrary;
    [SerializeField, Tooltip("Optional: map template + color to a specific background material.")]
    private TemplateColorSet[] templateColorMaterials;
    [SerializeField, Tooltip("Fallback to materialLibrary (name matching) if template+color mapping is missing.")]
    private bool useMaterialLibraryFallback = false;
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
    [SerializeField, Tooltip("If enabled, platforms are rebuilt once during startup and not on every rotation.")]
    private bool startupOnlyRebuild = true;
    [SerializeField, Tooltip("Spawn platforms only on exterior faces of each cube piece.")]
    private bool respawnOnlyExteriorFaces = true;
    [SerializeField, Tooltip("Seconds to wait before respawning platform children after removal.")]
    private float respawnDelaySeconds = 0.05f;
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showTemplatePickDebug = false;
    [SerializeField] private bool showMaterialMatchDebug = true;

    /// <summary>Invoked after platforms are respawned for a rotation.</summary>
    public event System.Action OnPlatformsRespawned;

    private Coroutine respawnRoutine;
    private int rotationSequence;
    private bool startupBuildCompleted;
    private readonly HashSet<MeshRenderer> hiddenVisibleFaceMeshes = new HashSet<MeshRenderer>();

    public bool StartupOnlyRebuild => startupOnlyRebuild;
    public bool StartupBuildCompleted => startupBuildCompleted;

    private void Awake()
    {
        if (cubeManager == null) cubeManager = GetComponent<CubeManager>();
        if (cubeRoot == null) cubeRoot = transform;
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        // --- ADDED ---
        if (visibleCubeVisual == null) visibleCubeVisual = FindVisibleCubeVisual();
        CacheVisiblePieces();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RegenerateTemplateColorSets(false);
    }
#endif

    // --- ADDED ---
    [ContextMenu("Validate Inspector Setup")]
    public void ValidateInspectorSetup()
    {
        List<string> templateNames = CollectTemplateNames();
        if (templateNames.Count == 0)
        {
            Debug.LogWarning("[FaceChildrenRemover] No platform templates found in inspector lists.");
            return;
        }

        if (visibleCubeVisual == null)
        {
            Debug.LogWarning("[FaceChildrenRemover] visibleCubeVisual is not assigned.");
        }

        Dictionary<string, TemplateColorSet> map = new Dictionary<string, TemplateColorSet>();
        if (templateColorMaterials != null)
        {
            for (int i = 0; i < templateColorMaterials.Length; i++)
            {
                TemplateColorSet set = templateColorMaterials[i];
                if (set == null || string.IsNullOrWhiteSpace(set.templateName)) continue;
                map[CleanMaterialName(set.templateName)] = set;
            }
        }

        for (int i = 0; i < templateNames.Count; i++)
        {
            string tName = templateNames[i];
            string key = CleanMaterialName(tName);
            if (!map.TryGetValue(key, out TemplateColorSet set))
            {
                Debug.LogWarning($"[FaceChildrenRemover] Missing Template Color entry for template '{tName}'.");
                continue;
            }

            ValidateColorSet(key, set);
        }
    }

    // --- ADDED ---
    private static void ValidateColorSet(string templateName, TemplateColorSet set)
    {
        if (set.white == null) Debug.LogWarning($"[FaceChildrenRemover] Template '{templateName}' missing material: white");
        if (set.yellow == null) Debug.LogWarning($"[FaceChildrenRemover] Template '{templateName}' missing material: yellow");
        if (set.pink == null) Debug.LogWarning($"[FaceChildrenRemover] Template '{templateName}' missing material: pink");
        if (set.blue == null) Debug.LogWarning($"[FaceChildrenRemover] Template '{templateName}' missing material: blue");
        if (set.green == null) Debug.LogWarning($"[FaceChildrenRemover] Template '{templateName}' missing material: green");
        if (set.orange == null) Debug.LogWarning($"[FaceChildrenRemover] Template '{templateName}' missing material: orange");
    }

    private void OnEnable()
    {
        if (cubeManager != null && removeOnRotation && !startupOnlyRebuild)
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

        RestoreHiddenVisibleFaceMeshes();

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
    }

    [ContextMenu("Remove Face Children (All)")]
    public void RemoveAllFaceChildren()
    {
        int removed = RemoveAllForStartup();
        Log($"Removed {removed} face children (all pieces).");
    }

    public void ResetStartupBuildState()
    {
        startupBuildCompleted = false;
    }

    public void MarkStartupBuildCompleted()
    {
        startupBuildCompleted = true;
    }

    public int RemoveAllForStartup()
    {
        if (cubeRoot == null)
        {
            Log("Cube root is not assigned.");
            return 0;
        }

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        int removed = RemoveFaceChildren(null);
        RestoreHiddenVisibleFaceMeshes();
        return removed;
    }

    public int RespawnAllForStartup()
    {
        if (cubeRoot == null)
        {
            Log("Cube root is not assigned.");
            return 0;
        }

        CacheVisiblePieces();
        int spawned = RespawnPlatforms(null);
        RestoreHiddenVisibleFaceMeshes();
        OnPlatformsRespawned?.Invoke();
        return spawned;
    }

    private void HandleRotationStart(CubeManager.Axis axis, int layerIndex, bool clockwise)
    {
        if (startupOnlyRebuild)
        {
            return;
        }

        rotationSequence++;
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        playerController?.Freeze();
        HashSet<CubePiece> affected = CollectPiecesInLayer(axis, layerIndex);
        ShowVisibleFaceMeshesForLayer(affected);
        int removed = RemoveFaceChildren(affected);
        Log($"Removed {removed} face children for rotated layer.");
    }

    private void HandleRotationComplete(CubeManager.Axis axis, int layerIndex, bool clockwise)
    {
        if (startupOnlyRebuild)
        {
            return;
        }

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
        }
        int sequence = rotationSequence;
        respawnRoutine = StartCoroutine(RespawnAfterDelay(axis, layerIndex, sequence));
    }

    private System.Collections.IEnumerator RespawnAfterDelay(CubeManager.Axis axis, int layerIndex, int sequence)
    {
        if (respawnDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(respawnDelaySeconds);
        }

        // Ensure rotation + state sync have fully applied before sampling materials.
        yield return null;

        if (sequence != rotationSequence)
        {
            yield break;
        }

        CacheVisiblePieces();

        HashSet<CubePiece> affected = CollectPiecesInLayer(axis, layerIndex);
        int spawned = RespawnPlatforms(affected);
        HideVisibleFaceMeshesForLayer(affected);
        Log($"Respawned {spawned} platform groups for rotated layer.");
        respawnRoutine = null;
        OnPlatformsRespawned?.Invoke();
        playerController?.Unfreeze();
    }

    private void HideVisibleFaceMeshesForLayer(HashSet<CubePiece> affected)
    {
        if (affected == null || affected.Count == 0) return;

        foreach (CubePiece piece in affected)
        {
            if (piece == null) continue;
            if (!visiblePiecesByGrid.TryGetValue(piece.GridPosition, out CubePiece visiblePiece) || visiblePiece == null) continue;

            for (int i = 0; i < faceNames.Length; i++)
            {
                string faceName = faceNames[i];
                Transform faceChild = FindBestVisibleFaceChild(visiblePiece.transform, faceName);
                if (faceChild == null) continue;

                MeshRenderer meshRenderer = faceChild.GetComponent<MeshRenderer>();
                if (meshRenderer == null) continue;

                if (meshRenderer.enabled)
                {
                    meshRenderer.enabled = false;
                    hiddenVisibleFaceMeshes.Add(meshRenderer);
                }
            }
        }
    }

    private void ShowVisibleFaceMeshesForLayer(HashSet<CubePiece> affected)
    {
        if (affected == null || affected.Count == 0) return;

        foreach (CubePiece piece in affected)
        {
            if (piece == null) continue;
            if (!visiblePiecesByGrid.TryGetValue(piece.GridPosition, out CubePiece visiblePiece) || visiblePiece == null) continue;

            for (int i = 0; i < faceNames.Length; i++)
            {
                string faceName = faceNames[i];
                Transform faceChild = FindBestVisibleFaceChild(visiblePiece.transform, faceName);
                if (faceChild == null) continue;

                MeshRenderer meshRenderer = faceChild.GetComponent<MeshRenderer>();
                if (meshRenderer == null) continue;

                meshRenderer.enabled = true;
                hiddenVisibleFaceMeshes.Remove(meshRenderer);
            }
        }
    }

    private void RestoreHiddenVisibleFaceMeshes()
    {
        if (hiddenVisibleFaceMeshes.Count == 0) return;

        foreach (MeshRenderer meshRenderer in hiddenVisibleFaceMeshes)
        {
            if (meshRenderer == null) continue;
            meshRenderer.enabled = true;
        }

        hiddenVisibleFaceMeshes.Clear();
    }

    private int RemoveFaceChildren(HashSet<CubePiece> onlyPieces)
    {
        int removed = 0;
        Transform[] all = cubeRoot.GetComponentsInChildren<Transform>(includeInactive);
        foreach (Transform t in all)
        {
            if (t == null) continue;
            if (!IsFaceTransform(t.name)) continue;

            CubePiece owner = t.GetComponentInParent<CubePiece>();
            if (owner == null) continue;

            // Only accept direct face transforms under the cube piece root.
            if (t.parent != owner.transform) continue;

            // Guard against duplicate face transforms with the same name under one piece.
            Transform canonicalFace = owner.transform.Find(t.name);
            if (canonicalFace != t)
            {
                if (showDebugLogs) Debug.LogWarning($"[FaceChildrenRemover] Duplicate face transform skipped: piece='{owner.name}' face='{t.name}'");
                continue;
            }

            if (onlyPieces != null)
            {
                if (!onlyPieces.Contains(owner)) continue;
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

            CubePiece owner = t.GetComponentInParent<CubePiece>();
            if (owner == null) continue;

            // Only accept direct face transforms under the cube piece root.
            if (t.parent != owner.transform) continue;

            // Guard against duplicate face transforms with the same name under one piece.
            Transform canonicalFace = owner.transform.Find(t.name);
            if (canonicalFace != t)
            {
                if (showDebugLogs) Debug.LogWarning($"[FaceChildrenRemover] Duplicate face transform skipped: piece='{owner.name}' face='{t.name}'");
                continue;
            }

            if (onlyPieces != null)
            {
                if (!onlyPieces.Contains(owner)) continue;
            }

            if (respawnOnlyExteriorFaces && !IsConcernedExteriorFace(owner, t.name))
            {
                ClearChildren(t);
                continue;
            }

            // Defensive clear: if a previous remove pass was skipped/stale, avoid duplicate platform roots.
            ClearChildren(t);

            Transform template = GetTemplateForFace(t.name, out int templateIndex);
            if (template == null) continue;
            if (showTemplatePickDebug)
            {
                Log($"Spawn face '{t.name}' -> template '{template.name}' (index {templateIndex})");
            }

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
            ApplyMaterialFromVisibleFaceName(t, clone);
            spawned++;
        }

        return spawned;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        List<Transform> toRemove = new List<Transform>();
        foreach (Transform child in parent)
        {
            toRemove.Add(child);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            Transform child = toRemove[i];
            if (child == null) continue;
            Destroy(child.gameObject);
        }
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

    private Transform GetTemplateForFace(string faceName, out int templateIndex)
    {
        if (string.Equals(faceName, "front", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesFrontRandom, platformTemplateFront, out templateIndex);
        if (string.Equals(faceName, "back", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesBackRandom, platformTemplateBack, out templateIndex);
        if (string.Equals(faceName, "left", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesLeftRandom, platformTemplateLeft, out templateIndex);
        if (string.Equals(faceName, "right", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesRightRandom, platformTemplateRight, out templateIndex);
        if (string.Equals(faceName, "up", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesUpRandom, platformTemplateUp, out templateIndex);
        if (string.Equals(faceName, "down", System.StringComparison.OrdinalIgnoreCase)) return GetRandomTemplate(platformTemplatesDownRandom, platformTemplateDown, out templateIndex);
        templateIndex = 0;
        return null;
    }

    // --- ADDED ---
    private Transform GetRandomTemplate(Transform[] pool, Transform fallback, out int templateIndex)
    {
        if (pool != null && pool.Length > 0)
        {
            templateIndex = Random.Range(0, pool.Length);
            return pool[templateIndex];
        }

        templateIndex = 0;
        return fallback;
    }

    // --- ADDED ---
    private readonly Dictionary<Vector3Int, CubePiece> visiblePiecesByGrid = new Dictionary<Vector3Int, CubePiece>();

    private void CacheVisiblePieces()
    {
        visiblePiecesByGrid.Clear();
        if (visibleCubeVisual == null) return;

        Transform root = visibleCubeVisual.PiecesRoot;
        if (root == null) return;

        CubePiece[] pieces = root.GetComponentsInChildren<CubePiece>(true);
        foreach (CubePiece p in pieces)
        {
            if (!IsValidVisiblePiece(p)) continue;
            visiblePiecesByGrid[p.GridPosition] = p;
        }
    }

    private CubeVisual FindVisibleCubeVisual()
    {
        CubeVisual[] visuals = FindObjectsByType<CubeVisual>(FindObjectsInactive.Include);
        for (int i = 0; i < visuals.Length; i++)
        {
            CubeVisual v = visuals[i];
            if (v == null) continue;
            if (v.PiecesRoot != null && v.PiecesRoot != cubeRoot)
                return v;
        }
        return visuals.Length > 0 ? visuals[0] : null;
    }

    private void ApplyMaterialFromVisibleFaceName(Transform faceTransform, GameObject templateRoot)
    {
        if (!matchFaceMaterialByName)
        {
            if (showMaterialMatchDebug) Debug.Log("[FaceChildrenRemover] Match skipped: matchFaceMaterialByName is false");
            return;
        }
        if (visibleCubeVisual == null || faceTransform == null || templateRoot == null)
        {
            if (showMaterialMatchDebug) Debug.Log("[FaceChildrenRemover] Match skipped: missing references (visibleCubeVisual/faceTransform/templateRoot)");
            return;
        }

        CubePiece owner = faceTransform.GetComponentInParent<CubePiece>();
        if (owner == null)
        {
            if (showMaterialMatchDebug) Debug.Log("[FaceChildrenRemover] Match skipped: faceTransform has no CubePiece parent");
            return;
        }

        if (!TryGetValidVisiblePiece(owner.GridPosition, out CubePiece visiblePiece))
        {
            if (showMaterialMatchDebug) Debug.Log($"[FaceChildrenRemover] Match skipped: no visible piece for grid {owner.GridPosition}");
            return;
        }

        string faceChildName = faceTransform.name;
        if (showMaterialMatchDebug)
        {
            Debug.Log($"[FaceChildrenRemover] Comparing requested face '{faceChildName}' on visible piece '{visiblePiece.name}'. Candidates: {GetVisibleFaceCandidatesDebug(visiblePiece.transform, faceChildName)}");
        }
        Transform faceChild = FindBestVisibleFaceChild(visiblePiece.transform, faceChildName);
        if (faceChild == null)
        {
            if (showMaterialMatchDebug) Debug.Log($"[FaceChildrenRemover] Match skipped: visible face child '{faceChildName}' not found");
            return;
        }

        Renderer faceRenderer = faceChild.GetComponent<Renderer>();
        if (faceRenderer == null)
        {
            if (showMaterialMatchDebug) Debug.Log("[FaceChildrenRemover] Match skipped: visible face renderer missing");
            return;
        }

        Material srcMat = faceRenderer.sharedMaterial != null ? faceRenderer.sharedMaterial : faceRenderer.material;
        if (srcMat == null)
        {
            if (showMaterialMatchDebug) Debug.Log("[FaceChildrenRemover] Match skipped: visible face material is null");
            return;
        }

        string rawMatName = CleanMaterialName(srcMat.name);
        string colorName = ExtractColorName(rawMatName);
        string templateName = CleanMaterialName(templateRoot.name);
        if (showMaterialMatchDebug && !IsKnownColor(colorName))
        {
            Debug.Log($"[FaceChildrenRemover] Warning: color '{colorName}' not recognized (raw='{rawMatName}')");
        }

        Material matched = FindTemplateColorMaterial(templateName, colorName);
        if (matched == null && useMaterialLibraryFallback)
        {
            matched = FindMaterialByName(colorName);
        }
        if (matched == null && useMaterialLibraryFallback)
        {
            // Fallback: try the face material name directly.
            matched = FindMaterialByName(CleanMaterialName(srcMat.name));
        }
        if (showMaterialMatchDebug)
        {
            string matchedName = matched != null ? CleanMaterialName(matched.name) : "NULL";
            Debug.Log($"[FaceChildrenRemover] Match template='{templateName}' requestedFace='{faceChildName}' resolvedFace='{faceChild.name}' color='{colorName}' (raw='{rawMatName}') -> material='{matchedName}'");
        }
        if (matched == null)
        {
            if (showMaterialMatchDebug) Debug.Log("[FaceChildrenRemover] Match failed: no material matched");
            return;
        }

        Renderer ren = templateRoot.GetComponent<Renderer>();
        if (ren == null)
        {
            if (showMaterialMatchDebug) Debug.Log($"[FaceChildrenRemover] Match skipped: template root '{templateRoot.name}' has no Renderer");
            return;
        }
        Material[] mats = ren.sharedMaterials;
        if (mats == null || mats.Length == 0)
        {
            ren.sharedMaterial = matched;
            return;
        }
        for (int k = 0; k < mats.Length; k++) mats[k] = matched;
        ren.sharedMaterials = mats;
    }

    private Material FindMaterialByName(string matName)
    {
        if (materialLibrary == null || materialLibrary.Length == 0) return null;
        for (int i = 0; i < materialLibrary.Length; i++)
        {
            Material m = materialLibrary[i];
            if (m == null) continue;
            if (string.Equals(CleanMaterialName(m.name), matName, System.StringComparison.OrdinalIgnoreCase))
                return m;
        }
        return null;
    }

    private Material FindTemplateColorMaterial(string templateName, string colorName)
    {
        if (templateColorMaterials == null || templateColorMaterials.Length == 0) return null;
        for (int i = 0; i < templateColorMaterials.Length; i++)
        {
            TemplateColorSet m = templateColorMaterials[i];
            if (m == null) continue;
            if (!string.Equals(CleanMaterialName(m.templateName), templateName, System.StringComparison.OrdinalIgnoreCase)) continue;
            return GetMaterialForColor(m, colorName);
        }
        return null;
    }

    private static Material GetMaterialForColor(TemplateColorSet set, string colorName)
    {
        if (set == null || string.IsNullOrWhiteSpace(colorName)) return null;
        string c = colorName.Trim().ToLowerInvariant();
        return c switch
        {
            "white" => set.white,
            "yellow" => set.yellow,
            "pink" => set.pink,
            "blue" => set.blue,
            "green" => set.green,
            "orange" => set.orange,
            _ => null
        };
    }

    [ContextMenu("Regenerate Template Color Sets")]
    private void RegenerateTemplateColorSets()
    {
        RegenerateTemplateColorSets(true);
    }

    private void RegenerateTemplateColorSets(bool force)
    {
        List<string> names = CollectTemplateNames();
        if (names.Count == 0) return;

        Dictionary<string, TemplateColorSet> existing = new Dictionary<string, TemplateColorSet>();
        if (templateColorMaterials != null)
        {
            for (int i = 0; i < templateColorMaterials.Length; i++)
            {
                TemplateColorSet set = templateColorMaterials[i];
                if (set == null || string.IsNullOrWhiteSpace(set.templateName)) continue;
                string key = CleanMaterialName(set.templateName);
                if (!existing.ContainsKey(key)) existing.Add(key, set);
            }
        }

        List<TemplateColorSet> rebuilt = new List<TemplateColorSet>(names.Count);
        for (int i = 0; i < names.Count; i++)
        {
            string name = names[i];
            if (existing.TryGetValue(CleanMaterialName(name), out TemplateColorSet set))
            {
                rebuilt.Add(set);
            }
            else
            {
                TemplateColorSet fresh = new TemplateColorSet { templateName = name };
                rebuilt.Add(fresh);
            }
        }

        if (force || templateColorMaterials == null || templateColorMaterials.Length != rebuilt.Count)
        {
            templateColorMaterials = rebuilt.ToArray();
        }
    }

    private List<string> CollectTemplateNames()
    {
        HashSet<string> names = new HashSet<string>();
        AddTemplateNames(names, platformTemplatesFrontRandom, platformTemplateFront);
        AddTemplateNames(names, platformTemplatesBackRandom, platformTemplateBack);
        AddTemplateNames(names, platformTemplatesLeftRandom, platformTemplateLeft);
        AddTemplateNames(names, platformTemplatesRightRandom, platformTemplateRight);
        AddTemplateNames(names, platformTemplatesUpRandom, platformTemplateUp);
        AddTemplateNames(names, platformTemplatesDownRandom, platformTemplateDown);

        List<string> list = new List<string>(names);
        list.Sort();
        return list;
    }

    private static void AddTemplateNames(HashSet<string> names, Transform[] pool, Transform fallback)
    {
        if (pool != null)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                Transform t = pool[i];
                if (t != null) names.Add(t.name);
            }
        }
        if (fallback != null) names.Add(fallback.name);
    }

    private bool TryGetValidVisiblePiece(Vector3Int gridPosition, out CubePiece visiblePiece)
    {
        visiblePiece = null;

        if (visiblePiecesByGrid.TryGetValue(gridPosition, out CubePiece cached) && IsValidVisiblePiece(cached))
        {
            visiblePiece = cached;
            return true;
        }

        CacheVisiblePieces();
        if (visiblePiecesByGrid.TryGetValue(gridPosition, out cached) && IsValidVisiblePiece(cached))
        {
            visiblePiece = cached;
            return true;
        }

        return false;
    }

    private bool IsValidVisiblePiece(CubePiece piece)
    {
        if (piece == null) return false;

        Transform t = piece.transform;
        if (t == null) return false;
        if (string.Equals(t.name, "LayerPivot", System.StringComparison.OrdinalIgnoreCase)) return false;

        return HasValidFaceChildren(t);
    }

    private bool HasValidFaceChildren(Transform pieceTransform)
    {
        if (pieceTransform == null) return false;

        foreach (Transform child in pieceTransform)
        {
            if (child != null && IsFaceTransform(child.name))
            {
                return true;
            }
        }

        return false;
    }

    private static string CleanMaterialName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        string cleaned = name.Replace(" (Instance)", "").Trim();
        while (cleaned.Contains("  ")) cleaned = cleaned.Replace("  ", " ");
        return cleaned;
    }

    private static string ExtractColorName(string materialName)
    {
        if (string.IsNullOrWhiteSpace(materialName)) return "";
        string normalized = materialName.ToLowerInvariant()
            .Replace("-", " ")
            .Replace("_", " ");
        while (normalized.Contains("  ")) normalized = normalized.Replace("  ", " ");
        string[] parts = normalized.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return normalized;

        for (int i = parts.Length - 1; i >= 0; i--)
        {
            string p = NormalizeColorName(parts[i]);
            if (IsKnownColor(p)) return p;
        }

        // Some materials include prefixes/suffixes around the color token.
        if (normalized.Contains("white")) return "white";
        if (normalized.Contains("yellow")) return "yellow";
        if (normalized.Contains("pink")) return "pink";
        if (normalized.Contains("blue")) return "blue";
        if (normalized.Contains("green")) return "green";
        if (normalized.Contains("orange")) return "orange";
        if (normalized.Contains("red")) return "pink";

        // Unknown names like "New Material 2" should not produce a fake color token.
        return "";
    }

    private static bool IsKnownColor(string value)
    {
        return value == "white"
            || value == "yellow"
            || value == "pink"
            || value == "blue"
            || value == "green"
            || value == "orange";
    }

    private Transform FindBestVisibleFaceChild(Transform visiblePiece, string requestedFaceName)
    {
        if (visiblePiece == null || string.IsNullOrWhiteSpace(requestedFaceName)) return null;

        Vector3 requestedLocal = FaceNameToLocalNormal(requestedFaceName);
        if (requestedLocal == Vector3.zero)
        {
            return visiblePiece.Find(requestedFaceName);
        }

        Vector3 targetWorld = cubeRoot != null
            ? cubeRoot.TransformDirection(requestedLocal).normalized
            : requestedLocal.normalized;

        float bestDot = -999f;
        Transform best = null;

        foreach (Transform child in visiblePiece)
        {
            if (child == null) continue;
            if (!IsFaceTransform(child.name)) continue;

            Vector3 childLocal = FaceNameToLocalNormal(child.name);
            if (childLocal == Vector3.zero) continue;

            Vector3 childWorld = visiblePiece.TransformDirection(childLocal).normalized;
            float dot = Vector3.Dot(childWorld, targetWorld);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = child;
            }
        }

        if (best != null) return best;
        return visiblePiece.Find(requestedFaceName);
    }

    private string GetVisibleFaceCandidatesDebug(Transform visiblePiece, string requestedFaceName)
    {
        if (visiblePiece == null || string.IsNullOrWhiteSpace(requestedFaceName)) return "none";

        Vector3 requestedLocal = FaceNameToLocalNormal(requestedFaceName);
        Vector3 targetWorld = cubeRoot != null
            ? cubeRoot.TransformDirection(requestedLocal).normalized
            : requestedLocal.normalized;

        List<string> candidates = new List<string>();
        foreach (Transform child in visiblePiece)
        {
            if (child == null || !IsFaceTransform(child.name)) continue;

            Vector3 childLocal = FaceNameToLocalNormal(child.name);
            float dot = -999f;
            if (childLocal != Vector3.zero && requestedLocal != Vector3.zero)
            {
                Vector3 childWorld = visiblePiece.TransformDirection(childLocal).normalized;
                dot = Vector3.Dot(childWorld, targetWorld);
            }

            candidates.Add($"{child.name} (dot={dot:0.000})");
        }

        if (candidates.Count == 0) return "none";
        return string.Join(", ", candidates);
    }

    private static Vector3 FaceNameToLocalNormal(string faceName)
    {
        if (string.Equals(faceName, "front", System.StringComparison.OrdinalIgnoreCase)) return Vector3.forward;
        if (string.Equals(faceName, "back", System.StringComparison.OrdinalIgnoreCase)) return Vector3.back;
        if (string.Equals(faceName, "left", System.StringComparison.OrdinalIgnoreCase)) return Vector3.left;
        if (string.Equals(faceName, "right", System.StringComparison.OrdinalIgnoreCase)) return Vector3.right;
        if (string.Equals(faceName, "up", System.StringComparison.OrdinalIgnoreCase)) return Vector3.up;
        if (string.Equals(faceName, "down", System.StringComparison.OrdinalIgnoreCase)) return Vector3.down;
        return Vector3.zero;
    }

    private static string NormalizeColorName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string v = value.Trim().ToLowerInvariant();
        return v switch
        {
            "red" => "pink",
            _ => v
        };
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

    private static bool IsConcernedExteriorFace(CubePiece owner, string faceName)
    {
        if (owner == null || string.IsNullOrWhiteSpace(faceName)) return false;

        Vector3Int p = owner.GridPosition;
        if (string.Equals(faceName, "front", System.StringComparison.OrdinalIgnoreCase)) return p.z == 1;
        if (string.Equals(faceName, "back", System.StringComparison.OrdinalIgnoreCase)) return p.z == 0;
        if (string.Equals(faceName, "right", System.StringComparison.OrdinalIgnoreCase)) return p.x == 1;
        if (string.Equals(faceName, "left", System.StringComparison.OrdinalIgnoreCase)) return p.x == 0;
        if (string.Equals(faceName, "up", System.StringComparison.OrdinalIgnoreCase)) return p.y == 1;
        if (string.Equals(faceName, "down", System.StringComparison.OrdinalIgnoreCase)) return p.y == 0;
        return false;
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
