﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shape : Persistable
{
    int shapeId = int.MinValue;

    public GameObject mesh;
    public MeshRenderer meshRenderer;
    public Transform meshTransform;

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
        meshRenderer.material = material;
        MaterialId = materialId;
    }

    public Color color { get; set; }
    Color _color;
    static int colorPropertyId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock sharedPropertyBlock;

    public void SetColor(Color color)
    {
        _color = this.color;
        this.color = color;
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetColor(colorPropertyId, color);
        meshRenderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public void ResetColor()
    {
        if (_color != null)
        {
            SetColor(_color);
            Debug.Log("Reset color");
        }
    }

    public override void Save(DataWriter writer)
    {
        base.Save(writer);
        writer.Write(color);
    }


    public override void Load(DataReader reader)
    {
        base.Load(reader);
        SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);
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