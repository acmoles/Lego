using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapHandInit : MonoBehaviour
{
    void Start()
    {

    }

    [ContextMenu("Init Hands")]
    public void Init()
    {
        RiggedHand_L = Hand_L.gameObject.AddComponent<RiggedHand>();
        RiggedHand_L.SetupRiggedHand();
        RiggedHand_L.StoreJointsStartPose();
    }
}
