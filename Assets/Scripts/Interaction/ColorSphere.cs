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
        base.Start();
        startOpacity = skinnedMeshRenderer.sharedMaterial.GetFloat("_Alpha");
    }

    protected override bool ActivateCondition()
    {
        return hoverSender.hovered && hoverSender.isClosestHandHolding() && !dial.positionsUp;
    }

    protected override void OnCountdownFinished()
    {
        Debug.Log("Stop: change color");
        if (Game.Instance.heldShapes.Count > 0)
        {
            foreach (var item in Game.Instance.heldShapes)
            {
                item.SetColor((int)(dial.activeColor));
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
