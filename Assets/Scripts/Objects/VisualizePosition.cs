using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizePosition : ScriptableObject
{
    //GameObject target;
    //Vector3 targetsLocalPosition;

    public static void Create(GameObject target, Vector3 targetsLocalPosition, float size)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.GetComponent<Collider>().enabled = false;
        sphere.GetComponent<MeshRenderer>().material.color = new Color(0.71f, 0.80f, 0.49f);
        sphere.transform.localScale = new Vector3(size, size, size);
        sphere.transform.position = targetsLocalPosition;
        if (target)
        {
            sphere.transform.parent = target.transform;
        }

        //var vp = sphere.AddComponent<VisualizePosition>();
        //vp.target = target;
        //vp.targetsLocalPosition = targetsLocalPosition;

    }

    //void Update()
    //{
    //    transform.position = target.transform.position + (target.transform.rotation * targetsLocalPosition);
    //}

}

