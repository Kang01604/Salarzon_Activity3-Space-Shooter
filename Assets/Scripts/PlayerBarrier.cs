// PlayerBarrier.cs
using UnityEngine;

public class PlayerBarrier : MonoBehaviour
{
    [Header("X‑Axis Barrier")]
    public float minX = -12f, maxX = 12f;

    void LateUpdate()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        transform.position = p;
    }
}
