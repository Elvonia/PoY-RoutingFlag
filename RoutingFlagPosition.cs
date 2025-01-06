using UnityEngine;

public class RoutingFlagPosition
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public float CameraHolderRotationY { get; set; }
    public float CameraHolderRotationW { get; set; }
    public float CameraLookRotationY { get; set; }

    public RoutingFlagPosition(
        Vector3 position, 
        Vector3 rotation, 
        float cameraHolderRotationY, 
        float cameraHolderRotationW, 
        float cameraLookRotationY
        )
    {
        Position = position;
        Rotation = rotation;
        CameraHolderRotationY = cameraHolderRotationY;
        CameraHolderRotationW = cameraHolderRotationW;
        CameraLookRotationY = cameraLookRotationY;
    }
}