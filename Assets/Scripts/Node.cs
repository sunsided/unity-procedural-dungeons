using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[DebuggerDisplay("pos: {Position}, parent: {Parent}")]
public sealed class Node
{
    public readonly Vector2 Position;
    public readonly Vector2 Parent;

    public Node(Vector2 position, Vector2 parent)
    {
        this.Position = position;
        this.Parent = parent;
    }
}
