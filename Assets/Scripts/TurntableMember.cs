using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurntableMember : MonoBehaviour
{
    public bool nonMember = false;

    private void OnCollisionEnter(Collision collision)
    {
        // Don't reparent base
        if (gameObject.name == "Base") return;

        if (collision.gameObject.TryGetComponent(out TurntableMember member))
        {
            if (member.nonMember)
            {
                transform.parent = null;
                //Debug.Log(gameObject.name + " found non member: " + member.name);
            }

            // Don't change parent unless hitting base
            //if (collision.gameObject.name != "Base") return;
            GameObject baseParent = LegoStaticUtils.FindParentWithName(member.gameObject, "Main");
            if (baseParent != null)
            {
                //Debug.Log(gameObject.name + " found member: " + member.name + " with parent " + baseParent.name);
                transform.parent = baseParent.transform;
            }
        }
    }

}
