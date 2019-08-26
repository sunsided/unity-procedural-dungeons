using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    public Vector2 patrolInterval = new Vector2(1, 2);
    public float moveSpeed = 5;

    private readonly List<Vector2> _availableMovements = new List<Vector2>(4);
    private Vector2 _targetPos;
    private LayerMask _obstacleMask;
    private bool _isMoving;
    private bool _flipX;
    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _flipX = _renderer.flipX;
    }

    private void Start()
    {
        _targetPos = transform.position;
        _obstacleMask = LayerMask.GetMask("Wall", "Enemy", "Player");
    }

    private void Update()
    {
        if (!_isMoving) Patrol();
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

            if (movement == Vector2.right)
            {
                _renderer.flipX = _flipX;
            }
            else if (movement == Vector2.left)
            {
                _renderer.flipX = !_flipX;
            }
        }

        StartCoroutine(SmoothMove());
    }

    private IEnumerator SmoothMove()
    {
        _isMoving = true;

        while (Vector2.Distance(transform.position, _targetPos) > 0.01)
        {
            transform.position = Vector2.MoveTowards(transform.position, _targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = _targetPos;

        var moveDelay = Random.Range(patrolInterval.x, patrolInterval.y);
        yield return new WaitForSeconds(moveDelay);
        _isMoving = false;
    }
}
