
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UnparentLego : MonoBehaviour
{
    [MenuItem("Tools/Lego/Remove Parent 2lvl")]
    static void RemoveParent()
    {
        foreach (GameObject rootGameObject in Selection.gameObjects)
        {
            Transform lvl1 = rootGameObject.transform.GetChild(0);
            lvl1.parent = null;
            Transform lvl2 = lvl1.GetChild(0);
            lvl2.parent = null;

            DestroyImmediate(lvl1.gameObject);
            DestroyImmediate(rootGameObject);
        }
    }

}
