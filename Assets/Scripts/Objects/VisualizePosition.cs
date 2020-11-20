using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizePosition : ScriptableObject
{
    //GameObject target;
    //Vector3 targetsLocalPosition;
    public static List<GameObject> spheres = new List<GameObject>();

    public static void Create(GameObject target, Vector3 targetsLocalPosition, float size)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.GetComponent<Collider>().enabled = false;

        Material SphereMaterial = Resources.Load<Material>("Lime");

        sphere.GetComponent<MeshRenderer>().material = SphereMaterial;
        sphere.transform.localScale = new Vector3(size, size, size);
        sphere.transform.position = targetsLocalPosition;
        if (target)
        {
            sphere.transform.parent = target.transform;
        }
        spheres.Add(sphere);

        //var vp = sphere.AddComponent<VisualizePosition>();
        //vp.target = target;
        //vp.targetsLocalPosition = targetsLocalPosition;

    }

    //void Update()
    //{
    //    transform.position = target.transform.position + (target.transform.rotation * targetsLocalPosition);
    //}

}

