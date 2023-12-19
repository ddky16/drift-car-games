using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform carPlayerTransform;
    [SerializeField] private Rigidbody carPlayerRigidbody;

    [SerializeField] private Vector3 camOffset;
    [SerializeField] private float camSpeed;

    private void FixedUpdate()
    {
        Vector3 playerForward = (carPlayerRigidbody.velocity + carPlayerTransform.transform.forward).normalized;
        transform.position = Vector3.Lerp(
            transform.position,
            carPlayerTransform.position + carPlayerTransform.transform.TransformVector(camOffset) + playerForward * (-5f),
                camSpeed * Time.fixedDeltaTime
            );
        transform.LookAt(carPlayerTransform);
    }
}
