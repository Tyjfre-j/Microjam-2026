using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColumnRotator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform bigCubeCenter;
    [SerializeField] private float rotationDuration = 0.35f;
    [SerializeField] private bool alignPlatformsAfterRotation = true;
    [SerializeField] private bool snapFacesAfterRotation = true;

    private bool isRotating;

    public Coroutine RotateColumn(List<GameObject> cubes, Vector3 axis, float angle)
    {
        if (isRotating) return null;
        return StartCoroutine(RotateColumnRoutine(cubes, axis, angle));
    }

    private IEnumerator RotateColumnRoutine(List<GameObject> cubes, Vector3 axis, float angle)
    {
        if (cubes == null || cubes.Count == 0) yield break;
        if (bigCubeCenter == null) bigCubeCenter = transform;

        isRotating = true;
        axis = axis.normalized;

        GameObject pivot = new GameObject("ColumnPivot");
        Transform pivotTransform = pivot.transform;
        pivotTransform.position = bigCubeCenter.position;
        pivotTransform.rotation = Quaternion.identity;

        Dictionary<Transform, Transform> originalParents = new Dictionary<Transform, Transform>(cubes.Count);
        List<Transform> faceMeshes = new List<Transform>(cubes.Count * 2);
        Dictionary<Transform, Quaternion> faceMeshWorldRotations = new Dictionary<Transform, Quaternion>(cubes.Count * 2);
        List<Transform> platformParents = new List<Transform>(cubes.Count * 2);

        foreach (GameObject cube in cubes)
        {
            if (cube == null) continue;
            Transform cubeTransform = cube.transform;
            originalParents[cubeTransform] = cubeTransform.parent;
            cubeTransform.SetParent(pivotTransform, true);
            CollectTargets(cubeTransform, faceMeshes, faceMeshWorldRotations, platformParents);
        }

        Debug.Log($"[ColumnRotator] Collected {faceMeshes.Count} face meshes and {platformParents.Count} platform parents.");

        float duration = Mathf.Max(0.01f, rotationDuration);
        Quaternion startRotation = pivotTransform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float step = angle * (Time.deltaTime / duration);
            pivotTransform.Rotate(axis, step, Space.World);

            elapsed += Time.deltaTime;
            yield return null;
        }

        pivotTransform.rotation = startRotation * Quaternion.AngleAxis(angle, axis);

        foreach (KeyValuePair<Transform, Transform> kvp in originalParents)
        {
            if (kvp.Key == null) continue;
            kvp.Key.SetParent(kvp.Value, true);
        }

        if (snapFacesAfterRotation)
        {
            for (int i = 0; i < faceMeshes.Count; i++)
            {
                Transform face = faceMeshes[i];
                if (face == null) continue;
                if (faceMeshWorldRotations.TryGetValue(face, out Quaternion worldRot))
                {
                    face.rotation = worldRot;
                }
            }
            Debug.Log("[ColumnRotator] Snapped face meshes to stored world rotations.");
        }

        if (alignPlatformsAfterRotation)
        {
            for (int i = 0; i < platformParents.Count; i++)
            {
                Transform platformParent = platformParents[i];
                if (platformParent == null) continue;
                FixPlatformAlignment(platformParent);
            }
            Debug.Log("[ColumnRotator] Aligned platform parents after rotation.");
        }

        Destroy(pivot);
        isRotating = false;
    }

    private void CollectTargets(
        Transform root,
        List<Transform> faceMeshes,
        Dictionary<Transform, Quaternion> faceMeshWorldRotations,
        List<Transform> platformParents)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t == null) continue;
            if (IsFaceMeshName(t.name))
            {
                if (!faceMeshWorldRotations.ContainsKey(t))
                {
                    faceMeshes.Add(t);
                    faceMeshWorldRotations[t] = t.rotation;
                }
                continue;
            }

            if (IsPlatformParent(t))
            {
                if (!platformParents.Contains(t))
                {
                    platformParents.Add(t);
                }
                continue;
            }
        }
    }

    private bool IsFaceMeshName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        string compact = name.Replace(" ", "").ToUpperInvariant();
        if (compact == "FACEMESH") return true;
        if (compact == "FRONT") return true;
        if (compact == "BACK") return true;
        if (compact == "UP") return true;
        if (compact == "DOWN") return true;
        if (compact == "LEFT") return true;
        if (compact == "RIGHT") return true;
        return false;
    }

    private bool IsPlatformParent(Transform t)
    {
        if (t == null) return false;
        string name = t.name.ToUpperInvariant();
        return name.Contains("PL") && name.Contains("PARENT");
    }

    private void FixPlatformAlignment(Transform plParent)
    {
        Vector3 faceNormal = (plParent.position - bigCubeCenter.position).normalized;

        if (Mathf.Abs(faceNormal.x) > Mathf.Abs(faceNormal.y) && Mathf.Abs(faceNormal.x) > Mathf.Abs(faceNormal.z))
            faceNormal = new Vector3(Mathf.Sign(faceNormal.x), 0f, 0f);
        else if (Mathf.Abs(faceNormal.y) > Mathf.Abs(faceNormal.x) && Mathf.Abs(faceNormal.y) > Mathf.Abs(faceNormal.z))
            faceNormal = new Vector3(0f, Mathf.Sign(faceNormal.y), 0f);
        else
            faceNormal = new Vector3(0f, 0f, Mathf.Sign(faceNormal.z));

        Vector3 worldHorizontal;
        if (Mathf.Abs(faceNormal.y) > 0.9f)
        {
            worldHorizontal = Vector3.forward;
        }
        else
        {
            worldHorizontal = Vector3.Cross(Vector3.up, faceNormal).normalized;
        }

        plParent.rotation = Quaternion.LookRotation(worldHorizontal, faceNormal);

        foreach (Transform child in plParent)
        {
            child.localRotation = Quaternion.identity;
        }
    }
}
