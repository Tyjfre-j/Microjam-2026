using UnityEngine;
using System.Text;

public class HierarchyAuditor : MonoBehaviour
{
    [Header("Settings")]
    public GameObject cubeHolder;
    public bool logToConsole = true;

    [ContextMenu("Verify Cube Hierarchy")]
    public void VerifyHierarchy()
    {
        if (cubeHolder == null) cubeHolder = this.gameObject;

        StringBuilder report = new StringBuilder();
        report.AppendLine($"=== Hierarchy Audit for {cubeHolder.name} ===");

        // Level 1: Small Cubes
        foreach (Transform smallCube in cubeHolder.transform)
        {
            report.AppendLine($"\n[Small Cube] {smallCube.name} | Pos: {smallCube.localPosition}");

            // Level 2: Face Pivots
            foreach (Transform facePivot in smallCube)
            {
                report.AppendLine($"  └─ [Pivot] {facePivot.name} | Rot: {facePivot.localEulerAngles}");

                // Level 3: PL Parents
                foreach (Transform plParent in facePivot)
                {
                    report.AppendLine($"    └─ [PL Parent] {plParent.name} | Pos: {plParent.localPosition}");

                    // Level 4: Face Mech
                    foreach (Transform faceMech in plParent)
                    {
                        report.AppendLine($"      └─ [Face Mech] {faceMech.name}");
                        CheckMechAlignment(faceMech, report);
                    }
                }
            }
        }

        if (logToConsole) Debug.Log(report.ToString());
    }

    private void CheckMechAlignment(Transform mech, StringBuilder report)
    {
        // Check if Z-axis points away from the center (Assuming center is 0,0,0)
        Vector3 worldDirection = mech.forward; 
        float dot = Vector3.Dot(worldDirection, mech.position.normalized);
        
        if (dot > 0.8f)
            report.AppendLine("         ✅ Z-Axis points OUT correctly.");
        else
            report.AppendLine("         ⚠️ WARNING: Z-Axis might not be pointing OUT.");
            
        report.AppendLine($"         Local Rot: {mech.localEulerAngles}");
    }
}