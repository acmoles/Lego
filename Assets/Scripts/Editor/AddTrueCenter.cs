using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class AddTrueCenter
{
    [MenuItem("Tools/Lego/Add true center")]
    static public void Go()
    {
        const string path = "Assets/Prefabs/Lego/Extra/";
        //const string pathExtra = "Assets/Prefabs/Lego/Extra/";

        var info = new DirectoryInfo(path);
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
        {
            if (file.Name.EndsWith("b"))
            {
                using (var editScope = new EditPrefabAssetScope(path + file.Name))
                {
                    Edit(editScope.prefabRoot);
                }
            }
        }
    }

    static void Edit(GameObject go)
    {
        Debug.Log("Current prefab: " + go.name);

        Transform myShell = go.transform.Find(go.name + "/Shell");
        if (myShell)
        {
            Mesh shellMesh = myShell.GetComponent<MeshFilter>().sharedMesh;
            Bounds bounds = shellMesh.bounds;
            //Vector3 center = go.transform.GetChild(0).TransformPoint(bounds.center) - go.transform.position;
            GameObject centerGo = new GameObject("TrueCenter");
            centerGo.transform.parent = go.transform.GetChild(0);
            centerGo.transform.localPosition = bounds.center;
        }


       

    }
}
