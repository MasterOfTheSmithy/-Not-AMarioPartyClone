using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;        // The player to follow
    public Vector3 offset = new Vector3(0, 10, -10);  // Camera offset from player
    public float smoothSpeed = 5f;  // How quickly the camera moves to follow

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;

        // Optional: always look at the player
        transform.LookAt(target);
    }
}