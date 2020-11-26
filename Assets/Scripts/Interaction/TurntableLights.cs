using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TurntableLights : MonoBehaviour
{
    public Turntable turntable;
    public MeshRenderer renderer;

    private float startOpacity;
    public float targetOpacity = 0.8f;

    // Start is called before the first frame update
    void Start()
    {
        turntable.OnTouch += OnTouch;
        turntable.OnRelease += OnRelease;

        startOpacity = renderer.material.color.a;
    }

    private void OnTouch()
    {
        Debug.Log("Touch turntable");
        SetOpacity(targetOpacity);
    }

    private void OnRelease()
    {
        Debug.Log("Release turntable");
        SetOpacity(startOpacity);
    }

    private void SetOpacity(float to, float t = 0.15f)
    {
        Color c = Color.white;
        c.a = to;

        if (renderer.material.HasProperty("_Color"))
        {
            renderer.material.DOColor(c, t);
            Debug.Log("Got _color");
        } else
        {
            renderer.material.SetColor("_TintColor", c);
            Debug.Log("Fallback to tint");
        }
    }

}
