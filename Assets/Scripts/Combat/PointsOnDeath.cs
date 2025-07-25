using UnityEngine;

/// <summary>
/// A simple component that holds the point value awarded when this object is destroyed.
/// </summary>
public class PointsOnDeath : MonoBehaviour
{
    [Tooltip("The number of points awarded for destroying this object.")]
    [SerializeField] private int points = 10;

    public int GetPoints()
    {
        return points;
    }
}
