using UnityEngine;
using Python.Runtime;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TriangleMeshBridge : MonoBehaviour
{
    [Header("Grid Settings")]
    public int numVertexWidth = 20;
    public int numVertexHeight = 20;
    public float gridWidth = 1.0f;
    public float gridHeight = 1.0f;
    public Vector3 originPosition = Vector3.zero;
    public bool doubleSided = true;

    [Header("Simulation Parameters")]
    public int sub_steps = 8;
    public float dt = 1f / 60f;
    public float tol = 0.008f;
    public float rho = 0.1f;
    public float delta = 0.1f;
    public float alpha = 0.3f;
    public float kappa = 1e-4f;
    public float kappa_bnd = 1e-5f;
    public float str = 1e-4f;
    public float shr = 2.0f * 1e-4f;
    public float mu_f = 0.2f;
    public float mu_s = 0.5f;
    public float thck = 1.2f;
    public float slf = 1e-4f;
    public int smooth = 2;

    [Header("Python Gripper")]
    public bool usePythonGripper = true;

    private int[] lastGraspedNodeIds = new int[0];

    public int[] LastGraspedNodeIds
    {
        get { return lastGraspedNodeIds; }
    }

    [Header("Node handles / debug visualization")]
    public bool showNodeHandles = false;
    public bool showAllNodeHandles = true;
    public GameObject handlePrefab;

    private struct NodeHandle
    {
        public Transform transform;
        public int nodeIndex;
    }
    private List<NodeHandle> handles = new List<NodeHandle>();

    private Mesh meshUnity;
    private MeshFilter meshFilter;

    private int N;
    private int numFaces;

    private dynamic meshPython;
    private PythonConnectionBridge connection;

    private bool textureChanged = false;

    void Awake()
    {
        CreateGrid();

        connection = new PythonConnectionBridge(this);
        connection.InitializePython();
        meshPython = connection.ImportClothScript();
    }

    void Update()
    {
        // In the bridge version, GripperBridge calls StepGripperFromUnity(...).
        // Therefore TriangleMeshBridge does not need to simulate anything here.
    }

    public void StepGripperFromUnity(
        Vector3 gripperWorldPosition,
        Quaternion gripperWorldRotation,
        Vector3 unityBoxSize,
        bool closed)
    {
        if (meshPython == null)
        {
            return;
        }

        float[] p = V3ToArray(gripperWorldPosition);
        float[] q = UnityQuatToPythonArray(gripperWorldRotation);

        // Unity box size -> Python box size.
        // Unity y maps to Python z, Unity z maps to Python y.
        float[] box = new float[]
        {
            unityBoxSize.x,
            unityBoxSize.z,
            unityBoxSize.y
        };

        // The address is pinned  because C# uses a moving garbage collector, and without pinning, 
        // the memory address you pass to Python is not stable.
        // In .NET, arrays live on the managed heap, and the garbage collector (GC) 
        // is allowed to move objects around in memory whenever it wants.
        GCHandle pHandle = GCHandle.Alloc(p, GCHandleType.Pinned);
        GCHandle qHandle = GCHandle.Alloc(q, GCHandleType.Pinned);
        GCHandle bHandle = GCHandle.Alloc(box, GCHandleType.Pinned);

        try
        {
            long pPtr = (long)pHandle.AddrOfPinnedObject();
            long qPtr = (long)qHandle.AddrOfPinnedObject();
            long bPtr = (long)bHandle.AddrOfPinnedObject();

            // python receives raw memory pointers 
            meshPython.simulate_gripper(pPtr, qPtr, bPtr, closed);

            // After Python simulates the step, Unity asks Python for grasped node IDs:
            lastGraspedNodeIds =
                meshPython.get_grasped_node_ids()
                    .AsManagedObject(typeof(int[])) as int[];

            if (lastGraspedNodeIds == null)
            {
                lastGraspedNodeIds = new int[0];
            }

            LoadPositionsFromPython();
        }
        finally
        {
            pHandle.Free();
            qHandle.Free();
            bHandle.Free();
        }
    }

    private float[] UnityQuatToPythonArray(Quaternion q)
    {
        // Python Gripper.py expects quaternion as [w, x, y, z].
        // Coordinate mapping follows the same Unity -> Python convention
        // used in V3ToArray.
        return new float[]
        {
            q.w,
            -q.x,
            -q.z,
            -q.y
        };
    }

    private float[] V3ToArray(Vector3 vector)
    {
        return new float[]
        {
            vector.x,
            vector.z,
            vector.y - 1.0f
        };
    }

    private Vector3 ArrayToV3(float[] vector)
    {
        return new Vector3(
            vector[0],
            vector[2] + 1.0f,
            vector[1]
        );
    }

    public Vector3 GetNodeWorldPosition(int nodeIndex)
    {
        if (meshUnity == null)
        {
            return transform.position;
        }

        if (nodeIndex < 0 || nodeIndex >= meshUnity.vertices.Length)
        {
            return transform.position;
        }

        return meshUnity.vertices[nodeIndex];
    }

    void CreateHandles() // Only for visual purposes, DragHandle.cs is not used anymore
    {
        if (!showNodeHandles || handlePrefab == null)
        {
            return;
        }

        handles.Clear();

        List<int> nodeIds = new List<int>();

        if (showAllNodeHandles)
        {
            int numPhysicalNodes = numVertexWidth * numVertexHeight;

            for (int i = 0; i < numPhysicalNodes; i++)
            {
                nodeIds.Add(i);
            }
        }
        else
        {
            nodeIds.Add(0);
            nodeIds.Add(numVertexHeight - 1);
            nodeIds.Add((numVertexWidth - 1) * numVertexHeight);
            nodeIds.Add((numVertexWidth - 1) * numVertexHeight + numVertexHeight - 1);
        }

        foreach (int nodeId in nodeIds)
        {
            GameObject handle = Instantiate(
                handlePrefab,
                meshUnity.vertices[nodeId],
                Quaternion.identity,
                transform.parent
            );

            NodeHandle nh = new NodeHandle();
            nh.transform = handle.transform;
            nh.nodeIndex = nodeId;

            handles.Add(nh);
        }
    }

    private void UpdateHandlePosition(int i, Vector3 newWorldPosition)
    {
        if (i < 0 || i >= handles.Count)
        {
            return;
        }

        if (handles[i].transform != null)
        {
            handles[i].transform.position = newWorldPosition;
        }
    }

    void CreateGrid()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshUnity = new Mesh();

        N =
            numVertexHeight * numVertexWidth
            + (numVertexHeight - 1) * (numVertexWidth - 1);

        Vector3[] vertices = new Vector3[N];
        Vector2[] uv = new Vector2[N];

        if (doubleSided)
        {
            vertices = new Vector3[2 * N];
            uv = new Vector2[2 * N];
        }

        int[,] faces = GetFacesTri();
        int[] triangleIndices = new int[numFaces * 3];

        int cnt = 0;

        // Physical cloth nodes
        for (int x = 0; x < numVertexWidth; x++)
        {
            for (int y = 0; y < numVertexHeight; y++)
            {
                float posX = x;
                float posY = y;

                posX /= (numVertexWidth - 1);
                posY /= (numVertexHeight - 1);

                Vector3 worldPos =
                    originPosition
                    + new Vector3(posX * gridWidth, 1.0f, posY * gridHeight);

                vertices[cnt] = worldPos;
                uv[cnt] = new Vector2(0.0f, 0.0f);

                if (doubleSided)
                {
                    vertices[cnt + N] = worldPos;
                    uv[cnt + N] = new Vector2(posX, posY);
                }

                cnt++;
            }
        }

        // Internal face-center vertices used for rendering
        for (int x = 1; x < numVertexWidth; x++)
        {
            for (int y = 1; y < numVertexHeight; y++)
            {
                float posX = x;
                float posY = y;

                posX -= 0.5f;
                posY -= 0.5f;

                posX /= (numVertexWidth - 1);
                posY /= (numVertexHeight - 1);

                Vector3 worldPos =
                    originPosition
                    + new Vector3(posX * gridWidth, 1.0f, posY * gridHeight);

                vertices[cnt] = worldPos;
                uv[cnt] = new Vector2(1.0f, 1.0f);

                if (doubleSided)
                {
                    vertices[cnt + N] = worldPos;
                    uv[cnt + N] = new Vector2(posX, posY);
                }

                cnt++;
            }
        }

        for (int i = 0; i < numFaces; i++)
        {
            triangleIndices[i * 3] = faces[i, 0];
            triangleIndices[i * 3 + 1] = faces[i, 1];
            triangleIndices[i * 3 + 2] = faces[i, 2];
        }

        meshUnity.vertices = vertices;
        meshUnity.triangles = triangleIndices;
        meshUnity.uv = uv;
        meshUnity.RecalculateNormals();
        meshUnity.RecalculateBounds();

        meshFilter.mesh = meshUnity;

        CreateHandles();
    }

    public float[,] getMeshPositions()
    // quad vertex positions and conversion from unity to python
    // called in PythonConnectionBridge.cs
    {
        Vector3[] vertices = meshUnity.vertices;

        int numPhysicalNodes = numVertexHeight * numVertexWidth;
        float[,] positions = new float[numPhysicalNodes, 3];

        for (int i = 0; i < numPhysicalNodes; i++)
        {
            float[] p = V3ToArray(vertices[i]);

            for (int j = 0; j < 3; j++)
            {
                positions[i, j] = p[j];
            }
        }

        return positions;
    }

        public List<Vector3> getTotalMeshPositions()
        // Get all positions of nodes: quad vertices
    {
        if (meshUnity == null)
        {
            return new List<Vector3>();
        }

        return new List<Vector3>(meshUnity.vertices);
    }

    // public List<Vector3> getRenderMeshPositions()
    // // Get postions of vertices of quads and their centers
    // {
    //     List<Vector3> positions = new List<Vector3>();

    //     if (meshUnity == null)
    //     {
    //         return positions;
    //     }

    //     Vector3[] vertices = meshUnity.vertices;

    //     // Export only the first N render vertices.
    //     // If doubleSided is true, the second N vertices are only duplicate back-side vertices.
    //     for (int i = 0; i < N; i++)
    //     {
    //         float[] p = V3ToArray(vertices[i]);
    //         positions.Add(new Vector3(p[0], p[2] + 1, p[1]));
    //     }

    //     return positions;
    // }



    public int[,] getFaces()
    {
        int cnt = 0;

        int[,] faces =
            new int[(numVertexHeight - 1) * (numVertexWidth - 1), 4];

        for (int i = 0; i < numVertexWidth - 1; i++)
        {
            for (int j = 0; j < numVertexHeight - 1; j++)
            {
                faces[cnt, 0] = i * numVertexHeight + j;
                faces[cnt, 1] = i * numVertexHeight + j + 1;
                faces[cnt, 2] = i * numVertexHeight + j + numVertexHeight + 1;
                faces[cnt, 3] = i * numVertexHeight + j + numVertexHeight;

                cnt++;
            }
        }

        return faces;
    }

    private int[,] GetFacesTri()
    {
        numFaces = (numVertexHeight - 1) * (numVertexWidth - 1) * 8;

        int cnt = 0;
        int cntF = 0;
        int diff = numVertexHeight * numVertexWidth;

        int[,] faces = new int[numFaces, 3];

        // Front side
        for (int i = 0; i < numVertexWidth - 1; i++)
        {
            for (int j = 0; j < numVertexHeight - 1; j++)
            {
                faces[cnt, 0] = i * numVertexHeight + j;
                faces[cnt, 1] = i * numVertexHeight + j + 1;
                faces[cnt, 2] = diff + cntF;
                cnt++;

                faces[cnt, 0] = i * numVertexHeight + j + numVertexHeight;
                faces[cnt, 1] = i * numVertexHeight + j;
                faces[cnt, 2] = diff + cntF;
                cnt++;

                faces[cnt, 0] = i * numVertexHeight + j + 1;
                faces[cnt, 1] = i * numVertexHeight + j + 1 + numVertexHeight;
                faces[cnt, 2] = diff + cntF;
                cnt++;

                faces[cnt, 0] = i * numVertexHeight + j + numVertexHeight + 1;
                faces[cnt, 1] = i * numVertexHeight + j + numVertexHeight;
                faces[cnt, 2] = diff + cntF;
                cnt++;

                cntF++;
            }
        }

        if (!doubleSided)
        {
            return faces;
        }

        // Back side
        cntF = 0;

        for (int i = 0; i < numVertexWidth - 1; i++)
        {
            for (int j = 0; j < numVertexHeight - 1; j++)
            {
                faces[cnt, 0] = i * numVertexHeight + j + 1 + N;
                faces[cnt, 1] = i * numVertexHeight + j + N;
                faces[cnt, 2] = diff + cntF + N;
                cnt++;

                faces[cnt, 1] = i * numVertexHeight + j + numVertexHeight + N;
                faces[cnt, 0] = i * numVertexHeight + j + N;
                faces[cnt, 2] = diff + cntF + N;
                cnt++;

                faces[cnt, 0] = i * numVertexHeight + j + 1 + numVertexHeight + N;
                faces[cnt, 1] = i * numVertexHeight + j + 1 + N;
                faces[cnt, 2] = diff + cntF + N;
                cnt++;

                faces[cnt, 1] = i * numVertexHeight + j + numVertexHeight + 1 + N;
                faces[cnt, 0] = i * numVertexHeight + j + numVertexHeight + N;
                faces[cnt, 2] = diff + cntF + N;
                cnt++;

                cntF++;
            }
        }

        return faces;
    }

    public float[][] GetPhysicalPositionsFromPython()
    {
        if (meshPython == null)
        {
            return null;
        }

        return meshPython.getPhysicalPositionsUnity()
            .AsManagedObject(typeof(float[][])) as float[][];
    }

    private void LoadPositionsFromPython()
    {
        if (meshPython == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            textureChanged = true;
        }

        // Get positions from Python
        float[][] positions =
            meshPython.getPositionsUnity(smooth)
                .AsManagedObject(typeof(float[][])) as float[][];

        if (positions == null)
        {
            return;
        }

        Vector3[] meshVertices;

        if (doubleSided)
        {
            meshVertices = new Vector3[2 * N];
            Vector2[] uv = new Vector2[2 * N];

            for (int i = 0; i < N; i++)
            {
                Vector3 p = ArrayToV3(positions[i]);

                meshVertices[i] = p;
                meshVertices[i + N] = p;

                if (textureChanged)
                {
                    uv[i] = new Vector2(0.0f, 0.0f);
                    uv[i + N] = new Vector2(1.0f, 1.0f);
                }
            }

            if (textureChanged)
            {
                meshUnity.uv = uv;
            }
        }
        else
        {
            meshVertices = new Vector3[N];

            for (int i = 0; i < N; i++)
            {
                meshVertices[i] = ArrayToV3(positions[i]);
            }
        }

        meshUnity.vertices = meshVertices;
        meshUnity.RecalculateNormals();
        meshUnity.RecalculateBounds();

        for (int i = 0; i < handles.Count; i++)
    {
        int nodeIndex = handles[i].nodeIndex;

        if (nodeIndex >= 0 && nodeIndex < meshVertices.Length)
        {
            UpdateHandlePosition(i, meshVertices[nodeIndex]);
        }
    }
    }
}