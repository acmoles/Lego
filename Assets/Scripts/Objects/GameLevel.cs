using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLevel : MonoBehaviour
{
    [SerializeField] SpawnZone spawnZone;

    // Start is called before the first frame update
    //void Start()
    //{
    //    Game.Instance.SpawnZoneLevel = spawnZone;
    //}

    public void SetActiveSpawnZone()
    {
        Game.Instance.SpawnZoneLevel = spawnZone;
    }
}
