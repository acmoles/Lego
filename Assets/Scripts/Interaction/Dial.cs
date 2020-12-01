using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using System;
using DG.Tweening;

[SelectionBase]
public class Dial : InteractionEventHoverSender
{
    private Quaternion originalRotation;
    private Quaternion startPinchRotationLeft = Quaternion.identity;
    private Quaternion startPinchRotationRight = Quaternion.identity;

    private Transform control;
    private Transform selectables;

    protected override void Start()
    {
        base.Start();

        control = transform.Find("Control");
        originalRotation = control.rotation;

        selectables = transform.Find("Selectables");

        CreatePositions();
        DialTo(LegoColors.Id.BrightYellow);

        innerColorPropertyId = Shader.PropertyToID("_Color");
        rimColorPropertyId = Shader.PropertyToID("_RimColor");
    }

    Dictionary<LegoColors.Id, GameObject> positionsList;
    public Material SphereMaterial;
    public float sensitivity;
    public float dialValue = 0;

    public float radius = 1.0f;
    public float arcWidth = 360f;
    public float sphereScale = 0.005f;
    public float selectedScale = 0.02f;
    LegoColors.Id activeColor;
    void CreatePositions()
    {
        positionsList = new Dictionary<LegoColors.Id, GameObject>();

        float x;
        float y = restY;
        float z;

        float angle = 0f;
        foreach (var colorID in LegoColors.pickable)
        {
            z = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Collider>().enabled = false;

            var renderer = sphere.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = SphereMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetColor(colorID, renderer);

            sphere.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);
            sphere.transform.parent = selectables.transform;
            sphere.transform.localPosition = new Vector3(x, y, z);

            positionsList[colorID] = sphere;

            angle += (arcWidth / LegoColors.pickable.Count);
        }
    }

    [HideInInspector]
    public static int colorPropertyId = Shader.PropertyToID("_Color");
    [HideInInspector]
    public static int alphaPropertyId = Shader.PropertyToID("_Alpha");
    [HideInInspector]
    public static MaterialPropertyBlock sharedPropertyBlockAlpha;

    public void SetColor(LegoColors.Id colorID, MeshRenderer rendererToSet)
    {
        Color color = LegoColors.GetColour(colorID);

        if (sharedPropertyBlockAlpha == null)
        {
            sharedPropertyBlockAlpha = new MaterialPropertyBlock();
        }
        sharedPropertyBlockAlpha.SetColor(colorPropertyId, color);
        rendererToSet.SetPropertyBlock(sharedPropertyBlockAlpha);
    }

    int innerColorPropertyId;
    int rimColorPropertyId;
    public void SetShapeColor(LegoColors.Id colorID)
    {
        // Used for dial body - uses static material property block in Shape

        Color color = LegoColors.GetColour(colorID);

        if (Shape.sharedPropertyBlock == null)
        {
            Shape.sharedPropertyBlock = new MaterialPropertyBlock();
        }
        Shape.sharedPropertyBlock.SetColor(Shape.colorPropertyId, color);
        dialBottom.SetPropertyBlock(Shape.sharedPropertyBlock);

        dialTop.material.SetColor(innerColorPropertyId, color);
        dialTop.material.SetColor(rimColorPropertyId, color);
        //dialTop.SetPropertyBlock(Shape.sharedPropertyBlock);

        // Set color of held brick(s)
        if (Game.Instance.heldShapes.Count > 0)
        {
            foreach (var item in Game.Instance.heldShapes)
            {
                item.SetColor((int)colorID);
            }
        }
    }

    public void SetAlpha(float alpha)
    {
        SphereMaterial.SetFloat("_Alpha", alpha);
    }

    public float fadeUpTime = 0.4f;
    public float restY = -0.004f;
    Transform[] transformsToUpdate;
    public float fadeOutDelay = 2f;
    private IEnumerator fadeOutCoroutine;
    IEnumerator FadeUpPositions(bool incoming = false)
    {
        WaitForSeconds wait = new WaitForSeconds(fadeOutDelay);
        if (incoming)
        {
            wait = null;
        }
        yield return wait;
        _FadeUpPositions(incoming);
    }
    private bool positionsUp = false;
    void _FadeUpPositions(bool incoming = false)
    {
        TweenParams tParms = new TweenParams().SetEase(Ease.InExpo);
        float targetY = restY;
        float finalAlpha = 0f;
        float currentAlpha = 1f;
        if (incoming)
        {
            if (positionsUp) return;
            targetY = 0f;
            finalAlpha = 1f;
            currentAlpha = 0f;
            tParms.SetEase(Ease.InOutExpo);
        }

        transformsToUpdate = new Transform[positionsList.Count];
        int index = 0;
        foreach (var item in positionsList)
        {
            transformsToUpdate[index] = item.Value.transform;
            index++;
        }

        //Debug.Log("FadeUp " + incoming + " - to change " + transformsToUpdate.Length);

        Tween t = DOTween.To(() => { return currentAlpha; }, x => { currentAlpha = x; }, finalAlpha, fadeUpTime).SetAs(tParms);
        t.OnUpdate(() =>
        {
            TweenCallback(currentAlpha, targetY, incoming);
        }).OnComplete(() =>
        {
            if (incoming)
            {
                positionsUp = true;
            }
            else
            {
                positionsUp = false;
            }
            fadeOutCoroutine = null;
        });
    }

    public MeshRenderer dialBottom;
    public SkinnedMeshRenderer dialTop;
    void TweenCallback (float alpha, float targetY, bool incoming)
    {
        SetAlpha(alpha);
        dialTop.SetBlendShapeWeight(0, alpha * 100f);
        float t = incoming ? alpha : 1f - alpha;

        for (int i = 0; i < transformsToUpdate.Length; i++)
        {
            Vector3 vec = new Vector3(transformsToUpdate[i].localPosition.x, transformsToUpdate[i].localPosition.y, transformsToUpdate[i].localPosition.z);
            vec.y = Mathf.Lerp(transformsToUpdate[i].localPosition.y, targetY, t);
            transformsToUpdate[i].localPosition = vec;
        }
    }

    void Highlight (GameObject go)
    {
        go.transform.DOScale(selectedScale, fadeUpTime).SetEase(Ease.OutElastic);
    }

    void Unhighlight(GameObject go)
    {
        go.transform.DOScale(sphereScale, fadeUpTime);
    }

    //private void OnDrawGizmos()
    //{
    //    if (rotationHeld)
    //    {
    //        //foreach (var item in positionsList)
    //        //{
    //        //    Vector3 pos = item.Value.transform.position;
    //        //    Debug.DrawLine(transform.position, pos, Color.green);
    //        //}

    //        LegoColors.Id active = ClosestPosition(control.forward);

    //        Utils.DrawCircle(positionsList[active].transform.position, Vector3.up, selectedScale, Color.red);

    //    }
    //}

    private void drawTestSpheres()
    {

        foreach (var item in VisualizePosition.spheres)
        {
            Destroy(item);
        }
        VisualizePosition.spheres.Clear();

        if (rotationHeld)
        {
            foreach (var item in positionsList)
            {
                Vector3 pos = transform.TransformPoint(item.Value.transform.position);
                VisualizePosition.Create(this.gameObject, pos, 0.005f);
            }

            LegoColors.Id active = ClosestPosition(control.right);
            VisualizePosition.Create(this.gameObject, transform.TransformPoint(positionsList[active].transform.position), 0.02f);
        }
    }

    private LegoColors.Id ClosestPosition(Vector3 direction)
    {
        var maxDot = -Mathf.Infinity;
        LegoColors.Id key = 0;

        foreach (var item in positionsList)
        {
            var t = Vector3.Dot(direction, item.Value.transform.position);
            if (t > maxDot)
            {
                key = item.Key;
                maxDot = t;
            }
        }
        return key;
    }

    LegoColors.Id lastActiveColor = 0;
    private bool rotationHeld = false;
    protected override void Update()
    {
        base.Update();

        if (lastActiveColor != activeColor)
        {
            Highlight(positionsList[activeColor]);
            SetShapeColor(activeColor);
            if (lastActiveColor != 0) Unhighlight(positionsList[lastActiveColor]);
        }
        lastActiveColor = activeColor;

        //drawSpheres();

        if (closestHand == handLeft && isClosestPinching() && hovered)
        {
            if (!rotationHeld)
            {
                rotationHeld = true;
                if (fadeOutCoroutine != null) StopCoroutine(fadeOutCoroutine);
                _FadeUpPositions(true);
                startPinchRotationLeft = pinchDetectorLeft.Rotation;
                return;
            }
            RotateDial(pinchDetectorLeft, startPinchRotationLeft);
        }
        else if (closestHand == handRight && isClosestPinching() && hovered)
        {
            if (!rotationHeld)
            {
                rotationHeld = true;
                if (fadeOutCoroutine != null) StopCoroutine(fadeOutCoroutine);
                _FadeUpPositions(true);
                startPinchRotationRight = pinchDetectorRight.Rotation;
                return;
            }
            RotateDial(pinchDetectorRight, startPinchRotationRight);
        }
        else if (rotationHeld)
        {
            rotationHeld = false;
            if (fadeOutCoroutine != null) StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = FadeUpPositions(false);
            StartCoroutine(fadeOutCoroutine);
            originalRotation = control.rotation;
        }
    }

    private void RotateDial(PinchDetector singlePinch, Quaternion startPinchRotation)
    {
        Vector3 p = singlePinch.Rotation * Vector3.forward;
        p.y = 0;
        Quaternion currentRotation = Quaternion.LookRotation(p);

        Vector3 l = startPinchRotation * Vector3.forward;
        l.y = 0;
        Quaternion startRotation = Quaternion.LookRotation(l);

        Quaternion difference = currentRotation * Quaternion.Inverse(startRotation);

        Quaternion target = originalRotation * difference;

        control.rotation = Quaternion.Slerp(control.rotation, target, Time.deltaTime * 20f);

        // Update active color
        activeColor = ClosestPosition(control.forward);
    }

    private void DialTo(LegoColors.Id colorID)
    {
        activeColor = colorID;
        Vector3 l = positionsList[colorID].transform.localPosition;
        control.rotation = Quaternion.LookRotation(l, Vector3.up);
        originalRotation = control.rotation;
    }
}
