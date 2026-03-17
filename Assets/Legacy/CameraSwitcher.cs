using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera cameraA;
    public Camera cameraB;

    void Start()
    {
        if (cameraA != null) cameraA.enabled = true;
        if (cameraB != null) cameraB.enabled = false;
    }

    public void SwitchToCameraB()
    {
        if (cameraA != null) cameraA.enabled = false;
        if (cameraB != null) cameraB.enabled = true;
    }

    public void SwitchToCameraA()
    {
        if (cameraA != null) cameraA.enabled = true;
        if (cameraB != null) cameraB.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (cameraA.enabled)
            {
                SwitchToCameraB();
            }
            else
            {
                SwitchToCameraA();
            }
        }
    }
}