using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SizeLegoCollider : MonoBehaviour
{
    static void FitToShell(int inflate = 0)
    {
        foreach (GameObject rootGameObject in Selection.gameObjects)
        {
            if (!(rootGameObject.GetComponent<Collider>() is BoxCollider))
                continue;

            Transform shell = rootGameObject.transform.GetChild(0).Find("Shell");
            Mesh shellMesh = shell.GetComponent<MeshFilter>().sharedMesh;
            Bounds bounds = shellMesh.bounds;

            if (inflate == 1)
            {
                bounds.size *= 1.2f;
            }
            else if (inflate == 2)
            {
                bounds.size *= 1.4f;
            }

            BoxCollider collider = rootGameObject.GetComponent<BoxCollider>();
            collider.center = bounds.center - rootGameObject.transform.position;
            collider.size = bounds.size;
        }
    }

    [MenuItem("Tools/Lego/Fit collider to shell")]
    static void FitInflate()
    {
        FitToShell();
    }

    [MenuItem("Tools/Lego/Fit collider to shell Inflate 1")]
    static void FitInflate1()
    {
        FitToShell(1);
    }

    [MenuItem("Tools/Lego/Fit collider to shell Inflate 2")]
    static void FitInflate2()
    {
        FitToShell(2);
    }
}
