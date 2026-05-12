using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class BoxGripperVR : MonoBehaviour
{
    [Header("Cloth")]
    public TriangleMesh cloth;

    [Header("Box dimensions in local coordinates")]
    public Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Squeeze")]
    public bool enableSqueeze = true;
    public float squeezeAmount = 0.5f; // 0.5 = move halfway toward box center
    public float squeezeAlphaStep = 0.1f; // same logic as in Gripper.py: 0.1, 0.2, 0.3, ..., 1.0
    private float squeezeAlpha = 1.0f;
    // Current local target for each grasped node
    private Dictionary<int, Vector3> localNodeGoalOffsets = new Dictionary<int, Vector3>();
    // Initial unsqueezed local position
    private Dictionary<int, Vector3> localNodeRestOffsets = new Dictionary<int, Vector3>();


    public bool enableKeyboardTranslation = true;
    public bool enableKeyboardRotation = true;

    public float translationSpeed = 0.1f;
    public float rotationSpeed = 50.0f;

    private bool isGrasping = false;
    private List<int> graspedNodes = new List<int>();
    private Dictionary<int, Vector3> localNodeOffsets = new Dictionary<int, Vector3>();

    public SteamVR_Action_Boolean m_GrabAction = null;
    public SteamVR_Action_Single squeezeAction;

    public bool isLeft = true;

    private SteamVR_Behaviour_Pose m_Pose = null;
    private FixedJoint m_Joint = null;

    // To set the scale same as the box size for visualization
    void Awake()
    {
        transform.GetChild(2).transform.localScale = boxSize;
        //transform.position = new Vector3(0, 1, 0);
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        m_Joint = GetComponent<FixedJoint>();
    }
    void Update()
    {
        //Debug.Log("Bola: " + transform.GetChild(1).transform.position);
        if (enableKeyboardTranslation)
        {
            TranslateBoxWithKeyboard();
        }
        if (enableKeyboardRotation)
        {
            RotateBoxWithKeyboard();
        }
        if (Input.GetKeyDown(KeyCode.G) || m_GrabAction.GetStateDown(m_Pose.inputSource))
        {
            if (!isGrasping)
            {
                Debug.Log("G pressed: grasping nodes");
                GraspNodesInsideBox(); 
            }
                
        }

        if (Input.GetKeyDown(KeyCode.R) || m_GrabAction.GetStateUp(m_Pose.inputSource))
        {
            Debug.Log("R pressed: releasing nodes");
            ReleaseNodes();
        }

        if (isGrasping)
        {
            UpdateGraspedNodePositions();
        }
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
           // transform.position += motion.normalized * translationSpeed * Time.deltaTime;
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

    bool IsInsideBox(Vector3 worldPoint)
    {
        //Vector3 localPoint = transform.GetChild(2).InverseTransformPoint(worldPoint);
        Vector3 localPoint = worldPoint - transform.GetChild(2).position;
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(transform.rotation);
        localPoint = rotationMatrix.MultiplyPoint3x4(localPoint);

        Vector3 halfSize = 0.5f * boxSize;
        // divide by boxsize because the scale affects the 
        // InverseTransformPoint function
        return  Mathf.Abs(localPoint.x) <= halfSize.x  &&
                Mathf.Abs(localPoint.y) <= halfSize.y  &&
                Mathf.Abs(localPoint.z) <= halfSize.z;

    }
    void GraspNodesInsideBox()
    {
        ReleaseNodes();

        if (cloth == null)
        {
            Debug.LogError("BoxGripper: TriangleMesh reference is missing.");
            return;
        }

        HashSet<int> selectNodes = new HashSet<int>();

        List<TriangleMesh.GraspCandidate> candidates = cloth.GetAllGraspCandidates();

        //for printing
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

        foreach (int nodeIndex in selectNodes)
        {
            Vector3 nodeWorldPosition = cloth.GetNodeWorldPosition(nodeIndex);

        // SQUEEZE:
            // Store the node in the gripper local frame at the moment of grasp.
            Vector3 restLocal = transform.InverseTransformPoint(nodeWorldPosition);

            Vector3 goalLocal = restLocal;
            if (enableSqueeze)
            {
                // box center is x = 0 in local frame
                float dx = 0.0f - goalLocal.x;
                goalLocal.x += squeezeAmount * dx;
            }

            graspedNodes.Add(nodeIndex);

            // need this to send offsets for control in the next function
            localNodeRestOffsets[nodeIndex] = restLocal;
            localNodeGoalOffsets[nodeIndex] = goalLocal;

            // Start from the unsqueezed position.
            localNodeOffsets[nodeIndex] = restLocal;

        }

        squeezeAlpha = 0.0f;
        isGrasping = graspedNodes.Count > 0;

        Debug.Log("Grasped nodes: " + graspedNodes.Count);
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

            // Convert the saved local offset back to world frame
            Vector3 newWorldPosition = transform.TransformPoint(localOffset);

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
        isGrasping = false;
    }
}