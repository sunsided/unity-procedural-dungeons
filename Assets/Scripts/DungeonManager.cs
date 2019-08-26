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

    [Range(0, 100)]
    public int enemySpawnProbability = 5;

    public bool roundedEdges;

    public DungeonType dungeonType;

    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject tilePrefab;
    public GameObject exitPrefab;

    public GameObject[] randomItems;
    public GameObject[] randomEnemies;
    public GameObject[] wallRoundedEdges;

    [HideInInspector]
    public float minX;

    [HideInInspector]
    public float maxX;

    [HideInInspector]
    public float minY;

    [HideInInspector]
    public float maxY;

    [NotNull]
    private static readonly Vector3[] Directions = { Vector3.up, Vector3.right, Vector3.down, Vector3.left };

    [NotNull]
    private readonly List<Vector3> _floorList = new List<Vector3>();

    private readonly Vector2 _hitSize = Vector2.one * 0.8f;
    private LayerMask _floorMask;
    private LayerMask _wallMask;
    private Vector3? _doorPos;

    private static Vector3 RandomDirection() => Directions[Random.Range(0, Directions.Length)];

    private void Start()
    {
        _floorMask = LayerMask.GetMask("Floor");
        _wallMask = LayerMask.GetMask("Wall");

        switch (dungeonType)
        {
            case DungeonType.Caverns: RandomWalker(); break;
            case DungeonType.Rooms: RoomWalker(); break;
        }

        // Wait for the awakes and starts to be called.
        StartCoroutine(DelayProgress());
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
            curPos += RandomDirection();

            // TODO: Should we use a hashset?
            if (_floorList.Contains(curPos)) continue;
            _floorList.Add(curPos);
        }
    }

    private void RoomWalker()
    {
        // Starting point of the PLAYER (... is also the random walker)
        var curPos = Vector3.zero;
        _floorList.Add(curPos);

        while (_floorList.Count < totalFloorCount)
        {
            var walkDirection = RandomDirection();

            // We want to create 3x3 up to 9x9 rooms later on.
            var walkLength = Random.Range(9, 18);
            for (var i = 0; i < walkLength; ++i)
            {
                curPos += walkDirection;

                // TODO: Should we use a hashset?
                if (_floorList.Contains(curPos)) continue;
                _floorList.Add(curPos);
            }

            // TODO: Create a random room at end of walk
        }
    }

    private IEnumerator DelayProgress()
    {
        // Instantiate tiles.
        for (var i = 0; i < _floorList.Count; ++i)
        {
            var goTile = Instantiate(tilePrefab, _floorList[i], Quaternion.identity, transform);
            goTile.name = tilePrefab.name;
        }

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

        const int offset = 2; // TODO: Why though?
        for (var x = (int) minX - offset; x <= (int) maxX + offset; ++x)
        {
            for (var y = (int) minY - offset; y <= (int) maxY + offset; ++y)
            {
                // Note that the angle (of 0) is hugely important. If unspecified, all the
                // areas surrounding a floor tile (i.e., walls and other open floors) will be triggering
                // collisions as well.
                var hitFloor = Physics2D.OverlapBox(new Vector2(x, y), _hitSize, 0, _floorMask);
                if (hitFloor)
                {
                    // Ensure we're not placing something onto the exit door.
                    // ReSharper disable once PossibleInvalidOperationException
                    var positionIsExitDoor = Vector2.Equals(hitFloor.transform.position, _doorPos.Value);
                    if (positionIsExitDoor) continue;

                    var hitTop = Physics2D.OverlapBox(new Vector2(x, y + 1), _hitSize, 0, _wallMask);
                    var hitRight = Physics2D.OverlapBox(new Vector2(x + 1, y), _hitSize, 0, _wallMask);
                    var hitBottom = Physics2D.OverlapBox(new Vector2(x, y - 1), _hitSize, 0, _wallMask);
                    var hitLeft = Physics2D.OverlapBox(new Vector2(x - 1, y), _hitSize, 0, _wallMask);

                    CreateRandomItem(hitFloor, hitTop, hitRight, hitBottom, hitLeft);
                    CreateRandomEnemy(hitFloor, hitTop, hitRight, hitBottom, hitLeft);
                }

                RoundedEdges(x, y);
            }
        }
    }

    private void RoundedEdges(int x, int y)
    {
        if (!roundedEdges) return;

        var position = new Vector2(x, y);
        var hitWall = Physics2D.OverlapBox(position, _hitSize, 0, _wallMask);
        if (!hitWall) return;

        var hitTop = Physics2D.OverlapBox(new Vector2(x, y + 1), _hitSize, 0, _wallMask);
        var hitRight = Physics2D.OverlapBox(new Vector2(x + 1, y), _hitSize, 0, _wallMask);
        var hitBottom = Physics2D.OverlapBox(new Vector2(x, y - 1), _hitSize, 0, _wallMask);
        var hitLeft = Physics2D.OverlapBox(new Vector2(x - 1, y), _hitSize, 0, _wallMask);

        var bitValue = 0;
        bitValue += hitTop ? 0 : 1;
        bitValue += hitRight ? 0 : 2;
        bitValue += hitBottom ? 0 : 4;
        bitValue += hitLeft ? 0 : 8;

        if (bitValue == 0) return;

        var edgePrefab = wallRoundedEdges[bitValue];
        var goItem = Instantiate(edgePrefab, position, Quaternion.identity, hitWall.transform);
        goItem.name = edgePrefab.name;
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

    private void CreateRandomEnemy([NotNull] Collider2D hitFloor,
        [NotNull] Collider2D hitTop, [NotNull] Collider2D hitRight, [NotNull] Collider2D hitBottom,
        [NotNull] Collider2D hitLeft)
    {
        var hasSurroundingWall = hitTop || hitRight || hitBottom || hitLeft;
        if (hasSurroundingWall) return;

        var roll = Random.Range(1, 101);
        if (roll > enemySpawnProbability) return;

        var enemyIndex = Random.Range(0, randomEnemies.Length);
        var enemyPrefab = randomEnemies[enemyIndex];

        var floorTransform = hitFloor.transform;
        var goEnemy = Instantiate(enemyPrefab, floorTransform.position, Quaternion.identity, floorTransform);
        goEnemy.name = enemyPrefab.name;
    }
}
