using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;

public class GestureEvents : MonoBehaviour
{
    public static bool shakaL;
    public static bool shakaR;
    public static bool shakaShown;

    public static bool thumbL;
    public static bool thumbR;
    public static bool thumbShown;

    public static Transform head;
    public static InteractionHand handRight, handLeft;

    void Awake()
    {
        head = Hands.Provider.transform;

        var interactionHands = FindObjectsOfType<InteractionHand>();
        if (interactionHands.Length > 1)
            foreach (var item in interactionHands)
                if (item.isLeft)
                    handLeft = item;
                else
                    handRight = item;
        else
            Debug.Log("Both left and right InteractionHands are required in the scene", this);
    }

    // Update is called once per frame
    void Update()
    {
        //shakaL = ShakaIsShown(handLeft);
        //shakaR = ShakaIsShown(handRight);
        //shakaShown = shakaL || shakaR;

        thumbL = thumbIsShown(handLeft);
        thumbR = thumbIsShown(handRight);
        thumbShown = thumbL || thumbR;

        if (thumbShown) onThumbShown();
    }

    private bool ShakaIsShown(InteractionHand hand)
    {
        return hand.isTracked && hand.leapHand.GetThumb().IsExtended && hand.leapHand.GetPinky().IsExtended &&
               !hand.leapHand.GetIndex().IsExtended && !hand.leapHand.GetMiddle().IsExtended &&
               !hand.leapHand.GetRing().IsExtended;
    }

    private bool thumbIsShown(InteractionHand hand)
    {
        return hand.isTracked && hand.leapHand.GetThumb().IsExtended && !hand.leapHand.GetPinky().IsExtended &&
               !hand.leapHand.GetIndex().IsExtended && !hand.leapHand.GetMiddle().IsExtended &&
               !hand.leapHand.GetRing().IsExtended;
    }

    private void onThumbShown()
    {
        Debug.Log("Thumbs up shown");
    }
}
