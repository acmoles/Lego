using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connectivity
{
    public Vector3 position;
    public bool available = true;
    public Vector3 directionUp;
    public Quaternion rotation;
}

public static class LegoStaticUtils
{
    public static Connectivity FindClosestPosition(Vector3 targetPosition, List<Connectivity> listOfPositions)
    {
        Connectivity preferredPosition = null;
        float closestDistSqrd = float.PositiveInfinity;
        foreach (var gp in listOfPositions)
        {
            float testDistanceSqrd = (gp.position - targetPosition).sqrMagnitude;
            if (testDistanceSqrd < closestDistSqrd && gp.available)
            {
                preferredPosition = gp;
                closestDistSqrd = testDistanceSqrd;
            }
        }
        return preferredPosition;
    }

    public static Vector3 ClosestLegoLocalDirection(Vector3 worldDirection, Transform localTransform, Quaternion knobRotation)
    {
        //Vector3[] compass = { localTransform.right, -localTransform.right, localTransform.forward, -localTransform.forward };

        // Not up/down since lego plane
        Vector3[] compass = { Vector3.right, -Vector3.right, Vector3.forward, -Vector3.forward };

        var maxDot = -Mathf.Infinity;
        var ret = Vector3.zero;

        foreach (Vector3 dir in compass)
        {

            var rotatedDir = knobRotation * dir;
            rotatedDir = localTransform.TransformDirection(rotatedDir);


            var t = Vector3.Dot(worldDirection, rotatedDir);
            
            if (t > maxDot)
            {
                ret = rotatedDir;
                maxDot = t;
            }
        }

        return ret;

        // && rotatedDir != worldUpVector && rotatedDir != -worldUpVector
    }

    public static Vector3 ClosestWorldAxis(Vector3 v)
    {
        if (Mathf.Abs(v.x) < Mathf.Abs(v.y))
        {
            v.x = 0;
            if (Mathf.Abs(v.y) < Mathf.Abs(v.z))
                v.y = 0;
            else
                v.z = 0;
        }
        else
        {
            v.y = 0;
            if (Mathf.Abs(v.x) < Mathf.Abs(v.z))
                v.x = 0;
            else
                v.z = 0;
        }

        return v;
    }

    public static Vector3 ClosestLegoWorldAxis(Vector3 v)
    {
        v.y = 0;
        if (Mathf.Abs(v.x) < Mathf.Abs(v.z))
            v.x = 0;
        else
            v.z = 0;

        return v;
    }

    /*
 * Check positions occupied (set?)
- Make two new Vec3 arrays, one for myPostionsWorld and one for otherPositionsWorld (known length so array)
- Loop through each my and other GridLocation lists, converting GridLocation.position to world (other or my inverse transform) and adding to corresonding array
- Now loop through first (my) list checking distance to all positions in second list (double loop)
- If the smallest distance is below a threshhold e.g. 0.01f then this point is marked unavailable, GridLocation[i].available = false
- Repeat for the second array
- HashSet could be faster? Have a look at brick check
 */
    public static void SetOccupiedGridPositions(LegoBrick me, LegoBrick other)
    {
        bool allOccupied = true;

        Vector3[] otherKnobs = new Vector3[other.setup.knobs.Count];
        Vector3 pos;
        for (int i = 0; i < other.setup.knobs.Count; i++)
        {
            pos = other.setup.knobs[i].position;
            pos = other.transform.TransformPoint(pos);
            otherKnobs[i] = pos;
            //
            if (other.setup.knobs[i].available)
                allOccupied = false;
        }
        Vector3[] mySlots = new Vector3[me.setup.slots.Count];
        for (int i = 0; i < me.setup.slots.Count; i++)
        {
            pos = me.setup.slots[i].position;
            pos = me.transform.TransformPoint(pos);
            mySlots[i] = pos;
        }

        for (int i = 0; i < otherKnobs.Length; i++)
        {
            for (int j = 0; j < mySlots.Length; j++)
            {
                float testDistance = (otherKnobs[i] - mySlots[j]).magnitude;
                
                if (testDistance < other.smallDistance)
                {
                    //Debug.Log("Distance: " + testDistance);
                    other.setup.knobs[i].available = false;
                    //VisualizePosition.Create(null, otherPoints[i], 0.01f);
                }
                else
                {
                    other.setup.knobs[i].available = true;
                }
            }
        }

        // Sets on subsequent tests i.e. in coroutine
        if (allOccupied)
            other.active = false;
        else
            other.active = true;
    }

    public static int FindLegoDepth(LegoBrick brick)
    {
        LegoBrick b = brick;
        int depth = 0;
        while (b != null)
        {
            depth++;
            b = b.connectedTo;
        }
        return depth;
    }

    public static GameObject FindParentWithTag(GameObject childObject, string tag)
    {
        Transform t = childObject.transform;
        while (t.parent != null)
        {
            if (t.parent.tag == tag)
            {
                return t.parent.gameObject;
            }
            t = t.parent.transform;
        }
        return null; // Could not find a parent with given tag.
    }

    public static GameObject FindParentWithName(GameObject childObject, string name)
    {
        Transform t = childObject.transform;
        while (t.parent != null)
        {
            if (t.parent.name == name)
            {
                return t.parent.gameObject;
            }
            t = t.parent.transform;
        }
        return null; // Could not find a parent with given name.
    }

}
