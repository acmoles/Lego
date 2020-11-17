using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TurntablePhysical : MonoBehaviour
{
    Rigidbody rb;
    public bool lockX = false;
    public bool lockY = false;
    public bool lockZ = false;

    private bool _physicsOccurred;
    Vector3 initialLocalTransform;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialLocalTransform = transform.localPosition;
    }

    private void FixedUpdate()
    {
        if (!_physicsOccurred)
        {
            _physicsOccurred = true;
        }
    }

    private void Update()
    {
        // Apply physical corrections only if PhysX has modified our positions.
        if (_physicsOccurred)
        {
            _physicsOccurred = false;

            if (rb != null)
            {
                Vector3 lockedPosition = transform.localPosition;
                if (lockX)
                    lockedPosition.x = initialLocalTransform.x;
                if (lockY)
                    lockedPosition.y = initialLocalTransform.y;
                if (lockZ)
                    lockedPosition.z = initialLocalTransform.z;
                transform.localPosition = lockedPosition;

                //Vector3 currentRotation = transform.localRotation.eulerAngles;
                //currentRotation.x = 0.0f;
                //currentRotation.z = 0.0f;
                //transform.localRotation = Quaternion.Euler(currentRotation);
            }
        }
    }

}
