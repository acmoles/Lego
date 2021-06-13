﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using TMPro;
using Leap.Unity.Interaction;
using Leap;
using Leap.Unity;

public class Game : Persistable
{
    public static Game Instance { get; set; }

    [SerializeField] ShapeFactory shapeFactory;
    List<Shape> shapes;
    public SpawnZone SpawnZoneLevel { get; set; }

    [SerializeField] PersistentStorage storage;
    const int saveVersion = 2;

    [SerializeField] int levelCount = 0;
    int loadedLevelBuildIndex = 0;

    public InteractionManager manager;
    //public LeapProvider provider;
    public PinchDetector pinchDetectorLeft, pinchDetectorRight;
    public InteractionHand handLeft, handRight;
    public int initialSpawn = 10;
    private HashSet<Shape> _heldShapes;
    public HashSet<Shape> HeldShapes
    {
        get
        {
            if (_heldShapes == null)
            {
                _heldShapes = new HashSet<Shape>();
            }
            return _heldShapes;
        }
    }
    Dictionary<string, int> idMap;

    [SerializeField] KeyCode create = KeyCode.C;
    [SerializeField] KeyCode reset = KeyCode.N;
    [SerializeField] KeyCode save = KeyCode.S;
    [SerializeField] KeyCode load = KeyCode.L;
    [SerializeField] KeyCode destroy = KeyCode.X;
    [SerializeField] KeyCode disconnectAll = KeyCode.D;

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
        idMap = shapeFactory.GenerateMapping();

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
                    GetGameLevelFromScene(activeScene).SetActiveSpawnZone();
                    // i.e. don't actually load the level

                    CreateSpecific();

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

        Debug.Log("Loading in build");
        // Else load level 3
        StartCoroutine(LoadLevel(3, () => {
            //CreateOneOfEach();
            CreateSpecific();
        }));
    }

    public delegate void CompletedCallBack();

    IEnumerator LoadLevel(int levelBuildIndex, Action callback = null)
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
        Scene activeScene = SceneManager.GetSceneByBuildIndex(levelBuildIndex);
        SceneManager.SetActiveScene(activeScene);
        GetGameLevelFromScene(activeScene).SetActiveSpawnZone();

        loadedLevelBuildIndex = levelBuildIndex;
        enabled = true;
        if (callback != null) callback();
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

    GameLevel GetGameLevelFromScene(Scene activeScene)
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
        else if (Input.GetKeyDown(disconnectAll))
        {
            LegoBrick.DisconnectAllBricks();
        }
        else if (Input.GetKeyDown(save))
        {
            storage.Save(this, saveVersion);
        }
        else if (Input.GetKeyDown(load))
        {
            NewGame();
            storage.Load(this);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        else
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

    void CreateRandomSet()
    {
        for (int k = 0; k < initialSpawn; k++)
        {
            CreateShape();
        }
    }

    void CreateOneOfEach()
    {
        for (int k = 0; k < shapeFactory.CountBrickPrefabs(); k++)
        {
            CreateShape(k);
        }
    }

    [SerializeField]
    Shape[] SpecificBricksToSpawn;

    void CreateSpecific()
    {
        foreach (var brick in SpecificBricksToSpawn)
        {
            CreateBrick(brick.name);
        }
    }

    public void CreateBrick(string name, int materialIndex = 0, int colorId = 0, float x = 0f, float y = 0f, float z = 0f)
    {
        int index = idMap[name];
        CreateShape(index, materialIndex, colorId, x, y, z);
    }

    public void CreateShape(int index = -1, int materialIndex = 0, int colorId = 0, float x = 0f, float y = 0f, float z = 0f)
    {

        Shape instance;
        if (index < 0)
        {
            instance = shapeFactory.GetRandom();
        } else
        {
            instance = shapeFactory.Get(index, materialIndex);
        }

        LegoBrick lb = instance.GetComponent<LegoBrick>();
        lb.Init();

        Transform t = instance.transform;
        if (x != 0f && y != 0f && z != 0f)
        {
            t.localPosition = new Vector3(x, y, z);
        } else
        {
            t.localPosition = SpawnZoneLevel.SpawnPoint;
        }
        t.localRotation = UnityEngine.Random.rotation;

        if (colorId != 0)
        {
            instance.SetColor(colorId);
        } 
        else
        {
            //Array values = Enum.GetValues(typeof(LegoColors.Id));
            Array values = LegoColors.spawnable.ToArray();
            int randomIndex = UnityEngine.Random.Range(0, values.Length);
            var randomId = (LegoColors.Id)values.GetValue(randomIndex);
            instance.SetColor((int)randomId);
        }
        instance.SetEmission(0f);

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

    public static void Warn(string msg, UnityEngine.Object obj)
    {
        Debug.Log("<b>Game: </b>" + msg, obj);
    }

}
