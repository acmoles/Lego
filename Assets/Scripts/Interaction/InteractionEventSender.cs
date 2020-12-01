using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class InteractionEventSender : MonoBehaviour
{
    public event Action OnHoverBegin, OnHoverEnd, OnPinchBegin, OnPinchEnd, OnHoldingBegin, OnHoldingEnd, OnHoldingSustain, OnTouch, OnRelease;

    // Events prefixed by _ are fired by the sender subclass
    protected virtual void _OnHoverBegin()
    {
        if (OnHoverBegin != null) OnHoverBegin();
    }

    protected virtual void _OnHoverEnd()
    {
        if (OnHoverEnd != null) OnHoverEnd();
    }

    protected virtual void _OnPinchBegin()
    {
        if (OnPinchBegin != null) OnPinchBegin();
    }

    protected virtual void _OnPinchEnd()
    {
        if (OnPinchEnd != null) OnPinchEnd();
    }

    protected virtual void _OnHoldingBegin()
    {
        if (OnHoldingBegin != null) OnHoldingBegin();
    }

    protected virtual void _OnHoldingEnd()
    {
        if (OnHoldingEnd != null) OnHoldingEnd();
    }

    protected virtual void _OnHoldingSustain()
    {
        if (OnHoldingSustain != null) OnHoldingSustain();
    }

    protected virtual void _OnTouch()
    {
        if (OnTouch != null) OnTouch();
    }

    protected virtual void _OnRelease()
    {
        if (OnRelease != null) OnRelease();
    }

    
}
