using UnityEngine;

public class QuantumSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;    // Drag your enemy here
    private GameObject spawnedEnemy;  // Track the monster
    
    [Header("Settings")]
    public float threshold = 0.6f;    // 0.6 = spawns when mostly facing camera

    void Start()
    {
        // Hide the marker cube so we don't see the grey box
        if (GetComponent<MeshRenderer>()) GetComponent<MeshRenderer>().enabled = false;
    }

    void Update()
    {
        // 1. Get the direction the spawn point is facing (Blue Arrow)
        Vector3 forwardDir = transform.forward; 
        
        // 2. Get the direction of the Camera
        Vector3 camDir = -Camera.main.transform.forward;

        // 3. Dot Product: 1 = Facing perfectly, 0 = Sideways, -1 = Backwards
        float dot = Vector3.Dot(forwardDir, camDir);

        // 4. LOGIC: If facing camera, spawn. If not, kill.
        if (dot > threshold)
        {
            if (spawnedEnemy == null)
            {
                spawnedEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
                // Make the enemy a child so it moves with the Rubik's Cube piece!
                spawnedEnemy.transform.SetParent(this.transform);
            }
        }
        else
        {
            if (spawnedEnemy != null)
            {
                Destroy(spawnedEnemy);
            }
        }
    }
}