using UnityEngine;

public class Node
{
    public int x;
    public int y;
    public Vector3 worldPosition;
    
    public Node(int gridX, int gridY, Vector3 worldPos)
    {
        x = gridX;
        y = gridY;
        worldPosition = worldPos;
    }
}