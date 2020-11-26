using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonLights : InteractionEventReceiver
{
    protected override void OnHoverBegin()
    {
        SetOpacity(targetOpacity);
        Debug.Log("Hover begin");
    }

    protected override void OnHoverEnd()
    {
        SetOpacity(startOpacity);
        Debug.Log("Hover end");
    }

    protected override void OnTouch()
    {
        Debug.Log("Button touch start");
    }

    protected override void OnRelease()
    {
        Debug.Log("Button touch release");
    }
}
