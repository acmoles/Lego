using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ColorSphere : CloneSphere
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Dial dial;

    protected override void Start()
    {
        hoverSender = (InteractionEventHoverSender)sender;
        handLeft = Game.Instance.handLeft;
        handRight = Game.Instance.handRight;

        originalScale = highlightSphere.localScale;
        originalPosition = highlightSphere.position;

        startOpacity = skinnedMeshRenderer.sharedMaterial.GetFloat("_Alpha");
    }

    protected override void OnCountdownFinished()
    {
        Debug.Log("Stop: change color");
        // Set color of held brick(s)
        if (Game.Instance.heldShapes.Count > 0)
        {
            foreach (var item in Game.Instance.heldShapes)
            {
                item.SetColor((int)dial.activeColor);
            }
        }
    }

    protected override void OnCountdownAbort(string location)
    {
        Debug.Log("Stop: no color change " + location);
    }

    protected override void SetAlpha(float to, float t = 0.15f)
    {
        if (skinnedMeshRenderer.material.HasProperty("_Alpha"))
        {
            float currentAlpha = skinnedMeshRenderer.sharedMaterial.GetFloat("_Alpha");
            Tween tween = DOTween.To(() => { return currentAlpha; }, x => { currentAlpha = x; }, to, t);
            tween.OnUpdate(() =>
            {
                skinnedMeshRenderer.sharedMaterial.SetFloat("_Alpha", currentAlpha);
            });
        }
    }
}
