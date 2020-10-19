using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventTest : MonoBehaviour
{
    public UnityEvent OnTestEvent = new UnityEvent();

    public Action<string, int> NamedActionDelegate;

    // Start is called before the first frame update
    void Start()
    {
        OnTestEvent.Invoke();
        NamedActionDelegate = NamedMethod;
        NamedActionDelegate += OtherNamedMethod;
        NamedActionDelegate.Invoke("Hi", 5);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NamedMethod(string text, int digit)
    {
        Debug.Log("Named said: " + text + digit);
    }

    public void OtherNamedMethod(string text, int digit)
    {
        Debug.Log("Other named said: " + text + digit);
    }
}
