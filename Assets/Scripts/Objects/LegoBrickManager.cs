using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegoBrickManager : MonoBehaviour
{
    private static HashSet<LegoBrick> _allLegoBricks;
    public static HashSet<LegoBrick> allLegoBricks
    {
        get
        {
            if (_allLegoBricks == null)
            {
                _allLegoBricks = new HashSet<LegoBrick>();
            }
            return _allLegoBricks;
        }
    }
    public static LegoBrick lastLegoBrick;

    Color _color;
    public static Transform ghost;
    public static MeshRenderer ghostRenderer;
    public Material ghostMaterial;

    // For placing exactly after lerp.
    public static Vector3 _ghostPosition;
    public static Quaternion _ghostRotation;

    public static void MakeGhost(Transform toCopy)
    {
        if (ghost != null)
        {
            return;
        }
        ghost = Instantiate(toCopy);
        ghost.name = "Ghost";

        var list = ghost.GetComponents(typeof(Component));
        for (int i = 0; i < list.Length; i++)
        {
            Debug.Log(list[i].name);
        }

        ghostRenderer = ghost.GetComponent<MeshRenderer>();
        //ghostRenderer.material = ghostMaterial;
    }

    public static void DestroyGhost()
    {
        //if (ghost != null)
        //{
            _ghostPosition = ghost.transform.position;
            _ghostRotation = ghost.transform.rotation;
            Destroy(ghost.gameObject);
        //}
        //else
        //{
        //    Debug.LogWarning("Already destroyed ghost. " + ghost);
        //}
    }
}
