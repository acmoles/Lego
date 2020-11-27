using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloneButton : InteractionEventHoverSender
{
    public Transform highlightSphere;
    public float lerpSpeed = 0.01f;
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

        if(hovered)
        {
            highlightSphere.position = Vector3.Lerp(highlightSphere.position, closestHand.position, lerpSpeed * Time.deltaTime);
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
