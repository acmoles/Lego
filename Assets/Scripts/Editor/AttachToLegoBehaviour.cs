using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AttachToLegoBehaviour : MonoBehaviour
{
    [MenuItem("Tools/Lego/Attach Lego Behaviour")]
    static void RemoveParent()
    {
        foreach (GameObject rootGameObject in Selection.gameObjects)
        {
            var prefabPath = "Lego base";
            Object originalPrefab = (GameObject)Resources.Load(prefabPath, typeof(GameObject));
            GameObject objSource = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;
            Debug.Log(objSource);
            rootGameObject.transform.parent = objSource.transform;
            var localPath = "Assets/Prefabs/Lego/" + rootGameObject.name + ".prefab";
            GameObject prefabVariant = PrefabUtility.SaveAsPrefabAsset(objSource, localPath);
        }
    }
}
