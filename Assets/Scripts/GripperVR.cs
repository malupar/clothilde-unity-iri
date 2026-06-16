using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class GripperVR : MonoBehaviour
{
    [Header("Cloth")]
    public TriangleMesh cloth;

    [Header("Grasp frame")]
    public Transform graspFrame;
    public Vector3 controllerLocalOffset = new Vector3(0.0f, 0.0f, 0.2f);
    public Quaternion controllerOrientation = new Quaternion(0.5f, -0.5f, 0.5f, 0.5f);

    [Header("Grasp box visual")]
    public Transform graspBoxVisual;
    public Vector3 graspBoxSize = new Vector3(0.06f, 0.06f, 0.06f);

    [Header("Visual gripper assembly")]
    public Assembly gripperAssembly;
    public float gripperAssemblyScale = 0.06f;

    // Distance from grasp box center to CAD gripper root.
    // Change sign/direction depending on your CAD orientation.
    public float t = 0.06f;
    public Vector3 gripperAssemblyOffsetDirection = new Vector3(-1.7f, 1.35f, -1.7f); // by moving the assembly to match the box in #Scene

    [Header("Squeeze")]
    public bool enableSqueeze = true;
    public float squeezeAmount = 0.5f;
    public float squeezeAlphaStep = 0.1f;
    private float squeezeAlpha = 1.0f;

    private bool isGrasping = false;
    private List<int> graspedNodes = new List<int>();

    private Dictionary<int, Vector3> localNodeOffsets = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> localNodeRestOffsets = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> localNodeGoalOffsets = new Dictionary<int, Vector3>();

    public SteamVR_Action_Boolean m_GrabAction = null;
    private SteamVR_Behaviour_Pose m_Pose = null;
    private FixedJoint m_Joint = null;

    [Header("Initial pose")]
    public Vector3 initialBoxCenter = new Vector3(0.0f, 0.0f, 0.0f);

    void Awake()
    {
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        m_Joint = GetComponent<FixedJoint>();

        graspFrame.localPosition = controllerLocalOffset;
        graspFrame.localRotation = controllerOrientation;

        graspBoxVisual.localPosition = Vector3.zero;
        graspBoxVisual.localScale = graspBoxSize;

        gripperAssembly.transform.localPosition = gripperAssemblyOffsetDirection * t;
        gripperAssembly.transform.localScale = Vector3.one * gripperAssemblyScale;

    }


    void Update()
    {
        if (m_GrabAction.GetStateDown(m_Pose.inputSource))
        {
            if (!isGrasping)
            {
                if (gripperAssembly != null)
                {
                    gripperAssembly.Close();
                }
                Debug.Log("Grasping nodes");
                GraspNodesInsideBox(); 
            }   
        }

        if (m_GrabAction.GetStateUp(m_Pose.inputSource))
        {
            if (gripperAssembly != null)
                {
                    gripperAssembly.Open();
                }
            Debug.Log("Releasing nodes");
            ReleaseNodes();
        }

        if (isGrasping)
        {
            UpdateGraspedNodePositions();
        }
    }

    bool IsInsideBox(Vector3 worldPoint)
    {
        // Since this GameObject is the graspFrame,
        // transform.InverseTransformPoint gives the point in grasp-box coordinates.
        Vector3 localPoint = graspFrame.InverseTransformPoint(worldPoint);

        Vector3 halfSize = 0.5f * graspBoxSize;

        return Mathf.Abs(localPoint.x) <= halfSize.x &&
               Mathf.Abs(localPoint.y) <= halfSize.y &&
               Mathf.Abs(localPoint.z) <= halfSize.z;
    }

    void GraspNodesInsideBox()
    {
        ReleaseNodes();

        HashSet<int> selectNodes = new HashSet<int>();

        List<TriangleMesh.GraspCandidate> candidates = cloth.GetAllGraspCandidates();

        // for printing
        System.Text.StringBuilder table = new System.Text.StringBuilder();

        table.AppendLine("========== SELECTED GRASP CANDIDATES ==========");
        table.AppendLine("Type\t\tID\tPosition\t\t\tNodes");
        table.AppendLine("-----------------------------------------------");

        foreach (TriangleMesh.GraspCandidate candidate in candidates)
        {
            if (IsInsideBox(candidate.worldPosition))
            {
                foreach (int nodeIndex in candidate.nodeIndices)
                    {
                    selectNodes.Add(nodeIndex);
                    }

                    table.AppendLine(candidate.type + "\t" + 
                            candidate.id + "\t" +
                            candidate.worldPosition + "\t" +
                            string.Join(", ", candidate.nodeIndices));

            }
        }

        table.AppendLine("-----------------------------------------------");
        table.AppendLine("Unique selected nodes: " + selectNodes.Count);
        table.AppendLine("Selected node indices: " + string.Join(", ", selectNodes));
        Debug.Log(table.ToString());

        //

        foreach (TriangleMesh.GraspCandidate candidate in candidates)
        {
            if (IsInsideBox(candidate.worldPosition))
            {
                foreach (int nodeIndex in candidate.nodeIndices)
                {
                    selectNodes.Add(nodeIndex);
                }
            }
        }

        foreach (int nodeIndex in selectNodes)
        {
            Vector3 nodeWorldPosition = cloth.GetNodeWorldPosition(nodeIndex);

            // Store unsqueezed node position in gripper/box local frame.
            Vector3 restLocal = graspFrame.InverseTransformPoint(nodeWorldPosition);

            // Build squeezed goal.
            Vector3 goalLocal = restLocal;

            if (enableSqueeze)
            {
                // Box center is local x = 0.
                float dx = 0.0f - goalLocal.x;
                goalLocal.x += squeezeAmount * dx;
            }

            graspedNodes.Add(nodeIndex);

            localNodeRestOffsets[nodeIndex] = restLocal;
            localNodeGoalOffsets[nodeIndex] = goalLocal;
            localNodeOffsets[nodeIndex] = restLocal;
        }

        squeezeAlpha = 0.0f;
        isGrasping = graspedNodes.Count > 0;

        Debug.Log("VR grasped nodes: " + graspedNodes.Count);
    }

    void UpdateGraspedNodePositions()
    {
        if (squeezeAlpha < 1.0f)
        {
            squeezeAlpha = Mathf.Min(1.0f, squeezeAlpha + squeezeAlphaStep);
        }

        float a = squeezeAlpha;

        foreach (int nodeIndex in graspedNodes)
        {
            Vector3 restLocal = localNodeRestOffsets[nodeIndex];
            Vector3 goalLocal = localNodeGoalOffsets[nodeIndex];

            // Python style:
            // local_points = (1-a) * rest + a * goal
            Vector3 localOffset = (1.0f - a) * restLocal + a * goalLocal;

            localNodeOffsets[nodeIndex] = localOffset;

            Vector3 newWorldPosition = graspFrame.TransformPoint(localOffset);

            cloth.DragHandle(nodeIndex, newWorldPosition);

        }
    }

    void ReleaseNodes()
    {
        if (cloth != null)
        {
            foreach (int nodeIndex in graspedNodes)
            {
                cloth.LetHandleGo(nodeIndex);

            }
        }

        graspedNodes.Clear();
        localNodeOffsets.Clear();
        localNodeRestOffsets.Clear();
        localNodeGoalOffsets.Clear();

        squeezeAlpha = 1.0f;
        isGrasping = false;
    }
}