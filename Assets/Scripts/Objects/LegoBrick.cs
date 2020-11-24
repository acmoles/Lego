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
    public bool active = false;

    protected Shape _shape;
    [HideInInspector]
    public LegoBrickSetup setup;
    private TurntableMember _turntableMember;

    public bool visualize = true;
    public bool kinematic = false;

    const float mass = 0.1f;

    [Range(0, 0.01F)]
    public float smallDistance = 0.001f;

    [Range(0, 4F)]
    public float smallAngle = 1f;

    void Awake()
    {
        if (interactionBehaviour != null)
        {
            if (kinematic) interactionBehaviour.rigidbody.isKinematic = true;
            interactionBehaviour.rigidbody.maxAngularVelocity = 10f;
            interactionBehaviour.rigidbody.mass = mass;
        }
        else
        {
            if (TryGetComponent(out Rigidbody rb))
            {
                if (kinematic) rb.isKinematic = true;
                rb.maxAngularVelocity = 10f;
                rb.mass = mass;
            }
        }

        if (interactionBehaviour) interactionBehaviour.manager = Game.Instance.manager;
        else Debug.Log("Missing interaction behaviour: " + gameObject.name);

        // TODO active brick after first grab - except if plate or checked bool
        allLegoBricks.Add(this);
        Init();
    }

    public void Init()
    {
        // TODO currently used to color brick visual
        _shape = GetComponent<Shape>();
        _shape.init();
        setup = GetComponent<LegoBrickSetup>();
        _turntableMember = GetComponent<TurntableMember>();
        StartCoroutine("UpdateOccupiedGridPositions");
    }

    public void onHoverBegin()
    {
        // TODO on hover highlighting

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

    public float maxGhostDistance = 0.1f;
    LegoBrick hoverTarget;

    [HideInInspector]
    public LegoBrick connectedTo;
    ConfigurableJoint connectionJoint;
    List<LegoBrick> connectedToMe = new List<LegoBrick>();

    bool _connecting = false;

    public void Update()
    {
        PositionGhost();
        UpdateConnectionTo();
    }

    float maxSpeed = 200f;
    void FixedUpdate()
    {
        if (interactionBehaviour != null) interactionBehaviour.rigidbody.velocity = Vector3.ClampMagnitude(interactionBehaviour.rigidbody.velocity, maxSpeed);
    }

        Vector3 otherClosestPointToMe;
    Vector3 myClosestPointToOther;
    public LegoBrick FindPreferredLegoBrick()
    {
        LegoBrick preferredLegoBrick = null;
        float closestDistSqrd = float.PositiveInfinity;
        float closestVerticalSqrd = float.PositiveInfinity;

        // Nearby brick all
        foreach (var brick in allLegoBricks)
        {
            if (!brick.active) continue;

            otherClosestPointToMe = brick.GetComponent<Collider>().ClosestPoint(this.transform.position);
            myClosestPointToOther = GetComponent<Collider>().ClosestPoint(otherClosestPointToMe);

            float testDistanceSqrd = (otherClosestPointToMe - myClosestPointToOther).sqrMagnitude;

            Vector3 delta = otherClosestPointToMe - myClosestPointToOther;
            float testClosestVerticalSqrd = delta.y * delta.y;

            if (testDistanceSqrd < closestDistSqrd
                && testClosestVerticalSqrd < closestVerticalSqrd
                && brick != this && brick != connectedTo
                && !connectedToMe.Contains(brick))
            {
                preferredLegoBrick = brick;
                closestDistSqrd = testDistanceSqrd;
                closestVerticalSqrd = testClosestVerticalSqrd;
            }
        }        

        if (closestDistSqrd < maxGhostDistance * maxGhostDistance)
        {
            // Reset nearest points for ghosting
            otherClosestPointToMe = preferredLegoBrick.GetComponent<Collider>().ClosestPoint(this.transform.position);
            myClosestPointToOther = GetComponent<Collider>().ClosestPoint(otherClosestPointToMe);
            if (visualize) Debug.DrawLine(otherClosestPointToMe, myClosestPointToOther, Color.red);

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
        // TODO on grasp highlighting
        Disconnect();
    }

    public void onGraspStay()
    {
        preferredLegoBrick = FindPreferredLegoBrick();
    }

    public void onGraspEnd()
    {
        // Start connection at Ghost
        if (_ghosting)
        {
            Bounds ghostBounds = ghost.GetComponent<MeshFilter>().sharedMesh.bounds;
            Collider[] hitColliders = Physics.OverlapBox(ghost.transform.position, ghostBounds.extents, ghost.transform.rotation, layerMask);

            foreach (Collider hit in hitColliders)
            {
                if (hit.bounds.Contains(ghost.transform.TransformPoint(ghostBounds.center)) && hit.gameObject.GetComponent<LegoBrick>() != null)
                {
                    Debug.Log("Abort connect on account of ghost/brick intersection. " + hit.gameObject.name);
                    DestroyGhost();
                    return;
                }
            }

            _connecting = true;
            MakeKinematic();
        }
    }

    public void PositionGhost()
    {
        if (!_ghosting || preferredLegoBrick == null || preferredLegoBrick == this)
        {
            return;
        }
        hoverTarget = preferredLegoBrick;

        // Position

        LocalPosition otherClosestKnob = LegoStaticUtils.FindClosestPosition(
            hoverTarget.transform.InverseTransformPoint(myClosestPointToOther),
            hoverTarget.setup.knobs
        );
        if (otherClosestKnob == null)
        {
            DestroyGhost();
            return;
        }
        Vector3 otherWorldClosestPoint = hoverTarget.transform.TransformPoint(otherClosestKnob.position);

        LocalPosition myClosestSlot = LegoStaticUtils.FindClosestPosition(
            transform.InverseTransformPoint(otherWorldClosestPoint),
            setup.slots
        );
        Vector3 ghostWorldClosestSlot = ghost.transform.TransformPoint(myClosestSlot.position); //!

        if (visualize) Debug.DrawLine(transform.position, ghostWorldClosestSlot, Color.cyan);
        ghost.transform.position += otherWorldClosestPoint - ghostWorldClosestSlot;

        // Rotation

        // Must always be local xz plane

        // Find closest non-vertical local axis in other to transform.forward
        Vector3 otherClosestAxis = LegoStaticUtils.ClosestLegoLocalDirection(transform.forward, hoverTarget.transform);
        //Debug.DrawRay(other.transform.position, otherClosestAxis, Color.red);
        //Debug.DrawRay(other.transform.position, other.transform.up, Color.red);

        // Make that forward direction for LookRotation
        Quaternion otherLocalRotation = Quaternion.LookRotation(otherClosestAxis, hoverTarget.transform.up);

        ghost.transform.rotation = otherLocalRotation;
    }

    public LayerMask layerMask;
    public void ConnectTo()
    {
        if (hoverTarget == null)
        {
            return;
        }
        Disconnect();

        connectedTo = hoverTarget;
        hoverTarget = null;

        _turntableMember.AddToTurntable();

        // TODO switch to parenting with common parent?
        // Test keeping kinematic for connected bricks

        //connectionJoint = gameObject.AddComponent<ConfigurableJoint>();
        ////connectionJoint.enableCollision = false;
        //connectionJoint.breakForce = float.PositiveInfinity;
        //connectionJoint.breakTorque = float.PositiveInfinity;
        //connectionJoint.autoConfigureConnectedAnchor = true;

        //connectionJoint.xMotion = ConfigurableJointMotion.Locked;
        //connectionJoint.yMotion = ConfigurableJointMotion.Locked;
        //connectionJoint.zMotion = ConfigurableJointMotion.Locked;
        //connectionJoint.angularXMotion = ConfigurableJointMotion.Locked;
        //connectionJoint.angularYMotion = ConfigurableJointMotion.Locked;
        //connectionJoint.angularZMotion = ConfigurableJointMotion.Locked;
        //connectionJoint.connectedBody = connectedTo.GetComponent<Rigidbody>();

        connectedTo.connectedToMe.Add(this);
        LegoStaticUtils.SetOccupiedGridPositions(this, connectedTo);
        SetMass();
        active = true;
        Debug.Log(gameObject.name + " connection to: " + connectedTo.name);
    }

    public void Disconnect(bool propagate = true)
    {
        if (!IsConnected()) {
            Debug.Log("Not connected!");
            return; 
        }

        Debug.Log(gameObject.name + " disconnection from " + connectedTo.name);
        _turntableMember.RemoveFromTurntable();
        StopKinematic();
        Destroy(connectionJoint);
        LegoStaticUtils.SetOccupiedGridPositions(this, connectedTo);

        if (propagate && connectedToMe.Count > 0)
        {
            DisconnectPropagate();
        }
        connectedTo.connectedToMe.Remove(this);
        connectedTo = null;
    }

    public void DisconnectPropagate()
    {
        foreach (var brick in connectedToMe)
        {
            //brick.DisconnectPropagate();
            brick.Disconnect();
        }
    }

    //public void OnJointBreak(float breakForce)
    //{
    //    Disconnect();
    //    Debug.Log("Joint broke");
    //}

    IEnumerator UpdateOccupiedGridPositions()
    {

        while (true)
        {
            if (IsConnected())
            {
                LegoStaticUtils.SetOccupiedGridPositions(this, connectedTo);
            }
            yield return new WaitForSeconds(10);
        }

    }

    [Range(0, 100F)]
    public float lerpSpeed = 20f;

    public void UpdateConnectionTo()
    {
        if (!_connecting)
        {
            return;
        }

        if (ghost == null)
        {
            Debug.Log("No ghost connecting");
            _connecting = false;
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
        if (visualize) Debug.DrawLine(targetPosition, finalPosition, Color.red);

        if (Vector3.Distance(finalPosition, targetPosition) < smallDistance && Quaternion.Angle(targetRotation, finalRotation) < smallAngle)
        {
            //Debug.Log("event: " + Vector3.Distance(finalPosition, targetPosition) + " : " + Quaternion.Angle(targetRotation, finalRotation));
            _connecting = false;
            DestroyGhost();
            ConnectTo();
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

    public void SetMass()
    {
        Rigidbody rb = null;

        if (interactionBehaviour != null)
        {
            rb = interactionBehaviour.rigidbody;
        }
        else
        {
            rb = gameObject.GetComponent<Rigidbody>();
        }

        int depth = LegoStaticUtils.FindLegoDepth(this);
        float mass = rb.mass;
        mass -= 0.02f * depth;
        rb.mass = mass < 0.01f ? 0.01f : mass;
        //Debug.Log("My mass: " + rb.mass);
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

            ghost = new GameObject();
            ghost.name = "Ghost";
            MeshFilter filter = ghost.AddComponent<MeshFilter>();
            filter.mesh = _shape.CombinedMesh;
            
            ghostRenderer = ghost.AddComponent<MeshRenderer>();
            ghostRenderer.sharedMaterial = ghostMaterial;
            ghostRenderer.sharedMaterial.SetFloat("_GlobalAlpha", 1.0f);
            //Debug.Log("Make Ghost");
        }
    }

    public void DestroyGhost()
    {
        if (_ghosting && ghost != null)
        {
            // TODO fade out ghost
            _ghosting = false;
            Destroy(ghost);
            ghost = null;
            Destroy(ghostRenderer);
            ghostRenderer = null;
            //Debug.Log("Destroy Ghost");
        }
    }

    public bool IsConnected()
    {
        return connectedTo != null;
    }

    public void StopKinematic()
    {
        Rigidbody rb = null;

        if (interactionBehaviour != null)
        {
            rb = interactionBehaviour.rigidbody;
        }
        else
        {
            rb = gameObject.GetComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.WakeUp();
        Debug.Log("Stop Kinematic");
    }
    public void MakeKinematic()
    {
        Rigidbody rb = null;

        if (interactionBehaviour != null)
        {
            rb = interactionBehaviour.rigidbody;
        }
        else
        {
            rb = gameObject.GetComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.Sleep();
    }
}
