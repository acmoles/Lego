using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LegoBrickSetup : MonoBehaviour
{
    public bool basePlate = false;
    public bool report = false;
    private bool setupComplete = false;

    public List<Connectivity> knobs;
    public List<Connectivity> slots;

    private void OnEnable()
    {
        knobs = new List<Connectivity>();
        slots = new List<Connectivity>();
        PopulateConnections();
    }

    void PopulateConnections()
    {
        Transform connectivity = transform.GetChild(0).Find("Connectivity");

        if (connectivity)
        {
            string[] allowableKnob = { "knob", "hollowKnob", "hollowKnobFitInPegHole", "knobFitInPegHole" };
            string[] allowableSlot = { "antiKnob", "squareAntiKnob" };
            // Find knobs and slots
            foreach (Transform field in connectivity)
            {
                if (allowableKnob.Contains(field.GetChild(0).name))
                {
                    Transform knobsList = field;
                    foreach (Transform child in knobsList)
                    {
                        Connectivity knob = new Connectivity();

                        // Hack for rotated knobs
                        if (knobsList.rotation != Quaternion.identity)
                        {
                            Debug.Log("Applying world position to: " + gameObject.name);
                            knob.position = child.position;
                        }
                        else {
                            knob.position = knobsList.localPosition + child.localPosition;
                            // Prebake realistic scale
                            knob.position = transform.GetChild(0).TransformVector(knob.position);
                        }

                        knob.directionUp = knobsList.up;
                        if (knob.directionUp != Vector3.up)
                            //Debug.LogFormat("Knob direction for {0}: {1}", gameObject.name, knob.direction);

                        knob.rotation = knobsList.rotation;

                        if (report) Debug.LogFormat("Knob at: {0}", knob.position);
                        if (allowableKnob.Contains(child.name))
                        {
                            knobs.Add(knob);
                        }
                        
                    }
                } else
                {
                    Transform slotsList = field;
                    foreach (Transform child in slotsList)
                    {
                        Connectivity slot = new Connectivity();
                        slot.position = slotsList.localPosition + child.localPosition;
                        // Prebake realistic scale
                        slot.position = transform.GetChild(0).TransformVector(slot.position);

                        if (report) Debug.LogFormat("Slot at: {0}", slot.position);
                        if (allowableSlot.Contains(child.name))
                        {
                            slots.Add(slot);
                        }
                    }
                }
            }
        }
        else
        {
            Debug.Log("missing connectivity");
            Debug.Log(connectivity);
        }

        if (report)
        {
            Debug.Log("Slot count: " + slots.Count);
            Debug.Log("Knobs count: " + knobs.Count);
        }
        setupComplete = true;
    }

    //private void OnDrawGizmos()
    //{
    //    //Collider col = GetComponent<Collider>();
    //    //Gizmos.DrawWireCube(col.bounds.center, col.bounds.extents);
    //    if (setupComplete && slots.Count > 0 && knobs.Count > 0)
    //    {
    //        Gizmos.color = Color.green;
    //        foreach (var item in slots)
    //        {
    //            var point = transform.TransformPoint(item.position);
    //            Gizmos.DrawLine(Vector3.zero, point);
    //        }
    //        Gizmos.color = Color.red;
    //        foreach (var item in knobs)
    //        {
    //            var point = transform.TransformPoint(item.position);
    //            Gizmos.DrawLine(Vector3.zero, point);
    //        }
    //    }

    //}


}
