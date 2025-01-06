using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoutingFlagData
{
    public List<string> keys = new List<string>();

    public List<Vector3> flagPositions = new List<Vector3>();
    public List<Vector3> flagRotations = new List<Vector3>();

    public List<float> holderY = new List<float>();
    public List<float> holderW = new List<float>();
    public List<float> cameraY = new List<float>();

    public void FromDictionary(Dictionary<string, RoutingFlagPosition> dictionary)
    {
        keys.Clear();
        flagPositions.Clear();
        flagRotations.Clear();
        holderY.Clear();
        holderW.Clear();
        cameraY.Clear();

        foreach (KeyValuePair<string, RoutingFlagPosition> kvp in dictionary)
        {
            keys.Add(kvp.Key);
            flagPositions.Add(kvp.Value.Position);
            flagRotations.Add(kvp.Value.Rotation);
            holderY.Add(kvp.Value.CameraHolderRotationY);
            holderW.Add(kvp.Value.CameraHolderRotationW);
            cameraY.Add(kvp.Value.CameraLookRotationY);
        }
    }

    public Dictionary<string, RoutingFlagPosition> ToDictionary()
    {
        Dictionary<string, RoutingFlagPosition> dictionary = new Dictionary<string, RoutingFlagPosition>();
        for (int i = 0; i < keys.Count; i++)
        {
            dictionary[keys[i]] = new RoutingFlagPosition(flagPositions[i], flagRotations[i], holderY[i], holderW[i], cameraY[i]);
        }
        return dictionary;
    }
}