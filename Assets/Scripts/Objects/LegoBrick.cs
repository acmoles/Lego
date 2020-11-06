using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Leap;
using Leap.Unity.Interaction;
using UnityEditor;
using Leap.Unity;

[SelectionBase]
[RequireComponent(typeof(Shape))]
[RequireComponent(typeof(LegoBrickSetup))]
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
    static LegoBrick preferredLegoBrick;

    protected Shape _shape;
    protected Transform _shapeMesh;
    public GameObject thisPrefab;
    Bounds meshBounds;
    const float importScaleFactor = 100f;

    public bool visualize = true;
    public bool kinematic = false;

    public List<LocalPosition> slots = new List<LocalPosition>();
    public List<LocalPosition> points = new List<LocalPosition>();

    const float halfSize = 1.139775f; // Distance between points
    const float height = 0.45f;
    const float mass = 0.1f;

    void Awake()
    {
        if (kinematic) GetComponent<Rigidbody>().isKinematic = true;
        allLegoBricks.Add(this);
        interactionBehaviour.manager = Game.Instance.manager;
    }

    public void Init()
    {
        gameObject.GetComponent<Rigidbody>().mass = mass;
        _shape = GetComponent<Shape>();
        meshBounds = _shape.meshRenderer.bounds;
        _shapeMesh = _shape.meshTransform;

        /* TODO new init with LEGO models
         * 
         * - find children of connectivity and get localPostion for children in both lists, adding to points/slots appropriately
         * - ? rename to follow Lego conventions?
         * 
         * Logic in test file
         * 
         */

        // Compensate for scale
        float compX = (importScaleFactor * meshBounds.extents.x / _shapeMesh.localScale.x) / halfSize;
        float compZ = (importScaleFactor * meshBounds.extents.z / _shapeMesh.localScale.z) / halfSize;

        int xCount = (int)Math.Round(compX);
        int zCount = (int)Math.Round(compZ);
        //Debug.Log("init lego brick: " + xCount + ", " + zCount);

        float scaledHalfSize = (halfSize * _shapeMesh.localScale.x) / importScaleFactor;
        float scaledHeight = (height * _shapeMesh.localScale.y) / importScaleFactor;
        
        if (visualize) VisualizePosition.Create(gameObject, new Vector3(meshBounds.min.x + scaledHalfSize, meshBounds.max.y - scaledHeight, meshBounds.min.z + scaledHalfSize), 0.02f);

        for (int x = 0; x < xCount; x++)
        {
            for (int z = 0; z < zCount; z++)
            {
                var slot = new LocalPosition();
                slot.position = meshBounds.min + new Vector3(scaledHalfSize + x * scaledHalfSize * 2, 0, scaledHalfSize + z * scaledHalfSize * 2);
                slots.Add(slot);

                var point = new LocalPosition();
                point.position = slot.position;
                point.position.y = meshBounds.max.y;
                point.position.y -= scaledHeight; // Height of a lego brick
                points.Add(point);

                if (visualize)
                {
                    VisualizePosition.Create(gameObject, point.position, 0.01f);
                    VisualizePosition.Create(gameObject, slot.position, 0.005f);
                }
            }
        }

        // TODO dynamically set bounding box based on base mesh
        // OR use colliders from imported Lego model - see their collider combining logic
        // How to use child colliders with rigid body?
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(meshBounds.center, meshBounds.size);
        Gizmos.DrawWireSphere(meshBounds.center, 0.01f);
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


    public InteractionBehaviour interactionBehaviour;

    bool _grasped = false;
    public float maxGhostDistance = 0.1f;
    LegoBrick hoverTarget;

    public LegoBrick connectedTo;
    ConfigurableJoint connectionJoint;
    List<LegoBrick> connectedToMe = new List<LegoBrick>();

    bool _connecting = false;

    public void Update()
    {
        PositionGhost();
        UpdateConnectionTo();
    }

    Vector3 otherClosestPointToMe;
    Vector3 myClosestPointToOther;
    public LegoBrick FindPreferredLegoBrick()
    {
        // Nearby brick all
        LegoBrick preferredLegoBrick = null;
        float closestDistSqrd = float.PositiveInfinity;
        foreach (var brick in allLegoBricks)
        {
            otherClosestPointToMe = brick.GetComponent<Collider>().ClosestPoint(this.transform.position);
            myClosestPointToOther = GetComponent<Collider>().ClosestPoint(otherClosestPointToMe);

            float testDistanceSqrd = (otherClosestPointToMe - myClosestPointToOther).sqrMagnitude;
            if (testDistanceSqrd < closestDistSqrd && brick != this && brick != connectedTo && !connectedToMe.Contains(brick))
            {
                preferredLegoBrick = brick;
                closestDistSqrd = testDistanceSqrd;
            }
        }

        // Nearby brick below
        var ray = new Ray(transform.position, -transform.up);
        if (visualize) Debug.DrawRay(transform.position, -transform.up, Color.white);
        RaycastHit raycastHit;
        if (Physics.Raycast(ray, out raycastHit))
        {
            var hitBrick = raycastHit.collider.gameObject.GetComponent<LegoBrick>();
            if (hitBrick != this && hitBrick != connectedTo && !connectedToMe.Contains(hitBrick))
            {
                float belowDistSqrd = (hitBrick.transform.position - transform.position).sqrMagnitude;

                // Choose which to use, preferring below bricks
                if (belowDistSqrd < 100*closestDistSqrd)
                {
                    preferredLegoBrick = hitBrick;
                    closestDistSqrd = belowDistSqrd;
                }
            }
        }

        // Reset nearest points for ghosting
        otherClosestPointToMe = preferredLegoBrick.GetComponent<Collider>().ClosestPoint(this.transform.position);
        myClosestPointToOther = GetComponent<Collider>().ClosestPoint(otherClosestPointToMe);
        if (visualize) Debug.DrawLine(otherClosestPointToMe, myClosestPointToOther, Color.red);

        if (closestDistSqrd < maxGhostDistance * maxGhostDistance)
        {
            // Visualise selection
            MakeGhost();
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
        preferredLegoBrick = FindPreferredLegoBrick();
    }

    public void onGraspEnd()
    {
        _grasped = false;
        _shape.ResetColor();

        // Start connection at Ghost
        if (_ghosting)
        {
            _connecting = true;
            StartConnectLerp();
        }
    }

    public void PositionGhost()
    {
        if (!_ghosting || !_grasped || preferredLegoBrick.transform == null || preferredLegoBrick == this)
        {
            return;
        }
        hoverTarget = preferredLegoBrick;

        // Position

        LocalPosition otherClosestPoint = LegoStaticUtils.FindClosestPosition(
            hoverTarget.transform.InverseTransformPoint(myClosestPointToOther),
            hoverTarget.points
        );
        if (otherClosestPoint == null)
        {
            DestroyGhost();
            return;
        }
        Vector3 otherWorldClosestPoint = hoverTarget.transform.TransformPoint(otherClosestPoint.position);

        LocalPosition myClosestSlot = LegoStaticUtils.FindClosestPosition(
            transform.InverseTransformPoint(otherWorldClosestPoint),
            slots
        );
        Vector3 ghostWorldClosestSlot = ghost.transform.TransformPoint(myClosestSlot.position); //!

        if (visualize) Debug.DrawLine(transform.position, ghostWorldClosestSlot, Color.cyan);
        ghost.transform.position += otherWorldClosestPoint - ghostWorldClosestSlot;

        // Rotation

        // Must always be xz plane (world)
        Vector3 alignedForward = LegoStaticUtils.ClosestLegoWorldAxis(transform.forward);
        if (alignedForward.x == 0 && alignedForward.z == 0)
        {
            Debug.Log("! alignForward problem: " + alignedForward);
            alignedForward = new Vector3(1, 0, 0);
        }
        
        // Must always be up/down (world)
        Vector3 alignedUp = Vector3.up;
        //Debug.DrawRay(worldPoint, alignedUp, Color.cyan);
        //Debug.DrawRay(worldPoint, alignedForward, Color.cyan);

        // Find closest non-vertical local axis in other to alignForward
        Vector3 otherClosestAxis = LegoStaticUtils.ClosestLegoLocalDirection(alignedForward, hoverTarget.transform);
        //Debug.DrawRay(other.transform.position, otherClosestAxis, Color.red);
        //Debug.DrawRay(other.transform.position, other.transform.up, Color.red);

        // Make that forward direction for LookRotation
        Quaternion otherLocalRotation = Quaternion.LookRotation(otherClosestAxis, hoverTarget.transform.up);

        ghost.transform.rotation = otherLocalRotation;

        // TODO Add check for valid ghost position. Change color using Lego method?
        // Split lego brick setup and lego brick behavior? Even more so when using imported bricks (XR bootstrap).
    }

    public void ConnectTo()
    {
        if (hoverTarget == null)
        {
            return;
        }

        // Check valid ghosting position for connection



        connectedTo = hoverTarget;
        hoverTarget = null;

        // Stop collisions with connectedTo
        Physics.IgnoreCollision(this.GetComponent<Collider>(), connectedTo.GetComponent<Collider>(), true);

        // TODO switch to parenting to common parent

        connectionJoint = gameObject.AddComponent<ConfigurableJoint>();
        connectionJoint.enableCollision = false;
        //connectionJoint.breakForce = Mathf.Infinity;
        //connectionJoint.breakTorque = Mathf.Infinity;
        //connectionJoint.anchor = new Vector3(0, 0, 0);
        connectionJoint.xMotion = ConfigurableJointMotion.Locked;
        connectionJoint.yMotion = ConfigurableJointMotion.Locked;
        connectionJoint.zMotion = ConfigurableJointMotion.Locked;
        connectionJoint.angularXMotion = ConfigurableJointMotion.Locked;
        connectionJoint.angularYMotion = ConfigurableJointMotion.Locked;
        connectionJoint.angularZMotion = ConfigurableJointMotion.Locked;
        //connectionJoint.targetPosition = new Vector3(0, 0, 0);
        connectionJoint.connectedBody = connectedTo.GetComponent<Rigidbody>();

        connectedTo.connectedToMe.Add(this);
        LegoStaticUtils.SetOccupiedGridPositions(this, connectedTo);
        SetMass();
        //MassPropagate();
        Debug.Log("Connection!");
    }

    public void Disconnect()
    {
        if (!IsConnected()) return;
        _shape.ResetColor();
        // Re-allow collisions with connectedTo
        Physics.IgnoreCollision(this.GetComponent<Collider>(), connectedTo.GetComponent<Collider>(), false);

        Destroy(connectionJoint);
        LegoStaticUtils.SetOccupiedGridPositions(this, connectedTo);
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

        float shortDistance = ((slots[0].position - slots[1].position).magnitude) / 100f;

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
        if (visualize) Debug.DrawLine(targetPosition, finalPosition, Color.red);

        if (Vector3.Distance(finalPosition, targetPosition) < shortDistance && Quaternion.Angle(targetRotation, finalRotation) < 2F)
        {
            //Debug.Log("event");
            _connecting = false;
            DestroyGhost();
            ConnectTo();
            EndConnectLerp();
        }

        // Set final position
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
        if (ghostRenderer)
        {
            float alpha = ghostRenderer.sharedMaterial.GetFloat("_GlobalAlpha");
            float finalAlpha = Mathf.Lerp(alpha, 0f, lerpSpeed * Time.deltaTime);
            ghostRenderer.sharedMaterial.SetFloat("_GlobalAlpha", finalAlpha);
        }
    }

    /*
    Could instead do 'attempt connect' with the LERP - if collision Abort _connecting = false maybe flash red!!!
    With LERP maybe need to propagate kinematic = true through heirarchy of ghosttarget/ other to prevent physics impulse
    Then turn it back on with a successful connection, or after an aborted connection
    Could do sanity check...

    Update: seems redundant
    */
    //private void OnCollisionEnter(Collision collision)
    //{
        //var hitbrick = collision.gameObject.GetComponent<LegoBrick>();
        //if (hitbrick && _connecting)
        //{
        //    Debug.Log("abort lerp!");
        //    _connecting = false;
        //    EndConnectLerp();
        //}
    //}

    public void SetMass()
    {
        int depth = LegoStaticUtils.FindLegoDepth(this);
        float mass = gameObject.GetComponent<Rigidbody>().mass;
        gameObject.GetComponent<Rigidbody>().mass = mass < 0.01f ? 0.01f : mass -= 0.02f * depth;
        //Debug.Log("My mass: " + gameObject.GetComponent<Rigidbody>().mass);
    }

    public void DisconnectPropagate()
    {
        foreach (var brick in connectedToMe)
        {
            brick.Disconnect();
            brick.DisconnectPropagate();
        }
    }

    bool _ghosting = false;
    GameObject ghost;
    MeshRenderer ghostRenderer;
    public Material ghostMaterial;

    public void MakeGhost()
    {
        if (!_ghosting)
        {
            _ghosting = true;

            //ghost = Instantiate(thisPrefab);
            //ghost.name = "Ghost";

            //MonoBehaviour[] comps = GetComponents<MonoBehaviour>();
            //foreach (MonoBehaviour c in comps)
            //{
            //    c.enabled = false;
            //}
            ghost = Instantiate(_shapeMesh.gameObject);
            ghost.name = "Ghost";

            ghostRenderer = ghost.GetComponent<MeshRenderer>();
            //ghostRenderer.enabled = true;
            ghostRenderer.sharedMaterial = ghostMaterial;
            ghostRenderer.sharedMaterial.SetFloat("_GlobalAlpha", 1.0f);
        }
    }

    public void DestroyGhost()
    {
        if (_ghosting && ghost != null)
        {
            _ghosting = false;
            Destroy(ghost.gameObject);
            ghost = null;
            ghostRenderer = null;
            //Debug.Log("Destroy Ghost");
        }
    }

    public bool IsConnected()
    {
        return connectedTo != null;
    }

    public void EndConnectLerp()
    {
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
    }
    public void StartConnectLerp()
    {
        gameObject.GetComponent<Rigidbody>().isKinematic = true;
    }
}
