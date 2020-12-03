using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Leap;
using Leap.Unity.Interaction;
using UnityEditor;
using Leap.Unity;
using DG.Tweening;

// TODO fix 1x2 brick shell

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
            interactionBehaviour.rigidbody.angularDrag = 1f;
            interactionBehaviour.rigidbody.mass = mass;
        }
        else
        {
            if (TryGetComponent(out Rigidbody rb))
            {
                if (kinematic) rb.isKinematic = true;
                rb.maxAngularVelocity = 10f;
                rb.angularDrag = 1f;
                rb.mass = mass;
            }
        }

        if (interactionBehaviour) interactionBehaviour.manager = Game.Instance.manager;
        else Debug.Log("Missing interaction behaviour: " + gameObject.name);

        allLegoBricks.Add(this);
        Init();
    }

    public void Init()
    {
        // Currently used to color brick visual
        _shape = GetComponent<Shape>();
        _shape.init();
        setup = GetComponent<LegoBrickSetup>();
        _turntableMember = GetComponent<TurntableMember>();
        StartCoroutine("UpdateOccupiedGridPositions");
    }

    public void onHoverBegin()
    {
        TweenEmission(0.2f, 0.15f);
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

    public float emissionAmount = 0.5f;
    float currentEmission = 0f;
    public void onGraspBegin()
    {
        TweenEmission(emissionAmount, 0.15f);
        Disconnect();
        Game.Instance.heldShapes.Add(this._shape);
    }

    void TweenEmission(float to, float t)
    {
        // Need to clear material property block to keep current color
        Tween tween = DOTween.To(() => { return currentEmission; }, x => { currentEmission = x; }, to, t);
        tween.OnUpdate(() =>
        {
            _shape.SetEmission(currentEmission);
        }).OnComplete(() => {
            TweenEmission(0f, 1f);
        });
    }

    public void onGraspStay()
    {
        preferredLegoBrick = FindPreferredLegoBrick();
    }

    public void onGraspEnd()
    {
        if (Game.Instance.heldShapes.Contains(this._shape))
        {
            Game.Instance.heldShapes.Remove(this._shape);
        } else
        {
            Debug.LogWarning("Shape missing from list?");
        }

        // Start connection at Ghost
        if (_ghosting)
        {
            Bounds ghostBounds = ghost.GetComponent<MeshFilter>().sharedMesh.bounds;
            Collider[] hitColliders = Physics.OverlapBox(ghost.transform.position, ghostBounds.extents, ghost.transform.rotation, layerMask);

            foreach (Collider hit in hitColliders)
            {
                if (hit.bounds.Contains(ghost.transform.TransformPoint(ghostBounds.center)) && hit.gameObject.GetComponent<LegoBrick>() != null)
                {
                    Debug.Log("Abort connect on account of ghost/brick intersection. To " + hit.gameObject.name + ", from: " + gameObject.name);
                    DestroyGhost();
                    StopKinematic();
                    return;
                }
            }

            _connecting = true;
            MakeKinematic();
        } else
        {
            StopKinematic();
        }
    }

    public void PositionGhost()
    {
        if (!_ghosting || preferredLegoBrick == null || preferredLegoBrick == this)
        {
            return;
        }

        if (ghostRenderer.sharedMaterial.GetFloat("_GlobalAlpha") == 0 && !ghostFadingOut)
        {
            ghostRenderer.sharedMaterial.SetFloat("_GlobalAlpha", 1.0f);
            Debug.Log("Invisible ghost?");
        }

        hoverTarget = preferredLegoBrick;

        // Position

        Connectivity otherClosestKnob = LegoStaticUtils.FindClosestPosition(
            hoverTarget.transform.InverseTransformPoint(myClosestPointToOther),
            hoverTarget.setup.knobs
        );
        if (otherClosestKnob == null)
        {
            DestroyGhost();
            return;
        }
        Vector3 otherWorldClosestKnob = hoverTarget.transform.TransformPoint(otherClosestKnob.position);

        Connectivity myClosestSlot = LegoStaticUtils.FindClosestPosition(
            transform.InverseTransformPoint(otherWorldClosestKnob),
            setup.slots
        );
        if (myClosestSlot == null)
        {
            DestroyGhost();
            return;
        }
        Vector3 ghostClosestSlot = ghost.transform.TransformPoint(myClosestSlot.position);

        if (visualize) Debug.DrawLine(transform.position, ghostClosestSlot, Color.cyan);
        ghost.transform.position += otherWorldClosestKnob - ghostClosestSlot;


        // Rotation

        // Find closest lego plane local axis in other to transform.forward
        Vector3 otherClosestAxis = LegoStaticUtils.ClosestLegoLocalDirection(this.transform.forward, hoverTarget.transform, otherClosestKnob.rotation);
        if (visualize)
        {
            Debug.DrawRay(hoverTarget.transform.position, otherClosestAxis, Color.blue);
            Debug.DrawRay(hoverTarget.transform.position, otherClosestKnob.directionUp, Color.green);
        }

        // Make that forward direction for LookRotation (hoverTarget.transform.up)
        Quaternion otherLocalRotation = Quaternion.LookRotation(otherClosestAxis, hoverTarget.transform.TransformDirection(otherClosestKnob.directionUp));

        ghost.transform.rotation = otherLocalRotation;
    }

    public LayerMask layerMask;
    public void ConnectTo()
    {
        if (hoverTarget == null)
        {
            return;
        }

        connectedTo = hoverTarget;
        hoverTarget = null;

        _turntableMember.AddToTurntable();

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
        //SetMass();
        active = true;
        //Debug.Log(gameObject.name + " connection to: " + connectedTo.name);
    }

    public void Disconnect(bool propagate = true)
    {
        if (!IsConnected()) {
            return; 
        }

        //Debug.Log(gameObject.name + " disconnection from " + connectedTo.name);
        _turntableMember.RemoveFromTurntable();

        //Destroy(connectionJoint);

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
        foreach (var brick in connectedToMe.ToArray())
        {
            Debug.Log(brick.name + " disconnect propagate!");
            brick.StopKinematic();
            brick.Disconnect();
        }
    }

    public static void DisconnectAllBricks()
    {
        foreach (var brick in allLegoBricks)
        {
            brick.Disconnect();
            brick.StopKinematic();
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
    public float lerpSpeed = 10f;

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
            StopKinematic();
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

    //public void SetMass()
    //{
    //    Rigidbody rb = null;

    //    if (interactionBehaviour != null)
    //    {
    //        rb = interactionBehaviour.rigidbody;
    //    }
    //    else
    //    {
    //        rb = gameObject.GetComponent<Rigidbody>();
    //    }

    //    int depth = LegoStaticUtils.FindLegoDepth(this);
    //    float mass = rb.mass;
    //    mass -= 0.02f * depth;
    //    mass -= 0.02f * depth;
    //    rb.mass = mass < 0.01f ? 0.01f : mass;
    //    //Debug.Log("My mass: " + rb.mass);
    //}

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
            FadeGhostAlpha(0f);
        }
    }

    private bool ghostFadingOut = false;
    void FadeGhostAlpha(float to, float t = 0.15f)
    {
        if (ghostRenderer.sharedMaterial.HasProperty("_GlobalAlpha"))
        {
            ghostFadingOut = true;
            float currentAlpha = ghostRenderer.sharedMaterial.GetFloat("_GlobalAlpha");
            Tween tween = DOTween.To(() => { return currentAlpha; }, x => { currentAlpha = x; }, to, t);
            tween.OnUpdate(() =>
            {
                ghostRenderer.sharedMaterial.SetFloat("_GlobalAlpha", currentAlpha);
            }).OnComplete(() => {
                _ghosting = false;
                Destroy(ghost);
                ghost = null;
                Destroy(ghostRenderer);
                ghostRenderer = null;
                ghostFadingOut = false;
                Debug.Log("Destroy Ghost");
            });
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

        if (rb.isKinematic)
        {
            rb.isKinematic = false;
            if (rb.IsSleeping()) Debug.Log("Stop Kinematic was sleeping " + gameObject.name);
            rb.WakeUp();
            //Debug.Log("Stop Kinematic" + gameObject.name);
        }
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

        if (!rb.isKinematic)
        {
            rb.isKinematic = true;
            rb.Sleep();
            //Debug.Log("Start Kinematic " + gameObject.name);
        }
    }
}
