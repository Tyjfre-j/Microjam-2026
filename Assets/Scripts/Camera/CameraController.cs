using UnityEngine;

public class FinalCubeCamera : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 2, 10);
    public float positionSmooth = 15f;
    public float rotationSmooth = 10f;

    void LateUpdate()
    {
        if (player == null) return;

        // 1. On calcule la position désirée de manière plus robuste
        // On utilise la rotation du joueur pour définir la direction, 
        // mais on force la distance pour éviter le zoom.
        Vector3 targetPosition = player.position + (player.right * offset.x) + (player.up * offset.y) + (player.forward * offset.z);

        // 2. Déplacement fluide
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmooth * Time.deltaTime);

        // 3. Rotation fluide
        // On veut que la caméra regarde toujours le joueur, avec le "Up" du joueur
        Quaternion targetRotation = Quaternion.LookRotation(player.position - transform.position, player.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmooth * Time.deltaTime);
    }
}