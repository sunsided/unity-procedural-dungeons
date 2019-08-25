using System;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    private DungeonManager _dm;

    private void Awake()
    {
        _dm = FindObjectOfType<DungeonManager>();

        // Convert this spawner to a floor tile. Because we're going to spawn walls in
        // unoccupied tiles, we need to make sure the floor exists before the walls are placed.
        var pos = transform.position;
        var goFloor = Instantiate(_dm.floorPrefab, pos, Quaternion.identity, _dm.transform);
        goFloor.name = _dm.floorPrefab.name;

        // Extend the map size in the manager.
        _dm.maxX = Math.Max(_dm.maxX, pos.x);
        _dm.minX = Math.Min(_dm.minX, pos.x);
        _dm.maxY = Math.Max(_dm.maxY, pos.y);
        _dm.minY = Math.Min(_dm.minY, pos.y);
    }

    private void Start()
    {
        var envMask = LayerMask.GetMask("Wall", "Floor");
        var hitSize = Vector2.one * 0.8f;

        // Starting at the spawner's position, check whether there's a covered tile
        // on each of the four sides. If there isn't any, place a wall.
        var pos = transform.position;
        for (var x = -1; x <= 1; ++x)
        {
            for (var y = -1; y <= 1; ++y)
            {
                var targetPos = new Vector2(pos.x + x, pos.y + y);
                var hit = Physics2D.OverlapBox(targetPos, hitSize, 0, envMask);
                if (hit) continue;

                // The tile we're inspecting is empty, so create a wall here.
                var goWall = Instantiate(_dm.wallPrefab, targetPos, Quaternion.identity, _dm.transform);
                goWall.name = _dm.wallPrefab.name;
            }
        }

        // Remove the tile spawner from the game.
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawCube(transform.position, Vector3.one);
    }
}
