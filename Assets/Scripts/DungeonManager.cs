using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class DungeonManager : MonoBehaviour
{
    public int totalFloorCount;
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject tilePrefab;
    public GameObject exitPrefab;

    [HideInInspector]
    public float minX;

    [HideInInspector]
    public float maxX;

    [HideInInspector]
    public float minY;

    [HideInInspector]
    public float maxY;

    [NotNull]
    private readonly List<Vector3> _floorList = new List<Vector3>();

    private void Start()
    {
        RandomWalker();
    }

    private void Update()
    {
        // Reload scene on hotkey.
        if (Application.isEditor && Input.GetKeyDown(KeyCode.Backspace))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void RandomWalker()
    {
        // The directions the random walk can move to.
        var directions = new[] { Vector3.up, Vector3.right, Vector3.down, Vector3.left };

        // Starting point of the PLAYER (... is also the random walker)
        var curPos = Vector3.zero;

        _floorList.Add(curPos);
        while (_floorList.Count < totalFloorCount)
        {
            var delta = directions[Random.Range(0, directions.Length)];
            curPos += delta;

            // TODO: Should we use a hashset?
            if (_floorList.Contains(curPos)) continue;
            _floorList.Add(curPos);
        }

        for (var i = 0; i < _floorList.Count; ++i)
        {
            var goTile = Instantiate(tilePrefab, _floorList[i], Quaternion.identity, transform);
            goTile.name = tilePrefab.name;
        }

        // Wait for the awakes and starts to be called.
        StartCoroutine(DelayProgress());
    }

    private IEnumerator DelayProgress()
    {
        // Wait for all tile spawners to be created before continuing to place level elements.
        while (FindObjectsOfType<TileSpawner>().Length > 0)
        {
            yield return null;
        }

        CreateExitDoor();
    }

    private void CreateExitDoor()
    {
        // We're assuming that the random walker ended up somewhere distant from the player.
        // This is the location were we place our exit dor.
        var doorPos = _floorList[_floorList.Count - 1];

        var goDoor = Instantiate(exitPrefab, doorPos, Quaternion.identity, transform);
        goDoor.name = exitPrefab.name;
    }
}
