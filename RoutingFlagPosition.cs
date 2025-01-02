using UnityEngine;

public class RoutingFlagPosition
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public RoutingFlagPosition(Vector3 position, Vector3 rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}