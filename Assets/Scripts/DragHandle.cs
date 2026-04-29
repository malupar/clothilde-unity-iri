// detects the user drag and reports it
using UnityEngine;
using Valve.VR.InteractionSystem;

public class DragHandle : MonoBehaviour
{
    [HideInInspector]
    public TriangleMesh manipulator;
    
    [HideInInspector]
    public int cornerIndex;
    public int nodeIndex;

    private Vector3 offset;
    private float zCoord;

    void OnMouseDown()
    {
        zCoord = Camera.main.WorldToScreenPoint(transform.position).z;

        offset = transform.position - GetMouseWorldPosition();
    }

    void OnMouseUp()
    {
        manipulator.LetHandleGo(nodeIndex);
    }

    void OnMouseDrag()
    {
        Vector3 newWorldPosition = GetMouseWorldPosition() + offset;
        
        transform.position = newWorldPosition;
        
        manipulator.DragHandle(nodeIndex, newWorldPosition);
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord; 

        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    public void HandSelected(Vector3 worldPosition) {
        offset = transform.position - worldPosition;
    }

    public void HandDrop() {
        manipulator.LetHandleGo(nodeIndex);
    }

    public void HandDrag(Vector3 newWorldPosition) {
        manipulator.DragHandle(nodeIndex, newWorldPosition + offset);
    }
}