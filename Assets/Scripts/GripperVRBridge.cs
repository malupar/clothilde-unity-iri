using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class GripperVRBridge : MonoBehaviour
{
    [Header("Cloth")]
    public TriangleMeshBridge cloth;

    [Header("Grasp frame")]
    public Transform graspFrame;
    public Vector3 controllerLocalOffset = new Vector3(0.0f, 0.0f, 0.2f);

    [Header("Grasp box visual")]
    public Transform graspBoxVisual;
    public Vector3 graspBoxSize = new Vector3(0.06f, 0.06f, 0.06f);

    [Header("Visual gripper assembly")]
    public Assembly gripperAssembly;
    public float gripperAssemblyScale = 0.06f;

    [Header("Visual gripper offset")]
    public float gripperOffset = 0.06f;
    public Vector3 gripperAssemblyOffsetDirection =
        new Vector3(-1.7f, 1.35f, -1.7f);

    [Header("Grasped node markers")]
    public bool showGraspedNodeMarkers = true;
    // public float markerScale = 0.015f;

    public bool IsClosed { get; private set; } = false;

    private Dictionary<int, GameObject> graspMarkers =
        new Dictionary<int, GameObject>();

    [Header("VR input")]
    public SteamVR_Action_Boolean m_GrabAction = null;
    private SteamVR_Behaviour_Pose m_Pose = null;
    private FixedJoint m_Joint = null;

    void Awake()
    {
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        m_Joint = GetComponent<FixedJoint>();

        // if (graspFrame == null)
        // {
        //     graspFrame = transform;
        // }
    
        graspFrame.localPosition = controllerLocalOffset;

        graspBoxVisual.localPosition = Vector3.zero;
        graspBoxVisual.localScale = graspBoxSize;

        gripperAssembly.transform.localPosition = gripperAssemblyOffsetDirection * gripperOffset;
        gripperAssembly.transform.localScale = Vector3.one * gripperAssemblyScale;
    }

    void Update()
    {
        HandleVRGrabInput();

        if (cloth != null && graspFrame != null)
        {
            cloth.StepGripperFromUnity(
                GetInstanceID(),
                graspFrame.position,
                graspFrame.rotation,
                graspBoxSize,
                IsClosed
            );

            // if (showGraspedNodeMarkers)
            // {
            //     RenderGraspedNodes(cloth.LastGraspedNodeIds);
            // }
            // else
            // {
            //     ClearMarkers();
            // }
        }
    }

    void HandleVRGrabInput()
    {
        if (m_GrabAction.GetStateDown(m_Pose.inputSource))
        {
            IsClosed = true;

            if (gripperAssembly != null)
            {
                gripperAssembly.Close();
            }

            Debug.Log("VR grip pressed: Python gripper closed.");
        }

        if (m_GrabAction.GetStateUp(m_Pose.inputSource))
        {
            IsClosed = false;

            if (gripperAssembly != null)
            {
                gripperAssembly.Open();
            }

            Debug.Log("VR grip released: Python gripper opened.");
        }
    }

}