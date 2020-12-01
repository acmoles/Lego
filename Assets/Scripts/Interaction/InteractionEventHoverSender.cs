using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;
using Leap;
using Leap.Unity;

public class InteractionEventHoverSender : InteractionEventSender
{
    protected InteractionManager manager;
    protected InteractionHand handRight, handLeft;
    protected PinchDetector pinchDetectorLeft, pinchDetectorRight;
    public float hoverDistance = 0.05f;
    public Transform hoverTarget;

    [HideInInspector] public InteractionHand closestHand;
    [HideInInspector] public Vector3 closestHandPinchPosition;
    [HideInInspector] public float closestHandDist = float.PositiveInfinity;
    protected float dL, dR;
    protected bool handIsClose = false;

    [HideInInspector] public bool hovered = false;
    [HideInInspector] public bool pinched = false;
    [HideInInspector] public bool holding = false;

    protected virtual void Start()
    {
        manager = Game.Instance.manager;
        handLeft = Game.Instance.handLeft;
        handRight = Game.Instance.handRight;
        pinchDetectorLeft = Game.Instance.pinchDetectorLeft;
        pinchDetectorRight = Game.Instance.pinchDetectorRight;
    }

    protected virtual void Update()
    {
        closestHand = null;

        //find closest hand
        dL = HandDistance(handLeft);
        dR = HandDistance(handRight);

        if (dL <= dR) closestHand = handLeft;
        else if (dR < dL) closestHand = handRight;

        closestHandPinchPosition = closestHand.transform.GetChild(2).position;

        closestHandDist = Mathf.Min(dL, dR);
        handIsClose = closestHandDist < hoverDistance * hoverDistance;

        if (handIsClose && !hovered)
        {
            hovered = true;
            _OnHoverBegin();
        }

        if (!handIsClose && !pinched && hovered)
        {
            hovered = false;
            _OnHoverEnd();
        }

        if (hovered && !pinched && isClosestPinching() && !isClosestHandHolding())
        {
            pinched = true;
            _OnPinchBegin();
        }

        if (pinched && !isClosestPinching())
        {
            pinched = false;
            _OnPinchEnd();
        }

        if (hovered && !holding && isClosestPinching() && isClosestHandHolding())
        {
            holding = true;
            _OnHoldingBegin();
        }

        if (holding && !isClosestHandHolding())
        {
            holding = false;
            _OnHoldingEnd();
        }

        if (holding)
        {
            _OnHoldingSustain();
        }
    }

    public float HandDistance(InteractionHand hand)
    {
        if (!hand.isTracked) return float.PositiveInfinity;
        Vector3 hoverPoint = transform.position;
        if (hoverTarget) hoverPoint = hoverTarget.position;
        Vector3 handPoint = hand.transform.GetChild(2).position; // Pinch position
        return (hoverPoint - handPoint).sqrMagnitude;
    }

    public bool isClosestPinching()
    {
        return closestHand != null &&
               (closestHand.isLeft ? pinchDetectorLeft.IsActive : pinchDetectorRight.IsActive);
    }

    public bool isClosestHandHolding()
    {
        if (closestHand == null) Debug.LogWarning("Closest hand is null");
        return closestHand != null && closestHand.isGraspingObject;
    }

    public bool isHandClose(InteractionHand hand)
    {
        if (hand.isTracked)
        {
            float dist = hand.isLeft ? dL : dR;

            if (dist < hoverDistance * hoverDistance)
                return true;
        }

        return false;
    }

}
