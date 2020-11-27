using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using System;

[SelectionBase]
public class Dial : InteractionEventHoverSender
{
    private Quaternion originalRotation;
    private Quaternion startPinchRotationLeft = Quaternion.identity;
    private Quaternion startPinchRotationRight = Quaternion.identity;

    private Transform control;
    private Transform selectables;
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        control = transform.Find("Control");
        originalRotation = control.rotation;

        selectables = transform.Find("Selectables");

        CreatePositions();
        DialTo(LegoColors.Id.BrightYellow);
    }

    Dictionary<LegoColors.Id, GameObject> positionsList;
    public Material SphereMaterial;
    public float sensitivity;
    public float dialValue = 0;

    public float radius = 1.0f;
    public float arcWidth = 360f;
    public float sphereSize = 0.005f;
    public float selectedSize = 0.02f;
    LegoColors.Id activeColor;
    void CreatePositions()
    {
        positionsList = new Dictionary<LegoColors.Id, GameObject>();

        float x;
        float y = 0f;
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
            SetColor(colorID, renderer);

            sphere.transform.localScale = new Vector3(sphereSize, sphereSize, sphereSize);
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

    private void OnDrawGizmos()
    {
        if (rotationHeld)
        {
            foreach (var item in positionsList)
            {
                Vector3 pos = item.Value.transform.position;
                Debug.DrawLine(transform.position, pos, Color.green);
            }

            LegoColors.Id active = ClosestPosition(control.forward);

            Utils.DrawCircle(positionsList[active].transform.position, Vector3.up, 0.02f, Color.red);

        }
    }

    private void drawSpheres()
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

    private bool rotationHeld = false;
    protected override void Update()
    {
        base.Update();

        //drawSpheres();

        if (pinchDetectorLeft.IsActive && hovered)
        {
            if (!rotationHeld)
            {
                rotationHeld = true;
                startPinchRotationLeft = pinchDetectorLeft.Rotation;
                return;
            }
            RotateDial(pinchDetectorLeft, startPinchRotationLeft);
        }
        else if (pinchDetectorRight.IsActive && hovered)
        {
            if (!rotationHeld)
            {
                rotationHeld = true;
                startPinchRotationRight = pinchDetectorRight.Rotation;
                return;
            }
            RotateDial(pinchDetectorRight, startPinchRotationRight);
        }
        else if (rotationHeld)
        {
            rotationHeld = false;
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
    }

    private void DialTo(LegoColors.Id colorID)
    {
        activeColor = colorID;
        Vector3 l = positionsList[colorID].transform.localPosition;
        control.rotation = Quaternion.LookRotation(l, Vector3.up);
        originalRotation = control.rotation;
    }
}
