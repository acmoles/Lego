using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurntableLights : InteractionEventReceiver
{
    protected override void OnTouch()
    {
        SetOpacity(targetOpacity);
    }

    protected override void OnRelease()
    {
        SetOpacity(startOpacity);
    }

}
