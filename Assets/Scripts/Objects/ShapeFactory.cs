using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class ShapeFactory : ScriptableObject
{

    [SerializeField]
    Shape[] prefabs;

    [SerializeField]
    Material[] materials;

    [SerializeField]
    bool recycle;

    List<Shape>[] pools;
    Scene poolScene;

    public Shape Get(int shapeId = 0, int materialId = 0) {

        Shape instance;

        if (recycle)
        {
            if (pools == null)
            {
                CreatePools();
            }
            List<Shape> pool = pools[shapeId];
            int lastIndex = pool.Count - 1;
            if (lastIndex >= 0)
            {
                instance = pool[lastIndex];
                instance.gameObject.SetActive(true);
                pool.RemoveAt(lastIndex);
            }
            else
            {
                instance = Instantiate(prefabs[shapeId]);
                instance.ShapeId = shapeId;
                SceneManager.MoveGameObjectToScene(instance.gameObject, poolScene);
            }
        } else
        {
            instance = Instantiate(prefabs[shapeId]);
            instance.ShapeId = shapeId;
        }

        instance.SetMaterial(materials[materialId], materialId);

        return instance;
    }

    public Shape GetRandom()
    {
        return Get(Random.Range(0, prefabs.Length), Random.Range(0, materials.Length));
        // Random.Range has an exclusive maximum (n-1) with integer params (inclusive for float)
        // -1 to remove plate
    }

    //public Shape GetPlate()
    //{
    //    Shape instance = Instantiate(platePrefab);
    //    int materialId = Random.Range(0, materials.Length);
    //    instance.SetMaterial(materials[materialId], materialId);
    //    return instance;
    //}

    void CreatePools()
    {
        pools = new List<Shape>[prefabs.Length];
        // lists, arrays VS stacks c#?
        for (int i = 0; i < pools.Length; i++)
        {
            pools[i] = new List<Shape>();
        }

        if (Application.isEditor)
        {
            poolScene = SceneManager.GetSceneByName(name);
            if (poolScene.isLoaded)
            {
                GameObject[] rootObjects = poolScene.GetRootGameObjects();
                for (int i = 0; i < rootObjects.Length; i++)
                {
                    Shape pooledShape = rootObjects[i].GetComponent<Shape>();
                    if (!pooledShape.gameObject.activeSelf)
                    {
                        pools[pooledShape.ShapeId].Add(pooledShape);
                    }
                }

                return;
            }
        }

        poolScene = SceneManager.CreateScene(name);
        // name is name of the ShapeFactory instance
    }

    public void Reclaim(Shape toRecycle)
    {
        if (recycle)
        {
            if (pools == null)
            {
                CreatePools();
            }
            pools[toRecycle.ShapeId].Add(toRecycle);
            toRecycle.gameObject.SetActive(false);
        }
        else
        {
            Destroy(toRecycle.gameObject);
        }
    }

    public int CountBrickPrefabs()
    {
        return prefabs.Length;
    }

    public Dictionary<string, int> GenerateMapping()
    {
        Dictionary<string, int> mapIDs = new Dictionary<string, int>();

        for (int i = 0; i < prefabs.Length; i++)
        {
            mapIDs[prefabs[i].name] = i;
        }

        return mapIDs;
    }


}
