using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class UpdatePrefabMeshes
{

    [MenuItem("Tools/Lego/Update Prefab Meshes")]
    static public void Go()
    {
        Dictionary<string, GameObject> modelsToUse = Populate();
        Debug.Log("Populated? " + modelsToUse.Count);
        const string path = "Assets/Prefabs/Lego/";

        var info = new DirectoryInfo(path);
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
        {
            if (file.Name.EndsWith("b"))
            {
                using (var editScope = new EditPrefabAssetScope(path + file.Name))
                {
                    Edit(editScope.prefabRoot, modelsToUse);
                }
                //GameObject go = AssetDatabase.LoadAssetAtPath(path + file.Name, typeof(GameObject)) as GameObject;
                //var localPath = "Assets/Prefabs/Lego/Updated/" + go.name + ".prefab";
                //GameObject prefabVariant = PrefabUtility.SaveAsPrefabAsset(go, localPath);
                //EditorUtility.SetDirty(go);
                //PrefabUtility.SavePrefabAsset(go);
            }
        }
            

    }

    static GameObject Edit(GameObject go, Dictionary<string, GameObject> modelsToUse)
    {
        // Got set anyway?
        //Material mat = AssetDatabase.LoadAssetAtPath("Assets/Materials/InstancedObjects.mat", typeof(Material)) as Material;

        Debug.Log("Current prefab: " + go.name);

        // Apply all knobs
        Transform knobsParent = go.transform.Find(go.name + "/Knobs");
        if (knobsParent)
        {
            foreach (Transform knob in knobsParent)
            {
                GameObject loadedKnob = modelsToUse[knob.name];
                knob.GetComponent<MeshFilter>().sharedMesh = loadedKnob.GetComponent<MeshFilter>().sharedMesh;
            }
        }

        // Apply shell
        Transform myShell = go.transform.Find(go.name + "/Shell");
        if (myShell)
        {
            GameObject loadedShell = modelsToUse[go.name].transform.Find("Shell").gameObject;
            myShell.GetComponent<MeshFilter>().sharedMesh = loadedShell.GetComponent<MeshFilter>().sharedMesh;
            Debug.Log("Change shell: " + go.name + " - " + loadedShell.name);
        }

        // Apply all pins or tubes
        Transform tubesParent = go.transform.Find(go.name + "/Tubes");
        if (tubesParent)
        {
            foreach (Transform tube in tubesParent)
            {
                GameObject loadedTube = modelsToUse[tube.name];
                tube.GetComponent<MeshFilter>().sharedMesh = loadedTube.GetComponent<MeshFilter>().sharedMesh;
            }
        }
        return go;
    }

    static Dictionary<string, GameObject> Populate()
    {
        const string modelPath = "Assets/LEGO Data/Geometry/New/LOD0/";
        const string commonPath = "Assets/LEGO Data/Geometry/CommonParts/LOD0/";

        Dictionary<string, GameObject> modelsToLoad = new Dictionary<string, GameObject>();

        var info = new DirectoryInfo(modelPath);
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
        {
            if (file.Name.EndsWith("x"))
            {
                Debug.Log("Current path: " + modelPath + file.Name);
                GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(modelPath + file.Name, typeof(GameObject));
                modelsToLoad[go.name] = go;
            }
        }

        var info2 = new DirectoryInfo(commonPath);
        var fileInfo2 = info2.GetFiles();
        foreach (var file in fileInfo2)
        {
            if (file.Name.EndsWith("x"))
            {
                Debug.Log("Current path: " + commonPath + file.Name );
                GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(commonPath + file.Name, typeof(GameObject));
                modelsToLoad[go.name] = go;
            }
        }

        return modelsToLoad;
    }

}

public class EditPrefabAssetScope : IDisposable
{

    public readonly string assetPath;
    public readonly GameObject prefabRoot;

    public EditPrefabAssetScope(string assetPath)
    {
        this.assetPath = assetPath;
        prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
    }

    public void Dispose()
    {
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
