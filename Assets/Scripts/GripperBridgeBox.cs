using System.Collections.Generic;
using UnityEngine;

public class GripperBridgeBox : MonoBehaviour
{
    [Header("Cloth")]
    public TriangleMeshBridge cloth;

    [Header("Keyboard control")]
    public bool enableKeyboardTranslation = true;
    public bool enableKeyboardRotation = true;
    public float translationSpeed = 0.2f;
    public float rotationSpeed = 50.0f;
    
    [Header("Initial pose")]
    public Vector3 initialBoxCenter = new Vector3(0.0f, 1.0f, 0.0f);

    [Header("Grasp box visual")]
    public Transform graspBoxVisual;
    public Vector3 graspBoxSize = new Vector3(0.04f, 0.04f, 0.04f);

    public bool IsClosed { get; private set; } = false;

    void Awake()
    {
        // The Gripper object itself is the grasp box center.
        transform.position = initialBoxCenter;
        transform.rotation = Quaternion.identity;

        // Visual box used to show the grasp region.
        if (graspBoxVisual != null)
        {
            graspBoxVisual.localPosition = Vector3.zero;
            graspBoxVisual.localRotation = Quaternion.identity;
            graspBoxVisual.localScale = graspBoxSize;
        }

    }

    void Update()
    {
        if (enableKeyboardTranslation)
        {
            TranslateBoxWithKeyboard();
        }

        if (enableKeyboardRotation)
        {
            RotateBoxWithKeyboard();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            IsClosed = true;

            Debug.Log("G pressed: Python gripper closed.");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            IsClosed = false;

            Debug.Log("R pressed: Python gripper opened.");
        }

        StepPythonOnce();
    }

    void TranslateBoxWithKeyboard()
    {
        Vector3 motion = new Vector3(0, 0, 0);

        // x-direction
        if (Input.GetKey(KeyCode.J))
        {
            motion.x -= 1.0f;
        }
        if (Input.GetKey(KeyCode.L))
        {
            motion.x += 1.0f;
        }
        // z-direction
        if (Input.GetKey(KeyCode.I))
        {
            motion.z -= 1.0f;
        }
        if (Input.GetKey(KeyCode.K))
        {
            motion.z += 1.0f;
        }
        // Y-direction
        if (Input.GetKey(KeyCode.U))
        {
            motion.y -= 1.0f;
        }
        if (Input.GetKey(KeyCode.O))
        {
            motion.y += 1.0f;
        }
         if (motion.sqrMagnitude > 0.0f)
        {
           transform.position += motion.normalized * translationSpeed * Time.deltaTime;
        }

    }

    void RotateBoxWithKeyboard()
    {
        float angle = rotationSpeed * Time.deltaTime;

        // Yaw: rotate around world Y axis
        if (Input.GetKey(KeyCode.N))
        {
            transform.Rotate(Vector3.up, -angle, Space.Self);
        }

        if (Input.GetKey(KeyCode.M))
        {
            transform.Rotate(Vector3.up, angle, Space.Self);
        }

        // Pitch: rotate around box local X axis
        if (Input.GetKey(KeyCode.B))
        {
            transform.Rotate(Vector3.right, -angle, Space.Self);
        }

        if (Input.GetKey(KeyCode.V))
        {
            transform.Rotate(Vector3.right, angle, Space.Self);
        }

        // Roll: rotate around box local Z axis
        if (Input.GetKey(KeyCode.Comma))
        {
            transform.Rotate(Vector3.forward, -angle, Space.Self);
        }

        if (Input.GetKey(KeyCode.Period))
        {
            transform.Rotate(Vector3.forward, angle, Space.Self);
        }
    }

    void StepPythonOnce()
    {
        if (cloth == null)
            return;

        cloth.StepGripperFromUnity(
            GetInstanceID(),
            transform.position,
            transform.rotation,
            graspBoxSize,
            IsClosed
        );
    }
}