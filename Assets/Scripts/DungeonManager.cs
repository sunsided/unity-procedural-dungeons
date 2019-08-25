using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class DungeonManager : MonoBehaviour
{
    public int totalFloorCount;
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject tilePrefab;

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
        // Starting point of the PLAYER (... is also the random walker)
        var curPos = Vector3.zero;

        _floorList.Add(curPos);
        while (_floorList.Count < totalFloorCount)
        {
            switch (Random.Range(1, 5))
            {
                case 1:
                    curPos += Vector3.up;
                    break;
                case 2:
                    curPos += Vector3.right;
                    break;
                case 3:
                    curPos += Vector3.down;
                    break;
                case 4:
                    curPos += Vector3.left;
                    break;
            }

            // TODO: Should we use a hashset?
            if (_floorList.Contains(curPos)) continue;
            _floorList.Add(curPos);
        }

        for (var i = 0; i < _floorList.Count; ++i)
        {
            var goTile = Instantiate(tilePrefab, _floorList[i], Quaternion.identity, transform);
            goTile.name = tilePrefab.name;
        }
    }
}
