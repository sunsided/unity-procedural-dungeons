using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
///     An A* node.
/// </summary>
[DebuggerDisplay("pos: {Position}, parent: {Parent}, score: {Score}")]
public sealed class Node
{
    /// <summary>
    ///     The node's position.
    /// </summary>
    public readonly Vector2 Position;

    /// <summary>
    ///    The node's parent.
    /// </summary>
    public readonly Node Parent;

    /// <summary>
    ///     The Manhattan distance from the start, or:
    ///     The number of steps taken to reach this position, or:
    ///     The G-score.
    /// </summary>
    public readonly float DistanceFromStart;

    /// <summary>
    ///     The Manhattan distance heuristic from this node ot the target, or:
    ///     The estimated number of steps yet to take, or:
    ///     The H-score.
    /// </summary>
    public readonly float DistanceHeuristicToTarget;

    /// <summary>
    ///     The combined score of this node, or:
    ///     The H-score.
    /// </summary>
    /// <seealso cref="DistanceFromStart"/>
    /// <seealso cref="DistanceHeuristicToTarget"/>
    public readonly float Score;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Node"/> class.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="parent">The parent.</param>
    /// <param name="gScore">
    ///     The Manhattan distance from the start, or:
    ///     The number of steps taken to reach this position.
    /// </param>
    /// <param name="hScore">
    ///     The Manhattan distance heuristic from this node ot the target, or:
    ///     The estimated number of steps yet to take.
    /// </param>
    public Node(Vector2 position, [CanBeNull] Node parent, float gScore, float hScore)
    {
        Position = position;
        Parent = parent;
        DistanceFromStart = gScore;
        DistanceHeuristicToTarget = hScore;
        Score = gScore + hScore;
    }
}
