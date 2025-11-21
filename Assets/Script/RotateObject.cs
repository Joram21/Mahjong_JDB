using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0, 0, -50); // Rotation speed along x, y, z axes

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}