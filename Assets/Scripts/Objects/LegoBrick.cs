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



    protected Shape _shape;
    protected Transform _shapeMesh;
    Bounds meshBounds;
    const float importScaleFactor = 100f;

    public bool visualize = true;

    List<Vector3> slots = new List<Vector3>();
    List<Vector3> points = new List<Vector3>();

    const float halfSize = 1.139775f; // Distance between points?
    const float height = 0.45f;

    void Awake()
    {
        allLegoBricks.Add(this);
    }

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
        //else if (_grasped && lastLegoBrick == null)
        //{
        //    DestroyGhost();
        //} else if (_grasped && lastLegoBrick != null)
        //{
        //    MakeGhost();
        //}
        //UpdateConnectionTo();
    }

    public LegoBrick FindPreferredLegoBrick()
    {
        LegoBrick preferredLegoBrick = null;
        float closestDistSqrd = float.PositiveInfinity;
        foreach (var brick in allLegoBricks)
        {
            float testDistanceSqrd = (brick.transform.position - this.transform.position).sqrMagnitude;
            if (testDistanceSqrd < closestDistSqrd && brick != this && brick != connectedTo && !connectedToMe.Contains(brick))
            {
                preferredLegoBrick = brick;
                closestDistSqrd = testDistanceSqrd;
            }
        }

        if (closestDistSqrd < maxGhostDistance * maxGhostDistance)
        {
            // Visualise selection
            Debug.DrawLine(this.transform.position, preferredLegoBrick.transform.position, Color.red);
            return preferredLegoBrick;
        } else
        {
            return null;
        }
    }

    public void onGraspBegin ()
    {
        _shape.SetColor(Color.magenta);
        _grasped = true;
        MakeGhost();
    }

    public void onGraspStay ()
    {
        lastLegoBrick = FindPreferredLegoBrick();
    }

    public void onGraspEnd ()
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

    public void PositionGhost(LegoBrick other, Vector3 targetPosition)
    {
        if (ghost == null)
        {
            return;
        }
        connectedToHover = other;

        Vector3 connectedToHoverPreferredLocalPoint = other.FindClosestPoint(other.transform.InverseTransformPoint(targetPosition));
        Vector3 worldPoint = other.transform.TransformPoint(connectedToHoverPreferredLocalPoint);
        Vector3 myHoverPreferredLocalSlot = FindClosestSlot(transform.InverseTransformPoint(worldPoint));
        Vector3 worldSlot = ghost.transform.TransformPoint(myHoverPreferredLocalSlot);
        ghost.transform.position += worldPoint - worldSlot;
        ghost.transform.rotation = other.transform.rotation;
    }

    Vector3 _ghostPosition;
    Quaternion _ghostRotation;

    public void ConnectTo(LegoBrick other)
    {
        if (other == null)
        {
            return;
        }

        //var p = transform.position;

        Disconnect();

        connectedTo = other;

        this.transform.position = _ghostPosition;
        this.transform.rotation = _ghostRotation;

        //GetComponent<BoxCollider>().enabled = true;
        //Physics.IgnoreCollision(this.GetComponent<Collider>(), connectedTo.GetComponent<Collider>(), true);

        //transform.position = p;

        connectionJoint = gameObject.AddComponent<FixedJoint>();
        connectionJoint.connectedBody = connectedTo.GetComponent<Rigidbody>();

        //transform.position = p;

        connectedTo.connectedToMe.Add(this);

        //PositionWarped();
        Debug.Log("Connection!");
    }

    public void Disconnect()
    {
        if (!IsConnected()) return;

        Destroy(connectionJoint);

        //Physics.IgnoreCollision(GetComponent<Collider>(), connectedTo.GetComponent<Collider>(), false);

        connectedTo.connectedToMe.Remove(this);
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

    public void PositionWarped()
    {
        foreach (var lp in connectedToMe)
        {
            //lp.SetPositionGhost();
            lp.PositionWarped();
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

    bool _ghosting;
    Transform ghost;
    MeshRenderer ghostRenderer;
    public Material ghostMaterial;

    public void MakeGhost()
    {
        _ghosting = true;
        ghost = Instantiate(_shapeMesh);
        ghost.name = "Ghost";
        var list = GetComponents(typeof(Component));
        for (int i = 0; i < list.Length; i++)
        {
            Debug.Log(list[i].name);
        }
        ghostRenderer = ghost.GetComponent<MeshRenderer>();
        ghostRenderer.material = ghostMaterial;
    }

    public void DestroyGhost()
    {
        if (_ghosting)
        {
            _ghosting = false;
            _ghostPosition = ghost.transform.position;
            _ghostRotation = ghost.transform.rotation;
            Destroy(ghost.gameObject);
            ghost = null;
            ghostRenderer = null;
            Debug.Log("Destroy Ghost");
        }
            else
        {
            Debug.LogWarning("Already destroyed ghost. " + ghost);
        }
    }

    public bool IsConnected()
    {
        return connectedTo != null;
    }
}
