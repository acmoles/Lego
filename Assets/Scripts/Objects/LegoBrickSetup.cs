using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LegoBrickSetup : MonoBehaviour
{
    public bool basePlate = false;
    private bool setupComplete = false;

    public List<LocalPosition> knobs;
    public List<LocalPosition> slots;

    private void OnEnable()
    {
        setupComplete = false;
        Awake();
    }

    void Awake()
    {
        Debug.Log("Init " + gameObject.name);
        knobs = new List<LocalPosition>();
        slots = new List<LocalPosition>();
        PopulateConnections();
    }

    void PopulateConnections()
    {
        Transform connectivity = transform.GetChild(0).Find("Connectivity");

        if (connectivity)
        {
            // Find knobs and slots
            foreach (Transform field in connectivity)
            {
                if (field.GetChild(0).name == "knob")
                {
                    Transform knobsList = field;
                    foreach (Transform child in knobsList)
                    {
                        LocalPosition knob = new LocalPosition();
                        knob.position = child.localPosition + knobsList.localPosition;
                        //knob.position = Quaternion.Inverse(knobsList.rotation) * knob.position;
                        Debug.LogFormat("Knob at: {0}", knob.position);
                        knobs.Add(knob);
                    }
                } else
                {
                    Transform slotsList = field;
                    foreach (Transform child in slotsList)
                    {
                        LocalPosition slot = new LocalPosition();
                        slot.position = child.localPosition + slotsList.localPosition;
                        //slot.position = Quaternion.Inverse(slotsList.rotation) * slot.position;
                        Debug.LogFormat("Slot at: {0}", slot.position);
                        slots.Add(slot);
                    }
                }
            }
        }
        else
        {
            Debug.Log("missing connectivity");
            Debug.Log(connectivity);
        }

        Debug.Log("Slot count: " + slots.Count);
        Debug.Log("Knobs count: " + knobs.Count);
        setupComplete = true;
    }

    private void OnDrawGizmos()
    {
        //Collider col = GetComponent<Collider>();
        //Gizmos.DrawWireCube(col.bounds.center, col.bounds.extents);
        if (setupComplete)
        {
            Gizmos.color = Color.green;
            foreach (var item in slots)
            {
                Gizmos.DrawLine(Vector3.zero, transform.TransformPoint(item.position));
            }
            Gizmos.color = Color.red;
            foreach (var item in knobs)
            {
                Gizmos.DrawLine(Vector3.zero, transform.TransformPoint(item.position));
            }
        }

    }


}
