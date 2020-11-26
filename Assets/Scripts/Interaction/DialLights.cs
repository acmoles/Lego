using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialLights : InteractionEventReceiver
{
    protected override void OnHoverBegin()
    {
        SetOpacity(targetOpacity);
        //Debug.Log("Hover begin");
    }

    protected override void OnHoverEnd()
    {
        SetOpacity(startOpacity);
        //Debug.Log("Hover end");
    }

    protected override void OnPinchBegin()
    {
        //SetOpacity(1.0f);
        //Debug.Log("Pinch begin");
    }

    protected override void OnPinchEnd()
    {
        //SetOpacity(startOpacity);
        //Debug.Log("Pinch end");
    }
}
