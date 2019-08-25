using System;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 1.0f;

    private Transform _gfx;
    private float _flipX;
    private bool _isMoving;
    private LayerMask _obstacleMask;
    private Vector2 _targetPos;

    private void Start()
    {
        _obstacleMask = LayerMask.GetMask("Wall", "Enemy");

        _gfx = GetComponentInChildren<SpriteRenderer>().transform;
        _flipX = _gfx.localScale.x;
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        // Using Math.Sign() rather than Mathf.Sign() because we want 0 to be mapped to a 0 sign.
        // Note that we're also using GetAxisRaw instead of GetAxis.
        var horizontal = Math.Sign(Input.GetAxisRaw("Horizontal"));
        var vertical = Math.Sign(Input.GetAxisRaw("Vertical"));

        var xPressed = Mathf.Abs(horizontal) > 0;
        var yPressed = Mathf.Abs(vertical) > 0;
        if (!xPressed && !yPressed) return;

        if (xPressed)
        {
            _gfx.localScale = new Vector2(_flipX * horizontal, _gfx.localScale.y);
        }

        if (_isMoving) return;

        // Set new target position
        var pos = transform.position;
        if (xPressed)
        {
            _targetPos = new Vector2(pos.x + horizontal, pos.y);
        }
        else
        {
            Debug.Assert(yPressed, "yPressed == true");
            _targetPos = new Vector2(pos.x, pos.y + vertical);
        }

        // Check for collisions
        var hitSize = Vector2.one * 0.8f;
        var hit = Physics2D.OverlapBox(_targetPos, hitSize, 0, _obstacleMask);
        if (!hit)
        {
            StartCoroutine(SmoothMove());
        }
    }

    private IEnumerator SmoothMove()
    {
        Debug.Assert(!_isMoving, "!_isMoving");
        _isMoving = true;

        // Approach the target position just enough to be almost there.
        while (Vector2.Distance(transform.position, _targetPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, _targetPos, speed * Time.deltaTime);
            yield return null;
        }

        // Fix the target position.
        transform.position = _targetPos;
        _isMoving = false;
    }
}
