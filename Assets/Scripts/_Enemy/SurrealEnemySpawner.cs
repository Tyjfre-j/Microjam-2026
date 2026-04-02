using UnityEngine;

public class SurrealEnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;    // The monster to spawn
    private GameObject spawnedEnemy;  // Track the current monster
    private bool hasSpawnedThisTurn;  // Prevent infinite spawning while active

    [Header("Settings")]
    public float activeThreshold = 0.8f; // How "Frontal" the face must be

    void Update()
    {
        // 1. Calculate if this face is looking at the camera
        // transform.forward is the Blue Arrow of your spawn point
        // Camera.main.transform.forward is where the camera is looking
        float dot = Vector3.Dot(transform.forward, -Camera.main.transform.forward);

        // 2. IS ACTIVE (Facing Camera)
        if (dot > activeThreshold)
        {
            if (!hasSpawnedThisTurn)
            {
                Spawn();
            }
        }
        // 3. IS NOT ACTIVE (Facing side, back, top, or bottom)
        else
        {
            if (spawnedEnemy != null || hasSpawnedThisTurn)
            {
                Despawn();
            }
        }
    }

    void Spawn()
    {
        // Spawn the enemy
        spawnedEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);

        // Parent it to the spawn point so it moves/rotates WITH the cube piece
        spawnedEnemy.transform.SetParent(this.transform);

        hasSpawnedThisTurn = true;
        Debug.Log(gameObject.name + " spawned an enemy in this dimension.");
    }

    void Despawn()
    {
        // Delete the enemy
        if (spawnedEnemy != null)
        {
            Destroy(spawnedEnemy);
        }

        // Reset the spawn flag so it can spawn again next time it faces the camera
        hasSpawnedThisTurn = false;
        Debug.Log(gameObject.name + " vanished into the void.");
    }
}