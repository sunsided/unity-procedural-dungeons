using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class DungeonManager : MonoBehaviour
{
    [Range(50, 2000)]
    public int totalFloorCount = 500;

    [Range(0, 100)]
    public int itemSpawnProbability = 5;

    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject tilePrefab;
    public GameObject exitPrefab;

    public GameObject[] randomItems;

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

    private LayerMask _floorMask;
    private LayerMask _wallMask;
    private Vector3? _doorPos;

    private void Start()
    {
        _floorMask = LayerMask.GetMask("Floor");
        _wallMask = LayerMask.GetMask("Wall");

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
            curPos += directions[Random.Range(0, directions.Length)];

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
        CreateRandomItems();
    }

    private void CreateExitDoor()
    {
        // We're assuming that the random walker ended up somewhere distant from the player.
        // This is the location were we place our exit dor.
        _doorPos = _floorList[_floorList.Count - 1];

        var goDoor = Instantiate(exitPrefab, _doorPos.Value, Quaternion.identity, transform);
        goDoor.name = exitPrefab.name;
    }

    private void CreateRandomItems()
    {
        Debug.Assert(_doorPos.HasValue, "Exit door must be instantiated before placing items.");
        var hitSize = Vector2.one * 0.8f;

        const int offset = 2; // TODO: Why though?
        for (var x = (int) minX - offset; x <= (int) maxX + offset; ++x)
        {
            for (var y = (int) minY - offset; y <= (int) maxY + offset; ++y)
            {
                // Note that the angle (of 0) is hugely important. If unspecified, all the
                // areas surrounding a floor tile (i.e., walls and other open floors) will be triggering
                // collisions as well.
                var hitFloor = Physics2D.OverlapBox(new Vector2(x, y), hitSize, 0, _floorMask);
                if (hitFloor)
                {
                    var hitFloorTransformPos = hitFloor.transform.position;

                    // Ensure we're not placing something onto the exit door.
                    // ReSharper disable once PossibleInvalidOperationException
                    var positionIsExitDoor = Vector2.Equals(hitFloorTransformPos, _doorPos.Value);
                    if (!positionIsExitDoor)
                    {
                        var hitTop = Physics2D.OverlapBox(new Vector2(x, y + 1), hitSize, 0, _wallMask);
                        var hitRight = Physics2D.OverlapBox(new Vector2(x + 1, y), hitSize, 0, _wallMask);
                        var hitBottom = Physics2D.OverlapBox(new Vector2(x, y - 1), hitSize, 0, _wallMask);
                        var hitLeft = Physics2D.OverlapBox(new Vector2(x - 1, y), hitSize, 0, _wallMask);

                        CreateRandomItem(hitFloor, hitTop, hitRight, hitBottom, hitLeft);
                    }
                }
            }
        }
    }

    private void CreateRandomItem([NotNull] Collider2D hitFloor,
        [NotNull] Collider2D hitTop, [NotNull] Collider2D hitRight, [NotNull] Collider2D hitBottom,
        [NotNull] Collider2D hitLeft)
    {
        var hasSurroundingWall = hitTop || hitRight || hitBottom || hitLeft;
        var isHorizontalTunnel = hitTop && hitBottom;
        var isVerticalTunnel = hitLeft && hitRight;
        var isTunnel = isHorizontalTunnel || isVerticalTunnel;
        if (!hasSurroundingWall || isTunnel) return;

        var roll = Random.Range(1, 101);
        if (roll > itemSpawnProbability) return;

        var itemIndex = Random.Range(0, randomItems.Length);
        var itemPrefab = randomItems[itemIndex];

        var floorTransform = hitFloor.transform;
        var goItem = Instantiate(itemPrefab, floorTransform.position, Quaternion.identity, floorTransform);
        goItem.name = itemPrefab.name;
    }
}
