using UnityEngine;

public class FrontFaceSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;    // Drag your enemy prefab here
    private GameObject spawnedEnemy;  // Tracks the monster
    
    [Header("Settings")]
    [Range(0, 1)] public float sensitivity = 0.7f; // 0.7 means it spawns when mostly facing you

    void Start()
    {
        // Hide the marker cube when the game starts
        if (GetComponent<MeshRenderer>()) GetComponent<MeshRenderer>().enabled = false;
    }

    void Update()
    {
        // 1. Calculate if this face is looking at the camera
        // transform.forward is the Blue Arrow of the spawn point
        float dot = Vector3.Dot(transform.forward, -Camera.main.transform.forward);

        // 2. IS IT THE FRONT FACE?
        if (dot > sensitivity)
        {
            // Spawn if there isn't one already
            if (spawnedEnemy == null)
            {
                spawnedEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
                // Make it a child so it moves with the cube piece!
                spawnedEnemy.transform.SetParent(this.transform);
            }
        }
        else
        {
            // 3. NOT THE FRONT FACE? Kill it.
            if (spawnedEnemy != null)
            {
                Destroy(spawnedEnemy);
            }
        }
    }
}