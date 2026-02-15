using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 10f;     // Camera movement speed
    public float edgeSize = 10f;      // Distance from screen edge to trigger movement
    public bool useScreenPercentage = false; // Optional toggle

    void Update()
    {
        Vector3 move = Vector3.zero;

        float threshold = edgeSize;

        if (useScreenPercentage)
        {
            threshold = Screen.width * (edgeSize / 100f);
        }

        // Left
        if (Mouse.current.position.x.ReadValue() <= threshold)
        {
            move.x -= 1;
        }

        // Right
        if (Mouse.current.position.x.ReadValue() >= Screen.width - threshold)
        {
            move.x += 1;
        }

        // Bottom
        if (Mouse.current.position.y.ReadValue() <= threshold)
        {
            move.z -= 1;
        }

        // Top
        if (Mouse.current.position.y.ReadValue() >= Screen.height - threshold)
        {
            move.z += 1;
        }

        // Normalize to prevent faster diagonal movement
        if (move != Vector3.zero)
        {
            move.Normalize();
            transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}

