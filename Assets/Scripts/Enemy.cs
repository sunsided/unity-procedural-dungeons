using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    public Vector2 patrolInterval = new Vector2(1, 2);
    public Vector2 damageRange = new Vector2(1, 10);
    public float moveSpeed = 5;
    public float chaseDelay = 0.5f;
    public float alertRange = 10f;
    public float chaseRange = 20f;

    private readonly List<Vector2> _availableMovements = new List<Vector2>(4);
    private Player _player;
    private Vector2 _targetPos;
    private LayerMask _obstacleMask;
    private LayerMask _walkableMask;
    private bool _isMoving;
    private bool _flipX;
    private AlertState _state;
    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _flipX = _renderer.flipX;
    }

    private void Start()
    {
        _player = FindObjectOfType<Player>();
        _targetPos = transform.position;
        _obstacleMask = LayerMask.GetMask("Wall", "Enemy", "Player");
        _walkableMask = LayerMask.GetMask("Wall", "Enemy");
        StartCoroutine(Movement());
    }

    private void Patrol()
    {
        _availableMovements.Clear();
        var hitSize = Vector2.one * 0.8f;

        var hitUp = Physics2D.OverlapBox(_targetPos + Vector2.up, hitSize, 0, _obstacleMask);
        if (!hitUp) _availableMovements.Add(Vector2.up);

        var hitRight = Physics2D.OverlapBox(_targetPos + Vector2.right, hitSize, 0, _obstacleMask);
        if (!hitRight) _availableMovements.Add(Vector2.right);

        var hitDown = Physics2D.OverlapBox(_targetPos + Vector2.down, hitSize, 0, _obstacleMask);
        if (!hitDown) _availableMovements.Add(Vector2.down);

        var hitLeft = Physics2D.OverlapBox(_targetPos + Vector2.left, hitSize, 0, _obstacleMask);
        if (!hitLeft) _availableMovements.Add(Vector2.left);

        if (_availableMovements.Count > 0)
        {
            var index = Random.Range(0, _availableMovements.Count);
            var movement = _availableMovements[index];
            _targetPos += movement;
        }

        var moveDelay = Random.Range(patrolInterval.x, patrolInterval.y);
        StartCoroutine(SmoothMove(moveDelay));
    }

    private IEnumerator Movement()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            if (_isMoving) continue;

            // TODO: The whole movement logic is a bit off.
            //       This may be improved with the following behavior:
            //       If the player is in attack range, attack.
            //       If the player is in alert range, move to the player's position ("chase").
            //       If the player is lost during a chase ("irritated"), move to the player's last known position instead.

            // Sense the player.
            var distToPlayer = Vector2.Distance(transform.position, _player.transform.position);
            var inAlertRange = distToPlayer <= alertRange;
            var inChaseRange = _state == AlertState.Chasing && distToPlayer <= chaseRange;
            if (!inAlertRange && !inChaseRange)
            {
                if (_state != AlertState.Patrolling)
                {
                    UpdateState($"{name} lost its interest", AlertState.Patrolling);
                }

                Patrol();
                continue;
            }

            if (_state == AlertState.Patrolling)
            {
                UpdateState($"Player startled the {name}", AlertState.Alerted);
            }

            // Attack player if close enough.
            // Allowed directions are only orthogonal, not diagonal.
            // Distance on diagonal would be sqrt(2) = 1.4, so we check
            // for a value less than that.
            if (distToPlayer <= 1.1f)
            {
                _state = AlertState.Attacking;
                Attack();
                yield return new WaitForSeconds(Random.Range(0.5f, 1.15f));
                continue;
            }

            // Chase the player.
            var newPos = FindNextStep(transform.position, _player.transform.position);
            if (newPos != _targetPos)
            {
                UpdateState($"{name} is chasing the player", AlertState.Chasing);

                _targetPos = newPos;
                StartCoroutine(SmoothMove(chaseDelay));
            }
            else
            {
                UpdateState($"{name} can't find the player", AlertState.Irritated);
                Patrol();
            }
        }
    }

    private void Attack()
    {
        var roll = Random.Range(0, 100);
        if (roll > 50)
        {
            var damageAmount = Mathf.Ceil(Random.Range(damageRange.x, damageRange.y));
            Debug.Log($"{name} attacked and hit for {damageAmount}");
        }
        else
        {
            Debug.Log($"{name} attacked and missed");
        }
    }

    private Vector2 FindNextStep(Vector2 startPos, Vector2 targetPos)
    {
        const int failsafe = 1000;
        var openList = new List<Node>();

        var distanceFromStart = 0;
        var heuristicToTarget = DistanceHeuristic(startPos, targetPos);
        var current = new Node(startPos, null, distanceFromStart, heuristicToTarget);

        openList.Add(current);
        while (openList.Count > 0 && openList.Count < failsafe)
        {
            // Find the item with the smallest F-score, remove it from the
            // open list and add it to the closed list.
            var smallestIndex = FindIndexWithSmallestFScore(openList);
            current = openList[smallestIndex];
            openList.RemoveAt(smallestIndex);

            // Check if we have a match.
            if (current.Position == targetPos) break;

            // Add walkable tiles to explore list
            AddNodeIfWalkable(current, openList, Vector2.up, targetPos);
            AddNodeIfWalkable(current, openList, Vector2.right, targetPos);
            AddNodeIfWalkable(current, openList, Vector2.down, targetPos);
            AddNodeIfWalkable(current, openList, Vector2.left, targetPos);
        }

        // In case we didn't find anything, abort.
        if (current.Position != targetPos) return startPos;

        // Otherwise, backtrack to the start.
        var nextPosition = startPos;
        while (current.Parent != null)
        {
            nextPosition = current.Position;
            current = current.Parent;
        }

        return nextPosition;

        int FindIndexWithSmallestFScore<T>(T nodes) where T: IReadOnlyList<Node>
        {
            var smallestScore = float.PositiveInfinity;
            var smallestIndex = -1;
            for (var i = 0; i < nodes.Count; ++i)
            {
                var node = nodes[i];
                if (node.Score >= smallestScore) continue;
                smallestScore = node.Score;
                smallestIndex = i;
            }

            return smallestIndex;
        }
    }

    private static float DistanceHeuristic(Vector2 startPos, Vector2 targetPos)
    {
        var distance = startPos - targetPos;
        return Mathf.Abs(distance.x) + Mathf.Abs(distance.y);
    }

    private void AddNodeIfWalkable<T>([NotNull] Node current, [NotNull] T openList, Vector2 direction, Vector2 target)
        where T : IList<Node>
    {
        var hitSize = Vector2.one * 0.5f;
        var point = current.Position + direction;
        var hit = Physics2D.OverlapBox(point, hitSize, 0, _walkableMask);
        if (hit) return;

        var distanceFromStart = current.DistanceFromStart + 1;
        var heuristicToTarget = DistanceHeuristic(point, target);
        var next = new Node(point, current, distanceFromStart, heuristicToTarget);

        // If the position already exists in the list, but has a higher F-Score - replace it.
        var foundNode = false;
        for (var i = 0; i < openList.Count; ++i)
        {
            var node = openList[i];
            foundNode = node.Position == point;
            if (foundNode && node.Score > next.Score)
            {
                openList[i] = next;
                return;
            }
        }

        // Otherwise, if the node doesn't exist, add it.
        if (!foundNode) openList.Add(next);
    }

    private void UpdateState([NotNull] string message, AlertState newState)
    {
        Debug.Log($"{message} ({_state} -> {newState}).");
        _state = newState;
    }

    private IEnumerator SmoothMove(float moveDelay)
    {
        _isMoving = true;
        AdjustLookDirection();
        while (Vector2.Distance(transform.position, _targetPos) > 0.01)
        {
            transform.position = Vector2.MoveTowards(transform.position, _targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = _targetPos;
        yield return new WaitForSeconds(moveDelay);
        _isMoving = false;
    }

    private void AdjustLookDirection()
    {
        var pos = transform.position.x;
        var movingRight = _targetPos.x > pos;
        var movingLeft = _targetPos.x < pos;
        if (movingRight)
        {
            _renderer.flipX = _flipX;
        }
        else if (movingLeft)
        {
            _renderer.flipX = !_flipX;
        }
    }

    private enum AlertState
    {
        Patrolling,
        Alerted,
        Attacking,
        Chasing,
        Irritated
    }
}
