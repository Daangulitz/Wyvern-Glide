using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CenterOfMassAdjuster : MonoBehaviour
{
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0, -0.8f, 0);

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;
    }
}