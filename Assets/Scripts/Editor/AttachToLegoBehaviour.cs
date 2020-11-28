using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AttachToLegoBehaviour : MonoBehaviour
{
    [MenuItem("Tools/Lego/Attach Lego Behaviour")]
    static void Process()
    {
        foreach (GameObject rootGameObject in Selection.gameObjects)
        {
            var prefabPath = "Lego base";
            Object originalPrefab = (GameObject)Resources.Load(prefabPath, typeof(GameObject));
            GameObject objSource = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;
            Debug.Log(objSource);
            rootGameObject.transform.position = Vector3.zero;
            rootGameObject.transform.rotation = Quaternion.identity;
            rootGameObject.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            rootGameObject.transform.parent = objSource.transform;

            Transform shell = rootGameObject.transform.Find("Shell");
            Mesh shellMesh = shell.GetComponent<MeshFilter>().sharedMesh;
            Bounds bounds = shellMesh.bounds;
            BoxCollider collider = objSource.GetComponent<BoxCollider>();
            collider.center = rootGameObject.transform.TransformPoint(bounds.center);
            collider.size = rootGameObject.transform.TransformPoint(bounds.size);

            var localPath = "Assets/Prefabs/Lego/Extra/" + rootGameObject.name + ".prefab";
            GameObject prefabVariant = PrefabUtility.SaveAsPrefabAsset(objSource, localPath);
        }
    }

}
