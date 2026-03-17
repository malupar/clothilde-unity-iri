using UnityEngine;
using System;

public class GridMouseDrag : MonoBehaviour
{
    public MeshQuad gridManager;
    private Plane gridPlane;

    private Vector3 offset;
    private float zCoord;
    private GameObject selectedFace = null;
    private Tuple<int, int> selectedCoord;

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<MeshQuad>();
        }
        gridPlane = new Plane(Vector3.up, gridManager.originPosition);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectedFace = GetNodeFromMouse();
            if (selectedFace != null)
            {
                zCoord = Camera.main.WorldToScreenPoint(selectedFace.transform.position).z;
                offset = selectedFace.transform.position - GetMouseWorldPos();
            }
        }

        if (Input.GetMouseButton(0) && selectedFace != null)
        {
            Vector3 targetPos = GetMouseWorldPos() + offset;
            gridManager.registerNode(selectedCoord.Item1, selectedCoord.Item2, targetPos);
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectedFace = null;
            gridManager.unregisterNode(selectedCoord.Item1, selectedCoord.Item2);
        }
    }

    private GameObject GetNodeFromMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (gridPlane.Raycast(ray, out float enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);
            
            int x = Mathf.FloorToInt((worldPoint.x - gridManager.originPosition.x) / gridManager.cellSize);
            int y = Mathf.FloorToInt((worldPoint.z - gridManager.originPosition.z) / gridManager.cellSize);
            Debug.Log("Found node with " + x + " " + y);
            selectedCoord = Tuple.Create(x, y);
            return gridManager.GetNode(x, y);
        }

        return null; 
    }

    private bool MoveNodeFromMouse(Vector3 newPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (gridPlane.Raycast(ray, out float enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);
            
            int x = Mathf.FloorToInt((worldPoint.x - gridManager.originPosition.x) / gridManager.cellSize);
            int y = Mathf.FloorToInt((worldPoint.z - gridManager.originPosition.z) / gridManager.cellSize);
            
            gridManager.MoveNode(x, y, newPosition);
            return true;
        }

        return false; 
    }
}