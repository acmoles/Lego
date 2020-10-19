using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[RequireComponent(typeof(Shape))]
public class LegoBrick : MonoBehaviour
{
    #region init
    protected Shape _shape;
    protected Transform _shapeMesh;
    Bounds meshBounds;
    const float importScaleFactor = 100f;

    public bool visualize = true;

    List<Vector3> slots = new List<Vector3>();
    List<Vector3> points = new List<Vector3>();

    const float halfSize = 1.139775f; // Distance between points?
    const float height = 0.45f;
    #endregion

    #region interaction
    static LegoBrick lastLegoBrick;

    Color _color;
    Transform ghost;
    bool grasped = false;
    public float maxConnectionRange = 0.3F;

    LegoBrick connectedTo;
    FixedJoint connectionJoint;

    List<LegoBrick> connectedToMe = new List<LegoBrick>();

    LegoBrick connectedToPoint;
    Vector3 connecedToLocalPoint;
    Vector3 myLocalSlot;
    #endregion

    public void Init()
    {
        _shape = GetComponent<Shape>();
        meshBounds = _shape.meshRenderer.bounds;
        _shapeMesh = _shape.meshTransform;

        // Compensate for scale
        float compX = (importScaleFactor * meshBounds.extents.x / _shapeMesh.localScale.x) / halfSize;
        float compZ = (importScaleFactor * meshBounds.extents.z / _shapeMesh.localScale.z) / halfSize;

        int xCount = (int)Math.Round(compX);
        int zCount = (int)Math.Round(compZ);
        //Debug.Log("init lego brick: " + xCount + ", " + zCount);

        float scaledHalfSize = (halfSize * _shapeMesh.localScale.x) / importScaleFactor;
        float scaledHeight = (height * _shapeMesh.localScale.y) / importScaleFactor;

        for (int x = 0; x < xCount; x++)
        {
            for (int z = 0; z < zCount; z++)
            {
                var slot = meshBounds.min + new Vector3(scaledHalfSize + x * scaledHalfSize * 2, 0, scaledHalfSize + z * scaledHalfSize * 2);
                slots.Add(slot);

                var point = slot;
                point.y = meshBounds.max.y;
                point.y -= scaledHeight; // Height of a lego brick
                points.Add(point);

                if (visualize)
                {
                    VisualizePosition.Create(gameObject, point, 0.01f);
                    VisualizePosition.Create(gameObject, slot, 0.005f);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(meshBounds.center, meshBounds.size);
        Gizmos.DrawWireSphere(meshBounds.center, 0.01f);
    }

    public void Update()
    {
        if (grasped && lastLegoBrick != null && lastLegoBrick != this)
        {
            PositionGhost(lastLegoBrick, this.transform.position);
        }
    }

    public void onHoverBegin ()
    {
        Debug.Log("Same instance? " + (lastLegoBrick == this));
        if (lastLegoBrick != this)
        {
            lastLegoBrick = this;
        }
    }

    public void onGraspBegin ()
    {
        _shape.SetColor(Color.blue);
        grasped = true;
        MakeGhost();
        // Start positioning ghost
    }

    public void onGraspEnd ()
    {
        grasped = false;
        _shape.ResetColor();
        DestroyGhost();
        // Stop positioning ghost, position actual
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

    public void PositionGhost(LegoBrick other, Vector3 targetPosition)
    {
        ghost.transform.rotation = other.transform.rotation;
        connectedToPoint = other;
        connecedToLocalPoint = other.FindClosestPoint(other.transform.InverseTransformPoint(targetPosition));
        Vector3 worldPoint = other.transform.TransformPoint(connecedToLocalPoint);
        myLocalSlot = this.FindClosestSlot(this.transform.InverseTransformPoint(worldPoint));
        SetPositionMySlotPosition();
    }

    public void MakeGhost()
    {
        ghost = Instantiate(_shapeMesh);
    }

    public void DestroyGhost()
    {
        Destroy(ghost.gameObject);
    }

    public void SetPositionMySlotPosition()
    {
        Vector3 worldPoint = connectedToPoint.transform.TransformPoint(connecedToLocalPoint);
        Vector3 worldSlot = ghost.transform.TransformPoint(myLocalSlot);
        ghost.transform.position += worldPoint - worldSlot;
        ghost.transform.rotation = connectedToPoint.transform.rotation;
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
