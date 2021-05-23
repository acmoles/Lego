using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class PositionAtBoundsCenter
{
    [MenuItem("Tools/Lego/Position at Bounds Center")]
    public static void PositionAtBounds()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Debug.Log("Object: " + go.name);

            Transform myShell = go.transform.Find(go.name + "/Shell");

            // Change to whatever gameobject to position
            Transform myTarget = go.transform.Find(go.name + "/TrueCenter");


            if (myShell && myTarget)
            {
                Mesh shellMesh = myShell.GetComponent<MeshFilter>().sharedMesh;
                Bounds bounds = shellMesh.bounds;
                myTarget.localPosition = bounds.center;
            }



        }

    }
}

