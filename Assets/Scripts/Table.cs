using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour
{
    [SerializeField] private string targetName = "MeshRenderer";
    public Vector3 offset;
    public float mu = 0.3f;
    public Vector3 dimensions;
    public bool generateTable = false;

    void Start()
    {
        PositionTable();
    }

    public void PositionTable()
    {
        GameObject targetObj = GameObject.Find(targetName);

        TriangleMesh mesh = targetObj.GetComponent<TriangleMesh>();
        Renderer targetRenderer = targetObj.GetComponent<Renderer>();
        Renderer tableRenderer = GetComponent<Renderer>();
        float tableHalfHeight = 0f;

        if (tableRenderer != null)
        {
            tableHalfHeight = tableRenderer.bounds.extents.y;
        }


        float meshTopY = targetRenderer.bounds.max.y;

        Vector3 newPosition = new Vector3(
            targetObj.transform.position.x + mesh.gridWidth/2,
            meshTopY - tableHalfHeight - 0.005f,                // Restamos poco para que las texturas no se
            targetObj.transform.position.z + mesh.gridHeight/2
        );

        newPosition += offset;

        transform.position = newPosition;

        Debug.Log($"[Table] Successfully placed {gameObject.name} above {targetName}.");
    }
}