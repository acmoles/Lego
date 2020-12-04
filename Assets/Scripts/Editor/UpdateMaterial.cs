using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class UpdateMaterial
{

    [MenuItem("Tools/Lego/Update Prefab Materials")]
    static public void Go()
    {
        const string path = "Assets/Prefabs/Lego/Extra/";
        const string materialPath = "Assets/Materials/InstancedObjects.mat";

        Material material = AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)) as Material;

        var info = new DirectoryInfo(path);
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
        {
            if (file.Name.EndsWith("b"))
            {
                using (var editScope = new EditPrefabAssetScope(path + file.Name))
                {
                    Edit(editScope.prefabRoot, material);
                }
            }
        }
    }

    static GameObject Edit(GameObject go, Material material)
    {
        Debug.Log("Current prefab: " + go.name);

        // Apply all knobs
        Transform knobsParent = go.transform.Find(go.name + "/Knobs");
        if (knobsParent)
        {
            foreach (Transform knob in knobsParent)
            {
                knob.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        // Apply shell
        Transform myShell = go.transform.Find(go.name + "/Shell");
        if (myShell)
        {
            myShell.GetComponent<MeshRenderer>().sharedMaterial = material;
            Debug.Log("Change shell material: " + go.name);
        }

        // Apply all pins or tubes
        Transform tubesParent = go.transform.Find(go.name + "/Tubes");
        if (tubesParent)
        {
            foreach (Transform tube in tubesParent)
            {
                tube.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }
        return go;
    }

}
