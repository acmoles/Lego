
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UnparentLego : MonoBehaviour
{
    [MenuItem("Tools/Lego/Clean Lego Brick")]
    static void RemoveParent()
    {
        foreach (GameObject rootGameObject in Selection.gameObjects)
        {
            //Transform lvl1 = rootGameObject.transform.GetChild(0);
            Transform lvl1 = rootGameObject.transform;
            lvl1.parent = null;

            if (lvl1.name == "Main model")
            {
                Transform lvl2 = lvl1.GetChild(0);
                lvl2.parent = null;
                Transform lvl3 = lvl2.GetChild(0);
                lvl3.parent = null;

                Transform toDelete = lvl3.Find("DecorationSurfaces");
                if (toDelete)
                {
                    DestroyImmediate(toDelete.gameObject);
                }
                DestroyImmediate(lvl2.gameObject);
            }

            DestroyImmediate(lvl1.gameObject);
            DestroyImmediate(rootGameObject);
        }
    }

    public static void ChangeMaterial()
    {

    }

}
