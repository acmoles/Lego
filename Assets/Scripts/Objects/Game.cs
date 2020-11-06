using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;
using Leap.Unity.Interaction;

public class Game : Persistable
{
    public static Game Instance { get; set; }

    [SerializeField] ShapeFactory shapeFactory;
    List<Shape> shapes;
    public SpawnZone SpawnZoneLevel { get; set; }

    [SerializeField] PersistentStorage storage;
    const int saveVersion = 2;

    [SerializeField] int levelCount;
    int loadedLevelBuildIndex = 0;

    public TextMeshProUGUI levelText;
    public InteractionManager manager;

    [SerializeField] KeyCode create = KeyCode.C;
    [SerializeField] KeyCode reset = KeyCode.N;
    [SerializeField] KeyCode save = KeyCode.S;
    [SerializeField] KeyCode load = KeyCode.L;
    [SerializeField] KeyCode destroy = KeyCode.X;

    public float CreationSpeed { get; set; }
    float creationProgress;
    public float DestructionSpeed { get; set; }
    float destructionProgress;

    void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {
        Instance = this;
        shapes = new List<Shape>();

        // Load only the active level in the Editor, or next available level of none
        if (Application.isEditor)
        {
            bool notInLevel = false;
            int[] levelsToUnload = new int[SceneManager.sceneCount];
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.name.Contains("ObjectLevel "))
            {
                Debug.LogError("Active scene " + activeScene.name + "is not a level!");
                notInLevel = true;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name.Contains("ObjectLevel ") && loadedScene.name == activeScene.name)
                {
                    Debug.Log("Editor active level: " + loadedScene.name);
                    loadedLevelBuildIndex = loadedScene.buildIndex;
                    levelText.text = loadedScene.buildIndex.ToString();
                    GetGameLevelFromScene(activeScene).SetActiveSpawnZone();
                    // i.e. don't actually load the level
                    for (int k = 0; k < 10; k++)
                    {
                        CreateShape();
                    }
                    CreatePlate();

                } else if (loadedScene.name.Contains("ObjectLevel ") && notInLevel)
                {
                    notInLevel = false;
                    // Load next available level if none active
                    Debug.Log("Set editor active level: " + loadedScene.name);
                    loadedLevelBuildIndex = loadedScene.buildIndex;
                    GetGameLevelFromScene(loadedScene).SetActiveSpawnZone();
                }
                else if (!loadedScene.name.Contains("Objects"))
                {
                    levelsToUnload[i] = loadedScene.buildIndex;
                }
            }
            // Unload levels not needed
            if (loadedLevelBuildIndex != 0)
            {
                StartCoroutine(UnloadLevels(levelsToUnload));
                return;
            }
        }
        // Else load level 1
        StartCoroutine(LoadLevel(1));
    }

    IEnumerator LoadLevel(int levelBuildIndex)
    {
        enabled = false;
        // Would show loader

        if (loadedLevelBuildIndex > 0)
        {
            Debug.Log("Unloading level: " + loadedLevelBuildIndex);
            yield return SceneManager.UnloadSceneAsync(loadedLevelBuildIndex);
        }

        Debug.Log("Loading level: " + levelBuildIndex);
        yield return SceneManager.LoadSceneAsync(levelBuildIndex, LoadSceneMode.Additive);
        levelText.text = levelBuildIndex.ToString();
        Scene activeScene = SceneManager.GetSceneByBuildIndex(levelBuildIndex);
        SceneManager.SetActiveScene(activeScene);
        GetGameLevelFromScene(activeScene).SetActiveSpawnZone();

        loadedLevelBuildIndex = levelBuildIndex;
        enabled = true;
    }

    IEnumerator UnloadLevels(int[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] != 0)
            {
                Debug.Log("Unloading level: " + args[i]);
                yield return SceneManager.UnloadSceneAsync(args[i]);
            } else
            {
                yield return null;
            }
        }
    }

    GameLevel GetGameLevelFromScene (Scene activeScene)
    {
        List<GameObject> rootObjects = new List<GameObject>();
        activeScene.GetRootGameObjects(rootObjects);

        for (int i = 0; i < rootObjects.Count; ++i)
        {
            GameLevel gameLevel = rootObjects[i].GetComponent<GameLevel>();
            if (gameLevel != null)
            {
                Debug.Log("Found game level");
                return gameLevel;
            }
        }
        return null;
    }

    void Update()
    {
        if (Input.GetKeyDown(create))
        {
            CreateShape();
        }
        else if (Input.GetKeyDown(destroy))
        {
            DestroyShape();
        }
        else if (Input.GetKeyDown(reset))
        {
            NewGame();
        }
        else if (Input.GetKeyDown(save))
        {
            storage.Save(this, saveVersion);
        }
        else if (Input.GetKeyDown(load))
        {
            NewGame();
            storage.Load(this);
        } else
        {
            for (int i = 1; i <= levelCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    NewGame();
                    Debug.Log("hit number key " + KeyCode.Alpha0 + i);
                    StartCoroutine(LoadLevel(i));
                    return;
                }
            }
        }

        creationProgress += Time.deltaTime * CreationSpeed;
        while (creationProgress >= 1f)
        {
            creationProgress -= 1f;
            CreateShape();
        }
        destructionProgress += Time.deltaTime * DestructionSpeed;
        while (destructionProgress >= 1f)
        {
            destructionProgress -= 1f;
            DestroyShape();
        }
    }

    void CreateShape()
    {
        Shape instance = shapeFactory.GetRandom();
        LegoBrick lb = instance.GetComponent<LegoBrick>();
        lb.Init();
        Transform t = instance.transform;
        t.localPosition = SpawnZoneLevel.SpawnPoint;
        t.localRotation = UnityEngine.Random.rotation;

        Array values = Enum.GetValues(typeof(LegoColors.Id));
        int randomIndex = UnityEngine.Random.Range(0, values.Length);
        var randomId = (LegoColors.Id)values.GetValue(randomIndex);
        Color color = LegoColors.GetColour(randomId);

        //t.localScale = Vector3.one * Random.Range(0.5f, 2f);
        instance.SetColor(color);
        shapes.Add(instance);
    }

    void CreatePlate() {
        Debug.Log("Create plate");
        Shape instance = shapeFactory.GetPlate();
        LegoBrick lb = instance.GetComponent<LegoBrick>();
        lb.Init();
        Transform t = instance.transform;
        Vector3 position = new Vector3(0f, 1.455f, 0.2f);
        t.localPosition = position;
        instance.SetColor(Color.white);

        shapes.Add(instance);
    }

    void DestroyShape()
    {
        if (shapes.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, shapes.Count);
            Destroy(shapes[index].gameObject);
            int lastIndex = shapes.Count - 1;
            shapes[index] = shapes[lastIndex];
            shapes.RemoveAt(lastIndex);
        }
    }

    void NewGame()
    {
        for (int i = 0; i < shapes.Count; i++)
        {
            shapeFactory.Reclaim(shapes[i]);
        }
        shapes.Clear();
    }

    public override void Save(DataWriter writer)
    {
        writer.Write(shapes.Count);
        writer.Write(loadedLevelBuildIndex);
        for (int i = 0; i < shapes.Count; i++)
        {
            writer.Write(shapes[i].ShapeId);
            writer.Write(shapes[i].MaterialId);
            shapes[i].Save(writer);
        }
    }

    public override void Load(DataReader reader)
    {
        int version = reader.Version;
        if (version > saveVersion)
        {
            Debug.LogError("Unsupported future save version " + version);
            return;
        }
        int count = version <= 0 ? version : reader.ReadInt();
        StartCoroutine(LoadLevel(version < 2 ? 1 : reader.ReadInt()));
        for (int i = 0; i < count; i++)
        {
            int shaid = version > 0 ? reader.ReadInt() : 0;
            int matid = version > 0 ? reader.ReadInt() : 0;
            Shape instance = shapeFactory.Get(shaid, matid);
            instance.Load(reader);
            shapes.Add(instance);
        }
    }



    //void Save()
    //{
    //    using (
    //        var writer = new BinaryWriter(File.Open(savePath, FileMode.Create))
    //    )
    //        {
    //        writer.Write(objects.Count);
    //        for (int i = 0; i < objects.Count; i++)
    //        {
    //            Transform t = objects[i];
    //            writer.Write(t.localPosition.x);
    //            writer.Write(t.localPosition.y);
    //            writer.Write(t.localPosition.z);
    //        }
    //        Debug.Log("count saved: " + objects.Count);
    //    }
    //}

    //void Load()
    //{
    //    NewGame();
    //    using (
    //       var reader = new BinaryReader(File.Open(savePath, FileMode.Open))
    //    )
    //        {
    //        int count = reader.ReadInt32();
    //        Debug.Log("count loaded: " + count);
    //        for (int i = 0; i < count; i++)
    //        {
    //            Vector3 p;
    //            p.x = reader.ReadSingle();
    //            p.y = reader.ReadSingle();
    //            p.z = reader.ReadSingle();
    //            Transform t = Instantiate(prefab);
    //            t.localPosition = p;
    //            objects.Add(t);
    //        }
    //    }
    //}


}
