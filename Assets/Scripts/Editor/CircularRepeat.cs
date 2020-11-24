using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CircularRepeat : ScriptableWizard
{
    public int copies = 7;

    [MenuItem("Tools/Lego/Circular Repeat")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<CircularRepeat>("Repeat circle", "Go");
    }

    void OnWizardCreate()
    {
        Repeater(copies);
    }

    void OnWizardUpdate()
    {
        helpString = "Please set copy count.";
    }

    static public void Repeater(int copies)
    {

    GameObject[] selectedObjects = Selection.gameObjects;

    foreach (GameObject selected_object in selectedObjects)
    {
        Undo.RegisterCompleteObjectUndo(selected_object.transform, "Rotate Repeat Objects");

        float angle = 0f;

        for (int i = 0; i < copies; i++)
        {
            angle += (360f / (copies + 1));
            var a = Quaternion.AngleAxis(angle, Vector3.up);
            GameObject clone = Instantiate(selected_object, Vector3.zero, a);
            clone.transform.SetParent(selected_object.transform.parent, false);

            clone.name = "Ring" + (i + 2);
            var inner = clone.transform.GetChild(0);
            inner.name = "Ring" + (i + 2) + "-Inner";
            var outer = clone.transform.GetChild(1);
            TurntableMember to = outer.gameObject.GetComponent<TurntableMember>() as TurntableMember;
            to.nonMember = true;
            outer.name = "Ring" + (i + 2) + "-Outer";
            }
      }
    }

}
