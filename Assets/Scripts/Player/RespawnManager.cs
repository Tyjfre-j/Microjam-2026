using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public Transform player;        // your player
    public float respawnHeight = 10f; // height to respawn above the fall point
    public float fallY = -10f;      // Y limit where player is considered fallen

    private Rigidbody rb;

    void Start()
    {
        if (player != null)
            rb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (player.position.y < fallY)
        {
            Respawn();
        }
    }

    void Respawn()
    {
        if (rb != null)
            rb.linearVelocity = Vector3.zero; // reset momentum

        Vector3 newPos = new Vector3(
            player.position.x,       // keep same X
            respawnHeight,           // set new Y
            player.position.z        // keep same Z
        );

        player.position = newPos;
    }
}