using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurntableMember : MonoBehaviour
{
    public bool nonMember = false;
    public bool turntableTrigger = false;
    public bool isTouchingTurntable = false;
    private GameObject TurntableMain;

    private void Awake()
    {
        TurntableMain = GameObject.Find("Main");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Don't reparent base
        if (gameObject.tag == "TurntableBase") return;

        if (collision.gameObject.TryGetComponent(out TurntableMember member))
        {
            if (member.nonMember)
            {
                RemoveFromTurntable();
                return;
                //Debug.Log(gameObject.name + " found non member: " + member.name);
            }

            if (TurntableMain == null) return;

            // Don't change parent unless hitting base trigger or already touching member

            //GameObject baseParent = LegoStaticUtils.FindParentWithName(member.gameObject, "Main");
            //baseParent != null ||
            if (member.turntableTrigger || member.isTouchingTurntable)
            {
                //Debug.Log(gameObject.name + " hit member: " + member.name + " - " + member.turntableTrigger + member.isTouchingTurntable);
                AddToTurntable();
            }
        }
    }

    public void AddToTurntable()
    {
        transform.parent = TurntableMain.transform;
        isTouchingTurntable = true;
    }

    public void RemoveFromTurntable()
    {
        transform.parent = null;
        isTouchingTurntable = false;
    }

}
