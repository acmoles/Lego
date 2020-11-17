using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity.Interaction;
using Leap.Unity;

[RequireComponent(typeof(InteractionBehaviour))]
public class SetInteractionBehaviour : MonoBehaviour
{
    public InteractionBehaviour interactionBehaviour;

    void Start()
    {
        //if (TryGetComponent(out InteractionBehaviour ib))
        //{
        //    interactionBehaviour.manager = Game.Instance.manager;
        //} else
        //{
        //    Debug.Log("Couldn't get interaction behaviour");
        //}
        if (interactionBehaviour) interactionBehaviour.manager = Game.Instance.manager;
        else Debug.Log("Missing interaction behaviour: " + gameObject.name);
    }
}
