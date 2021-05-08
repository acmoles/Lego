using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Shape : Persistable
{
    int shapeId = int.MinValue;

    List<MeshRenderer> renderersToEdit = new List<MeshRenderer>();

    [HideInInspector]
    public Mesh CombinedMesh;

    public int colorID = 0;
    public bool isEye = false;

    public void Awake()
    {
        if (isEye) return;

        Transform parentPart = transform.GetChild(0);
        Transform shell = parentPart.Find("Shell");
        if (shell)
        {
            var mr = shell.GetComponent<MeshRenderer>();
            renderersToEdit.Add(mr);
        }
        else
        {
            Debug.Log("Missing shell!");
        }

        Transform knobs = parentPart.Find("Knobs");
        if (knobs != null)
        {
            foreach (Transform knob in knobs)
            {
                var mr = knob.GetComponent<MeshRenderer>();
                renderersToEdit.Add(mr);
            }
        }

        Transform slots = parentPart.Find("Tubes");
        if (slots != null)
        {
            foreach (Transform slot in slots)
            {
                var mr = slot.GetComponent<MeshRenderer>();
                renderersToEdit.Add(mr);
            }
        }

        if (colorID != 0)
        {
            SetColor(colorID);
        }
    }

    public void init()
    {
        GenerateCombinedMesh();
    }

    void GenerateCombinedMesh()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

            i++;
        }
        CombinedMesh = new Mesh();
        CombinedMesh.CombineMeshes(combine);
    }

    public int ShapeId
    {
        get
        {
           return shapeId;
        }

        set
        {
            if (shapeId == int.MinValue && value != int.MinValue)
            {
                shapeId = value;
            }
            else
            {
                Debug.LogError("Don't change shapeId");
            }
        }
    }

    public int MaterialId { get; private set; }

    public void SetMaterial(Material material, int materialId)
    {
        if (isEye) return;

        foreach (var renderer in renderersToEdit)
        {
            renderer.material = material;
        }
        MaterialId = materialId;
    }

    public Color color { get; set; }

    [HideInInspector]
    public static int colorPropertyId = Shader.PropertyToID("_Color");
    [HideInInspector]
    public static MaterialPropertyBlock sharedPropertyBlock;

    public void SetColor(int colorID)
    {
        if (isEye) return;
        Color color = LegoColors.GetColour(colorID);
        if (color == null)
        {
            Debug.LogError("No color at ID");
            return;
        }

        this.colorID = colorID;
        this.color = color;
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetColor(colorPropertyId, color);
        foreach (var renderer in renderersToEdit)
        {
            renderer.SetPropertyBlock(sharedPropertyBlock);
        }
    }

    [HideInInspector]
    public static int emissionPropertyId = Shader.PropertyToID("_Emission");
    public void SetEmission(float emission)
    {
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        // Keep current color
        sharedPropertyBlock.SetColor(colorPropertyId, this.color);
        sharedPropertyBlock.SetFloat(emissionPropertyId, emission);
        foreach (var renderer in renderersToEdit)
        {
            renderer.SetPropertyBlock(sharedPropertyBlock);
        }
    }

    public void ClearPropertyBlock()
    {
        sharedPropertyBlock.Clear();
    }

    public override void Save(DataWriter writer)
    {
        base.Save(writer);
        writer.Write(color);
    }


    public override void Load(DataReader reader)
    {
        base.Load(reader);
        //SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);
    }




    //// Start is called before the first frame update
    //void Start()
    //{

    //}

    //// Update is called once per frame
    //void Update()
    //{

    //}
}
