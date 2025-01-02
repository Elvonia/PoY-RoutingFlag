using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoutingFlagData
{
    public List<string> keys = new List<string>();
    public List<Vector3> positions = new List<Vector3>();
    public List<Vector3> rotations = new List<Vector3>();

    public void FromDictionary(Dictionary<string, RoutingFlagPosition> dictionary)
    {
        keys.Clear();
        positions.Clear();
        rotations.Clear();

        foreach (var kvp in dictionary)
        {
            keys.Add(kvp.Key);
            positions.Add(kvp.Value.Position);
            rotations.Add(kvp.Value.Rotation);
        }
    }

    public Dictionary<string, RoutingFlagPosition> ToDictionary()
    {
        Dictionary<string, RoutingFlagPosition> dictionary = new Dictionary<string, RoutingFlagPosition>();
        for (int i = 0; i < keys.Count; i++)
        {
            dictionary[keys[i]] = new RoutingFlagPosition(positions[i], rotations[i]);
        }
        return dictionary;
    }
}