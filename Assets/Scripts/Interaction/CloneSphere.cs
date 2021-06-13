using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;
using Leap;
using Leap.Unity;
using DG.Tweening;

public class CloneSphere : InteractionEventReceiver
{
    protected InteractionEventHoverSender hoverSender;
    public Transform highlightSphere;
    [Range(0, 100F)]
    public float lerpSpeed = 10f;
    public float shrinkTime = 5f;
    public Vector3 highlightScale = new Vector3(0.1f, 0.1f, 0.1f);

    protected Vector3 originalScale;
    protected Vector3 originalPosition;

    protected InteractionHand handRight, handLeft;

    protected bool cycledOnce = false;

    protected static Dictionary<string, CloneSphere> _allSpheres;
    public static Dictionary<string, CloneSphere> AllSpheres
    {
        get
        {
            if (_allSpheres == null)
            {
                _allSpheres = new Dictionary<string, CloneSphere>();
            }
            return _allSpheres;
        }
    }
    protected virtual string sphereName
    {
        get
        {
            return "clone";
        }
    }
    public bool active = false;

    protected virtual void Start()
    {
        hoverSender = (InteractionEventHoverSender)sender;
        handLeft = Game.Instance.handLeft;
        handRight = Game.Instance.handRight;

        originalScale = highlightSphere.localScale;
        originalPosition = highlightSphere.position;

        AllSpheres[sphereName] = this;
        Debug.Log(AllSpheres[sphereName].sphereName);

        if (meshRenderer)
        {
            startOpacity = meshRenderer.sharedMaterial.GetFloat("_Alpha");
        }
    }

    public float repeatTimout = 1f;
    private IEnumerator coroutine;
    private void Update()
    {
        if (ActivateCondition())
        {
            AllSpheres[sphereName].active = true;

            Shape closestHeldShape = null;
            float closestDistSqrd = float.PositiveInfinity;
            if (cachedHeldShapes.Count > 0)
            {
                foreach (var item in cachedHeldShapes)
                {
                    float testDistanceSqrd = (item.transform.position - transform.position).sqrMagnitude;
                    if (testDistanceSqrd < closestDistSqrd)
                    {
                        closestHeldShape = item;
                        closestDistSqrd = testDistanceSqrd;
                    }
                }
            }

            highlightSphere.position = Vector3.Lerp(highlightSphere.position, closestHeldShape.transform.GetChild(0).Find("TrueCenter").position, lerpSpeed * Time.deltaTime);

            if (coroutine == null && highlightSphere.localScale == highlightScale && !cycledOnce)
            {
                coroutine = Shrink(shrinkTime);
                StartCoroutine(coroutine);
            }

            if (coroutine == null)
            {
                highlightSphere.localScale = Vector3.Lerp(highlightSphere.localScale, highlightScale, lerpSpeed * Time.deltaTime);
            }

        }
        else
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
                OnCountdownAbort("update");
            }

            highlightSphere.position = Vector3.Lerp(highlightSphere.position, originalPosition, lerpSpeed * Time.deltaTime);

            if (coroutine == null)
            {
                highlightSphere.localScale = Vector3.Lerp(highlightSphere.localScale, originalScale, lerpSpeed * Time.deltaTime);
            }
        }
    }

    protected virtual bool ActivateCondition()
    {
        return hoverSender.hovered && hoverSender.isClosestHandHolding() && !AllSpheres["color"].active;
    }

    protected virtual void OnCountdownFinished() 
    {
        if (cachedHeldShapes.Count > 0)
        {
            foreach (var item in cachedHeldShapes)
            {
                Game.Instance.CreateShape(
                    item.ShapeId, 0, item.colorID, 
                    item.transform.position.x,
                    item.transform.position.y,
                    item.transform.position.z
                    );
            }
            SetAlpha(targetOpacity);
        }
        //repeatThreshhold = 0.64f;
        //cycledOnce = true;
        Debug.Log("Stop: make clone");
        AllSpheres[sphereName].active = false;
    }

    protected virtual void OnCountdownAbort(string location) 
    {
        Debug.Log("Stop: no clone " + location);
        SetAlpha(targetOpacity);
        AllSpheres[sphereName].active = false;
    }

    public float actionThreshhold = 0.25f;
    IEnumerator Shrink(float time)
    {
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / time)
        {
            highlightSphere.localScale = Vector3.Lerp(highlightSphere.localScale, originalScale * 2f, t);
            ShrinkAlpha(0, t);

            if (t > actionThreshhold && coroutine != null)
            {
                OnCountdownFinished();
                StopCoroutine(coroutine);
                coroutine = null;
            }
            else if (!hoverSender.hovered && coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
                OnCountdownAbort("hover distance");
            }
            else
            {
                yield return null;
            }
        }
    }

    protected virtual void ShrinkAlpha(float target, float t)
    {
        float currentAlpha = meshRenderer.sharedMaterial.GetFloat("_Alpha");
        currentAlpha = Mathf.Lerp(currentAlpha, target, t);
        meshRenderer.sharedMaterial.SetFloat("_Alpha", currentAlpha);
    }

    protected HashSet<Shape> cachedHeldShapes = new HashSet<Shape>();
    protected override void OnHoldingBegin()
    {
        SetAlpha(targetOpacity);
        if (Game.Instance.HeldShapes.Count > 0)
        {
            foreach (var item in Game.Instance.HeldShapes)
            {
                cachedHeldShapes.Add(item);
            }
        }
    }

    protected override void OnHoldingEnd()
    {
        SetAlpha(startOpacity);
        AllSpheres[sphereName].active = false;
        //repeatThreshhold = 0.25f;
        cachedHeldShapes.Clear();
    }

    protected override void OnHoverEnd()
    {
        AllSpheres[sphereName].active = false;
        cycledOnce = false;
    }

    protected virtual void SetAlpha(float to, float t = 0.15f)
    {
        if (meshRenderer.material.HasProperty("_Alpha"))
        {
            float currentAlpha = meshRenderer.sharedMaterial.GetFloat("_Alpha");
            Tween tween = DOTween.To(() => { return currentAlpha; }, x => { currentAlpha = x; }, to, t);
            tween.OnUpdate(() =>
            {
                meshRenderer.sharedMaterial.SetFloat("_Alpha", currentAlpha);
            });
        }
    }

}
