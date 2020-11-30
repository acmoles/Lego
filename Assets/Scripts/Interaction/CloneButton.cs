using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;
using Leap;
using Leap.Unity;

public class CloneButton : InteractionEventHoverSender
{
    public Transform highlightSphere;
    [Range(0, 100F)]
    public float lerpSpeed = 10f;
    public Vector3 highlightScale = new Vector3(0.1f, 0.1f, 0.1f);

    private Vector3 originalScale;
    private Vector3 originalPosition;

    protected override void Start()
    {
        base.Start();

        originalScale = highlightSphere.localScale;
        originalPosition = highlightSphere.position;
    }
    protected override void Update()
    {
        base.Update();

        if(hovered && isClosestHandHolding())
        {
            PinchDetector p = closestHand.isLeft ? pinchDetectorLeft : pinchDetectorRight;
            highlightSphere.position = Vector3.Lerp(highlightSphere.position, p.Position, lerpSpeed * Time.deltaTime);
            highlightSphere.localScale = Vector3.Lerp(highlightSphere.localScale, highlightScale, lerpSpeed * Time.deltaTime);
        } else
        {
            highlightSphere.position = Vector3.Lerp(highlightSphere.position, originalPosition, lerpSpeed * Time.deltaTime);
            highlightSphere.localScale = Vector3.Lerp(highlightSphere.localScale, originalScale, lerpSpeed * Time.deltaTime);
        }
        /* TODO
         * If hand is close and *holding Lego Brick* = *Game.Instance.heldShapes.Count > 0* then clone *All heldShapes*
         * if hand is close and finger touches button then add last held lego brick to scene ??
        */
    }


}
