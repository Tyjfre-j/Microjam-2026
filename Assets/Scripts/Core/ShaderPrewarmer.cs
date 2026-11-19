using UnityEngine;

/// <summary>
/// Compiles/uploads every shader variant used by the scene during the first frame,
/// so platforms spawned after a slice turn never hitch or flash a placeholder.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class ShaderPrewarmer : MonoBehaviour
{
    [SerializeField] private ShaderVariantCollection variants;

    private void Awake()
    {
        // Only warm the scene's own variants (WarmupAllShaders also hits unrelated shaders like Terrain and is slow).
        if (variants != null && !variants.isWarmedUp) variants.WarmUp();
    }
}
