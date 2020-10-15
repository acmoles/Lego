using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class LegoBrick : Shape
{


    List<Vector3> slots = new List<Vector3>();
    List<Vector3> points = new List<Vector3>();

    const float halfSize = 1.139775f; // Distance between points?
    const float height = 0.45f;

    Bounds meshBounds;

    public LegoBrick attachedTo;
    LegoBrick connectedTo;

    List<LegoBrick> connectedToMe = new List<LegoBrick>();
    FixedJoint connectionJoint;
    LegoBrick connectedToPoint;
    Vector3 connecedToLocalPoint;
    Vector3 myLocalSlot;



    public void Awake()
    {
        meshBounds = meshRenderer.bounds;

        int xCount = (int)Math.Round(meshBounds.extents.x / halfSize);
        int zCount = (int)Math.Round(meshBounds.extents.z / halfSize);

        for (int x = 0; x < xCount; x++)
        {
            for (int z = 0; z < zCount; z++)
            {
                var slot = meshBounds.min + new Vector3(halfSize + x * halfSize * 2, 0, halfSize + z * halfSize * 2);
                slots.Add(slot);

                var point = slot;
                point.y = meshBounds.max.y;
                point.y -= height; // Height of a lego brick
                points.Add(point);

                VisualizePosition.Create(gameObject, point, 1f);
                VisualizePosition.Create(gameObject, slot, 0.5f);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(meshBounds.center, meshBounds.size);
        Gizmos.DrawWireSphere(meshBounds.center, 0.3f);
    }

    public Vector3 FindClosestSlot(Vector3 localPosition)
    {
        var closestPosition = slots[0];
        var closestDistance = Vector3.Distance(closestPosition, localPosition);
        foreach (var s in slots)
        {
            var d = Vector3.Distance(s, localPosition);
            if (d < closestDistance)
            {
                closestDistance = d;
                closestPosition = s;
            }
        }
        return closestPosition;
    }

    public Vector3 FindClosestPoint(Vector3 localPosition)
    {
        var closestPosition = points[0];
        var closestDistance = Vector3.Distance(closestPosition, localPosition);
        foreach (var s in points)
        {
            var d = Vector3.Distance(s, localPosition);
            if (d < closestDistance)
            {
                closestDistance = d;
                closestPosition = s;
            }
        }
        return closestPosition;
    }

    public void VizualizeConnectionTo(LegoBrick other, Vector3 targetPosition)
    {
        this.transform.rotation = other.transform.rotation;
        connectedToPoint = other;
        connecedToLocalPoint = other.FindClosestPoint(other.transform.InverseTransformPoint(targetPosition));
        Vector3 worldPoint = other.transform.TransformPoint(connecedToLocalPoint);
        myLocalSlot = this.FindClosestSlot(this.transform.InverseTransformPoint(worldPoint));
        SetPositionMySlotPosition();
    }

    public void SetPositionMySlotPosition()
    {
        Vector3 worldPoint = connectedToPoint.transform.TransformPoint(connecedToLocalPoint);
        Vector3 worldSlot = this.transform.TransformPoint(myLocalSlot);
        transform.position += worldPoint - worldSlot;
        this.transform.rotation = connectedToPoint.transform.rotation;
    }

    public void ConnectTo(LegoBrick other)
    {
        var p = transform.position;

        Disconnect();

        connectedTo = other;

        GetComponent<BoxCollider>().enabled = true;
        Physics.IgnoreCollision(this.GetComponent<Collider>(), connectedTo.GetComponent<Collider>(), true);

        transform.position = p;

        connectionJoint = gameObject.AddComponent<FixedJoint>();
        connectionJoint.connectedBody = connectedTo.GetComponent<Rigidbody>();

        transform.position = p;

        connectedTo.connectedToMe.Add(this);

        PositionWarped();
    }

    public void PositionWarped()
    {
        foreach (var lp in connectedToMe)
        {
            lp.SetPositionMySlotPosition();
            lp.PositionWarped();
        }
    }

    public bool IsConnected()
    {
        return connectedTo != null;
    }

    public void Disconnect()
    {
        if (!IsConnected()) return;

        Destroy(connectionJoint);

        Physics.IgnoreCollision(GetComponent<Collider>(), connectedTo.GetComponent<Collider>(), false);

        connectedTo.connectedToMe.Remove(this);
    }
}
