using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Leap;
using Leap.Unity.Interaction;

[RequireComponent(typeof(Shape))]
public class LegoBrick : MonoBehaviour
{
    private static HashSet<LegoBrick> _allLegoBricks;
    public static HashSet<LegoBrick> allLegoBricks
    {
        get
        {
            if (_allLegoBricks == null)
            {
                _allLegoBricks = new HashSet<LegoBrick>();
            }
            return _allLegoBricks;
        }
    }
    static LegoBrick lastLegoBrick;

    private class Position
    {
        Vector3 position { get; set; }

    }

    protected Shape _shape;
    protected Transform _shapeMesh;
    Bounds meshBounds;
    const float importScaleFactor = 100f;

    public bool visualize = true;
    public bool kinemetic = false;

    List<Vector3> slots = new List<Vector3>();
    List<Vector3> points = new List<Vector3>();

    const float halfSize = 1.139775f; // Distance between points?
    const float height = 0.45f;

    void Awake()
    {
        if (kinemetic) GetComponent<Rigidbody>().isKinematic = true;
        allLegoBricks.Add(this);
        interactionBehaviour.manager = Game.Instance.manager;
    }

    public void Init()
    {
        gameObject.GetComponent<Rigidbody>().mass = .1f;
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
                    Debug.Log("Visualize");
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


    public InteractionBehaviour interactionBehaviour;

    bool _grasped = false;
    public float maxGhostDistance = 0.1f;
    LegoBrick connectedToHover;

    LegoBrick connectedTo;
    FixedJoint connectionJoint;
    List<LegoBrick> connectedToMe = new List<LegoBrick>();

    bool _connecting = false;

    public void Update()
    {
        if (_grasped && lastLegoBrick != null && lastLegoBrick != this)
        {
            PositionGhost(lastLegoBrick, this.transform.position);
        }

        //UpdateConnectionTo();
    }

    public LegoBrick FindPreferredLegoBrick()
    {
        LegoBrick preferredLegoBrick = null;
        float closestDistSqrd = float.PositiveInfinity;
        foreach (var brick in allLegoBricks)
        {
            var brickCollider = brick.GetComponent<Collider>();
            Vector3 nearestPointOnCollider = brickCollider.ClosestPoint(this.transform.position);
            float testDistanceSqrd = (nearestPointOnCollider - this.transform.position).sqrMagnitude;
            if (testDistanceSqrd < closestDistSqrd && brick != this && brick != connectedTo && !connectedToMe.Contains(brick))
            {
                preferredLegoBrick = brick;
                closestDistSqrd = testDistanceSqrd;
            }
        }

        if (closestDistSqrd < maxGhostDistance * maxGhostDistance)
        {
            // Visualise selection
            MakeGhost();
            Debug.DrawLine(this.transform.position, preferredLegoBrick.transform.position, Color.red);
            return preferredLegoBrick;
        } else
        {
            DestroyGhost();
            return null;
        }
    }

    public void onGraspBegin()
    {
        _shape.SetColor(Color.magenta);
        _grasped = true;
        Disconnect();
        if (connectedToMe.Count > 0)
        {
            DisconnectPropagate();
        }
    }

    public void onGraspStay()
    {
        lastLegoBrick = FindPreferredLegoBrick();
    }

    public void onGraspEnd()
    {
        _grasped = false;
        _shape.ResetColor();

        // Start connection at Ghost
        if (_ghosting)
        {
            DestroyGhost();
            ConnectTo(connectedToHover);
            //_connecting = true;
        }
    }

    public void onHoverBegin()
    {
        //Debug.Log("Same instance? " + (lastLegoBrick == this));
        //if (lastLegoBrick != this)
        //{
        //    lastLegoBrick = this;
        //}
        //_shape.SetColor(Color.cyan);
    }

    public void onHoverEnd()
    {
        //_shape.ResetColor();
    }

    public Vector3 axis;

    public void PositionGhost(LegoBrick other, Vector3 targetPosition)
    {
        if (_ghosting == false)
        {
            return;
        }
        connectedToHover = other;

        // Position

        Vector3 otherPreferredLocalPoint = other.FindClosestPoint(other.transform.InverseTransformPoint(targetPosition));
        Vector3 worldPoint = other.transform.TransformPoint(otherPreferredLocalPoint);

        // Hmmm
        Vector3 ghostHoverPreferredLocalSlot = FindClosestSlot(ghost.transform.InverseTransformPoint(worldPoint));
        Vector3 worldSlot = ghost.transform.TransformPoint(ghostHoverPreferredLocalSlot);

        // For visualisation
        Vector3 myHoverPreferredLocalSlot = FindClosestSlot(transform.InverseTransformPoint(worldPoint));
        Vector3 myWorldSlot = transform.TransformPoint(myHoverPreferredLocalSlot);
        Debug.DrawLine(worldPoint, myWorldSlot, Color.yellow);

        // Rotation

        // Must always be xz plane
        Vector3 alignedForward = NearestLegoAxis(transform.forward);
        if (alignedForward.x == 0 && alignedForward.z == 0)
        {
            Debug.Log("! alignForward problem: " + alignedForward);
            alignedForward = new Vector3(1, 0, 0);
        }

        axis = alignedForward;

        // Must always be up/down (world or local)
        Vector3 alignedUp = Vector3.up;

        Quaternion worldRotation = Quaternion.LookRotation(alignedForward, alignedUp);
        Quaternion localRotation = Quaternion.LookRotation(other.axis, alignedUp);

        Debug.DrawRay(worldPoint, alignedUp, Color.cyan);
        Debug.DrawRay(worldPoint, alignedForward, Color.cyan);

        ghost.transform.position += worldPoint - worldSlot;
        //ghost.transform.rotation = worldRotation * other.transform.rotation;
        ghost.transform.rotation = other.transform.rotation * worldRotation * Quaternion.Inverse(localRotation);

        // ghost.transform.rotation = other.transform.rotation;
    }

    private static Vector3 NearestWorldAxis(Vector3 v)
    {
        if (Mathf.Abs(v.x) < Mathf.Abs(v.y))
        {
            v.x = 0;
            if (Mathf.Abs(v.y) < Mathf.Abs(v.z))
                v.y = 0;
            else
                v.z = 0;
        }
        else
        {
            v.y = 0;
            if (Mathf.Abs(v.x) < Mathf.Abs(v.z))
                v.x = 0;
            else
                v.z = 0;
        }

        return v;
    }

    private static Vector3 NearestLegoAxis(Vector3 v)
    {
        v.y = 0;
        if (Mathf.Abs(v.x) < Mathf.Abs(v.z))
            v.x = 0;
        else
            v.z = 0;

        return v;
    }

    Vector3 _ghostPosition;
    Quaternion _ghostRotation;
    float oldMass;

    public void ConnectTo(LegoBrick other)
    {
        if (other == null)
        {
            return;
        }


        Disconnect();

        connectedTo = other;

        this.transform.position = _ghostPosition;
        this.transform.rotation = _ghostRotation;

        // Stop collisions with connectedTo
        //Physics.IgnoreCollision(this.GetComponent<Collider>(), connectedTo.GetComponent<Collider>(), true);

        connectionJoint = gameObject.AddComponent<FixedJoint>();
        connectionJoint.enableCollision = false;
        connectionJoint.breakForce = Mathf.Infinity;
        connectionJoint.breakTorque = Mathf.Infinity;
        connectionJoint.connectedBody = connectedTo.GetComponent<Rigidbody>();
        var rb = gameObject.GetComponent<Rigidbody>();
        oldMass = rb.mass;
        rb.mass = oldMass / 10f;


        connectedTo.connectedToMe.Add(this);

        // PositionPropegate();
        Debug.Log("Connection!");
    }

    public void Disconnect()
    {
        if (!IsConnected()) return;

        // Re-allow collisions with connectedTo
        //Physics.IgnoreCollision(this.GetComponent<Collider>(), connectedTo.GetComponent<Collider>(), false);
        gameObject.GetComponent<Rigidbody>().mass = oldMass;

        Destroy(connectionJoint);

        connectedTo.connectedToMe.Remove(this);
        connectedTo = null;
    }

    public void OnJointBreak(float breakForce)
    {
        Disconnect();
        DisconnectPropagate();
    }


    [Range(0, 100F)]
    public float lerpSpeed = 20f;

    public void UpdateConnectionTo()
    {
        if (!_connecting)
        {
            return;
        }
        // Initialize position and rotation.
        Vector3 finalPosition;
        Quaternion finalRotation;
        if (interactionBehaviour != null)
        {
            finalPosition = interactionBehaviour.rigidbody.position;
            finalRotation = interactionBehaviour.rigidbody.rotation;
        }
        else
        {
            finalPosition = this.transform.position;
            finalRotation = this.transform.rotation;
        }

        Vector3 targetPosition = ghost.transform.position;
        Quaternion targetRotation = ghost.transform.rotation;
        finalPosition = Vector3.Lerp(finalPosition, targetPosition, lerpSpeed * Time.deltaTime);
        finalRotation = Quaternion.Slerp(finalRotation, targetRotation, lerpSpeed * 0.8f * Time.deltaTime);
        if (Vector3.Distance(finalPosition, targetPosition) < 0.001f && Quaternion.Angle(targetRotation, finalRotation) < 2F)
        {
            _connecting = false;
            DestroyGhost();
            ConnectTo(connectedToHover);
        }

        // Set final position.
        if (interactionBehaviour != null)
        {
            interactionBehaviour.rigidbody.position = finalPosition;
            this.transform.position = finalPosition;
            interactionBehaviour.rigidbody.rotation = finalRotation;
            this.transform.rotation = finalRotation;
        }
        else
        {
            this.transform.position = finalPosition;
            this.transform.rotation = finalRotation;
        }

        // Fade out ghost
        float alpha = ghostRenderer.material.GetFloat("Alpha");
        float finalAlpha = Mathf.Lerp(alpha, 0f, lerpSpeed * Time.deltaTime);
        ghostRenderer.material.SetFloat("Alpha", finalAlpha);
    }

    public void PositionPropagate()
    {
        foreach (var brick in connectedToMe)
        {
            // brick.SetPositionGhost();
            brick.PositionPropagate();
        }
    }

    public void DisconnectPropagate()
    {
        foreach (var brick in connectedToMe)
        {
            Disconnect();
            brick.DisconnectPropagate();
        }
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

    public bool CheckOccupiedPoint()
    {
        return false;
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

    bool _ghosting = false;
    Transform ghost;
    MeshRenderer ghostRenderer;
    public Material ghostMaterial;

    public void MakeGhost()
    {
        if (!_ghosting)
        {
            _ghosting = true;
            ghost = Instantiate(_shapeMesh);
            ghost.name = "Ghost";
            ghostRenderer = ghost.GetComponent<MeshRenderer>();
            ghostRenderer.material = ghostMaterial;
        }
    }

    public void DestroyGhost()
    {
        if (_ghosting && ghost != null)
        {
            _ghosting = false;
            _ghostPosition = ghost.transform.position;
            _ghostRotation = ghost.transform.rotation;
            Destroy(ghost.gameObject);
            ghost = null;
            ghostRenderer = null;
            Debug.Log("Destroy Ghost");
        }
    }

    public bool IsConnected()
    {
        return connectedTo != null;
    }

    public void EndVisualize()
    {
        gameObject.GetComponent<BoxCollider>().enabled = true;
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
    }
    public void StartVisualize()
    {
        gameObject.GetComponent<BoxCollider>().enabled = false;
        gameObject.GetComponent<Rigidbody>().isKinematic = true;
    }
}

/*
 * TODO
 * 
 * - Make procedural lego piece
 * 
 * - Make position class
 * - It should have a list of 'positions' with type 'point' and 'slot'
 * - They should know when they're occupied (and not be preferred) occupied = true/false
 * 
 * - Update preferred hand method to take into account target bounding box
 * 
 * - Fix fixed joint to make it actually solid
 * 
 *
 * 
 * 
 * Ghosting logic when grasping
 * - Which brick is preferred (ray to brick) - other color
 * - Which position on brick is preferred (ray from brick to position)
 * - Which position on grasped brick is preferred (ray from brick to grasped brick position)
 * - If first pref is top, second must be bottom (check in loop over positions)
 * 
 */


