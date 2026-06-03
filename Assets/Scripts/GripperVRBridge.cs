using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class GripperVRBridge : MonoBehaviour
{
    [Header("Cloth")]
    public TriangleMeshBridge cloth;

    [Header("VR input")]
    public SteamVR_Action_Boolean m_GrabAction = null;
    private SteamVR_Behaviour_Pose m_Pose = null;

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
    public float markerScale = 0.015f;

    public bool IsClosed { get; private set; } = false;

    private Dictionary<int, GameObject> graspMarkers =
        new Dictionary<int, GameObject>();

    void Awake()
    {
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();

        if (graspFrame == null)
        {
            graspFrame = transform;
        }

        if (graspFrame != transform)
        {
            graspFrame.localPosition = controllerLocalOffset;
            graspFrame.localRotation = Quaternion.identity;
        }

        if (graspBoxVisual != null)
        {
            graspBoxVisual.localPosition = Vector3.zero;
            graspBoxVisual.localRotation = Quaternion.identity;
            graspBoxVisual.localScale = graspBoxSize;
        }

        if (gripperAssembly != null)
        {
            gripperAssembly.transform.localPosition =
                gripperAssemblyOffsetDirection.normalized * gripperOffset;

            gripperAssembly.transform.localRotation = Quaternion.identity;
            gripperAssembly.transform.localScale =
                Vector3.one * gripperAssemblyScale;
        }
    }

    void Update()
    {
        HandleVRGrabInput();

        if (cloth != null && graspFrame != null)
        {
            cloth.StepGripperFromUnity(
                graspFrame.position,
                graspFrame.rotation,
                graspBoxSize,
                IsClosed
            );

            if (showGraspedNodeMarkers)
            {
                RenderGraspedNodes(cloth.LastGraspedNodeIds);
            }
            else
            {
                ClearMarkers();
            }
        }
    }

    void HandleVRGrabInput()
    {
        if (m_GrabAction == null || m_Pose == null)
        {
            return;
        }

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

            ClearMarkers();

            Debug.Log("VR grip released: Python gripper opened.");
        }
    }

    void RenderGraspedNodes(int[] nodeIds)
    {
        if (nodeIds == null || cloth == null)
        {
            ClearMarkers();
            return;
        }

        HashSet<int> live = new HashSet<int>(nodeIds);
        List<int> toRemove = new List<int>();

        foreach (var kv in graspMarkers)
        {
            if (!live.Contains(kv.Key))
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }

                toRemove.Add(kv.Key);
            }
        }

        foreach (int id in toRemove)
        {
            graspMarkers.Remove(id);
        }

        foreach (int id in live)
        {
            if (!graspMarkers.ContainsKey(id))
            {
                GameObject marker =
                    GameObject.CreatePrimitive(PrimitiveType.Sphere);

                marker.name = "PythonVRGraspedNode_" + id;
                marker.transform.localScale = Vector3.one * markerScale;

                Collider markerCollider = marker.GetComponent<Collider>();
                if (markerCollider != null)
                {
                    Destroy(markerCollider);
                }

                graspMarkers[id] = marker;
            }

            graspMarkers[id].transform.position =
                cloth.GetNodeWorldPosition(id);
        }
    }

    void ClearMarkers()
    {
        foreach (var kv in graspMarkers)
        {
            if (kv.Value != null)
            {
                Destroy(kv.Value);
            }
        }

        graspMarkers.Clear();
    }

    void OnDisable()
    {
        IsClosed = false;
        ClearMarkers();

        if (gripperAssembly != null)
        {
            gripperAssembly.Open();
        }
    }
}