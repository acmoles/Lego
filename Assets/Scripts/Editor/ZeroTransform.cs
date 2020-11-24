using UnityEngine;
using UnityEditor;

public class ResetGameObjectPosition
{
    [MenuItem("GameObject/Reset Transform #0")]
    static public void ResetTransformOne()
    {
        ResetTransform(1.0f);
    }

    [MenuItem("GameObject/Reset Transform 0.1 #1")]
    static public void ResetTransformSmall()
    {
        ResetTransform(0.1f);
    }

    [MenuItem("GameObject/Reset Transform 0.05 #5")]
    static public void ResetTransformSmaller()
    {
        ResetTransform(0.05f);
    }

    static public void ResetTransform(float factor)
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        foreach (GameObject selected_object in selectedObjects)
        {
            Undo.RegisterCompleteObjectUndo(selected_object.transform, "Reset game object to origin");

            Vector3 p_pos = Vector3.zero;
            Quaternion p_rot = Quaternion.identity;
            Vector3 p_scale = Vector3.one;

            if (selected_object.transform.parent != null)
            {
                p_pos = selected_object.transform.parent.position;
                p_rot = selected_object.transform.parent.rotation;
                p_scale = selected_object.transform.parent.localScale;
            }

            selected_object.transform.position = Vector3.zero + p_pos;
            selected_object.transform.rotation = Quaternion.identity * p_rot;
            selected_object.transform.localScale = Vector3.one * factor;
        }
    }
}
