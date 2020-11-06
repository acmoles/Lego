using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RemoveComponentNameEditor : MonoBehaviour
{
	[MenuItem("Tools/Lego/Remove All Components")]
	public static void RemoveComponents()
	{
		foreach (GameObject rootGameObject in Selection.gameObjects)
		{
			Component[] components = rootGameObject.GetComponentsInChildren(typeof(MonoBehaviour), true);
			foreach (var c in components)
			{
                if (c is MeshFilter || c is MeshRenderer)
                {
					continue;
                }
				DestroyImmediate(c);
			}
		}

	}

}