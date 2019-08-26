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

    private readonly List<Vector2> _availableMovements = new List<Vector2>(4);
    private Player _player;
    private Vector2 _targetPos;
    private LayerMask _obstacleMask;
    private LayerMask _walkableMask;
    private bool _isMoving;
    private bool _flipX;
    private AlertState _state;
    private SpriteRenderer _renderer;
    private readonly List<Node> _nodes = new List<Node>();

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

            // Sense the player.
            var distToPlayer = Vector2.Distance(transform.position, _player.transform.position);
            if (distToPlayer > alertRange)
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
        _nodes.Clear();
        _nodes.Add(new Node(startPos, startPos));

        // Use a flood filling approach to find a path to the player.
        const int maxTries = 1000;
        var index = 0;
        var myPos = startPos;
        while (myPos != targetPos && index < maxTries && _nodes.Count > 0)
        {
            AddNodeIfWalkable(startPos, myPos + Vector2.up, myPos);
            AddNodeIfWalkable(startPos, myPos + Vector2.right, myPos);
            AddNodeIfWalkable(startPos, myPos + Vector2.down, myPos);
            AddNodeIfWalkable(startPos, myPos + Vector2.left, myPos);

            ++index;
            if (index < _nodes.Count)
            {
                myPos = _nodes[index].Position;
            }
        }

        // If we didn't find the player, do nothing.
        if (myPos != targetPos) return startPos;
        Debug.Log($"{name} has sensed the player.");

        // Backtrack the path from where we connected to the player.
        for (var i = _nodes.Count - 1; i >= 0; --i)
        {
            var node = _nodes[i];
            if (myPos != node.Position) continue;
            if (node.Parent == startPos) return myPos;
            myPos = node.Parent;
        }

        // Shouldn't happen.
        return startPos;
    }

    private void AddNodeIfWalkable(Vector2 startPos, Vector2 point, Vector2 parent)
    {
        // Never try to explore outside the alert range.
        // If the player would be there, we couldn't have seen him to begin with.
        if (Vector2.Distance(startPos, point) > alertRange + 1f) return;

        var hitSize = Vector2.one * 0.5f;
        var hit = Physics2D.OverlapBox(point, hitSize, 0, _walkableMask);
        if (hit) return;
        _nodes.Add(new Node(point, parent));
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
