using UnityEngine;
using Python.Runtime;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
// using System.Numerics;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TriangleMesh : MonoBehaviour
{
#region Parameters
    [Header("Grid Settings")]
    public int numVertexWidth = 21;
    public int numVertexHeight = 21;
    public float gridWidth = 0.7f;
    public float gridHeight = 0.7f;
    public Vector3 originPosition = Vector3.zero;
    public bool doubleSided = true;
    public bool allHandles = true;

    //Runtime modifiable parameters
    public int sub_steps = 8;
    // public int numIter = 1;
    public float dt = 1f/60f;
    public float tol = 0.008f;
    public float rho = 0.1f;
    public float delta = 0.1f;
    public float alpha = 0.3f;
    public float kappa = 1e-4f;
    public float kappa_bnd = 1e-5f;
    public float str = 1e-4f;
    public float shr = 2.0f*1e-4f;
    public float mu_f = 0.2f;
    public float mu_s = 0.5f;
    public float thck = 1.1f;
    public bool selfCollisions = true;
    public int smooth = 2;
    public float slf = 1e-4f;

#endregion

    // Mesh objects
    private Mesh meshUnity;
    private MeshFilter meshFilter;
    private int N;
    private int numFaces;

    // Handle objects
    int numHandles = 4;
    public GameObject handlePrefab;
    private List<DragHandle> handles = new List<DragHandle>(); 
    private Dictionary<int, Vector3> simulated;

    // Python connection
    // dynamic is used because the object comes from Python, 
    // so C# does not know its type at compile time. dynamic 
    // allows the script to call methods on the Python object 
    // without a fixed C# type.
    private dynamic meshPython;
    private PythonConnection connection;

    // Saved parameters
    private bool textureChanged = false;

    // interpolation between current and target positions
    private Dictionary<int, Vector3> previousControlTargets = new Dictionary<int, Vector3>();


    // containeer describing possible grasp point on cloth
    public struct GraspCandidate
{
    public Vector3 worldPosition; // of the node or edge midpoint or quad center
    public int[] nodeIndices; // of the corresponding nodes
    public string type; // node, edge_midpoint, quad_center
    public int id; // node index, edge number, face number

    public GraspCandidate(Vector3 worldPosition, int[] nodeIndices, string type, int id)
    {
        this.worldPosition = worldPosition;
        this.nodeIndices = nodeIndices;
        this.type = type;
        this.id = id;
    }
}
    // Last Python positions in Python coordinates.
    // Shape is N x 3, where N = raw nodes + face-center render vertices.
    // This is the source for picking/export, not meshUnity.vertices.
    private double[][] latestPythonPositions = null;

#region Graspbox
    public Vector3 GetNodeWorldPosition(int nodeIndex)
    {
        if (meshUnity == null)
        {
            return transform.position;
        }

        return meshUnity.vertices[nodeIndex]; // ok here since it is only used for redering
    }


    // Logic to grasp nodes inside the grasp box

    public List<GraspCandidate> GetAllGraspCandidates()
    {
        List<GraspCandidate> candidates = new List<GraspCandidate>();

        candidates.AddRange(GetNodeCandidates());
        candidates.AddRange(GetQuadCenterCandidates());
        candidates.AddRange(GetEdgeCenterCandidates());

        return candidates;
    }

    public List<GraspCandidate> GetNodeCandidates()
    {
        List<GraspCandidate> candidates = new List<GraspCandidate>();
        int numNodes = numVertexWidth * numVertexHeight;

        for (int i = 0; i < numNodes; i++)
        {
            Vector3 p = GetNodeWorldPosition(i);
            int[] nodes = new int[] {i};
            candidates.Add(new GraspCandidate(p, nodes, "node", i));
        }

        return candidates;
    }

    public List<GraspCandidate> GetQuadCenterCandidates()
    {
        List<GraspCandidate> candidates = new List<GraspCandidate>();

        int[,] faces = getFaces();
        int numFaces = faces.GetLength(0);

        for (int i = 0; i < numFaces; i++)
        {
            int n0 = faces[i, 0];
            int n1 = faces[i, 1];
            int n2 = faces[i, 2];
            int n3 = faces[i, 3];

            Vector3 p0 = GetNodeWorldPosition(n0);
            Vector3 p1 = GetNodeWorldPosition(n1);
            Vector3 p2 = GetNodeWorldPosition(n2);
            Vector3 p3 = GetNodeWorldPosition(n3);

            Vector3 p = 0.25f * (p0 + p1 + p2 + p3);
            int[] nodes = new int[] {n0, n1, n2, n3};
            candidates.Add(new GraspCandidate(p, nodes, "quad_center", i));
        }

        return candidates;
    }

    public List<GraspCandidate> GetEdgeCenterCandidates()
    {
        List<GraspCandidate> candidates = new List<GraspCandidate>();

        int[,] edges = getEdges();
        int numEdges = edges.GetLength(0);

        for (int i = 0; i < numEdges; i++)
        {
            int n0 = edges[i, 0];
            int n1 = edges[i, 1];

            Vector3 p0 = GetNodeWorldPosition(n0);
            Vector3 p1 = GetNodeWorldPosition(n1);

            Vector3 p = 0.5f * (p0 + p1);
            int[] nodes = new int[] {n0, n1};
            candidates.Add(new GraspCandidate(p, nodes, "edge_midpoint", i));
        }

        return candidates;
    }

        int[,] getFacesTri() {
        numFaces = (numVertexHeight-1)*(numVertexWidth-1)*8;
        int cnt = 0, cntF = 0, diff = numVertexHeight*numVertexWidth;
        int[,] faces = new int[numFaces, 3];
        // Debug.Log("Numero caras: " + numFaces);
        for (int i = 0; i < numVertexWidth-1; ++i) {
            for (int j = 0; j < numVertexHeight-1; ++j) {
                faces[cnt,0] = i*numVertexHeight+j; // LD
                faces[cnt,1] = i*numVertexHeight+j+1; // LU
                faces[cnt,2] = diff+cntF;
                cnt += 1;
                faces[cnt,0] = i*numVertexHeight+j+numVertexHeight; // RD
                faces[cnt,1] = i*numVertexHeight+j; // LD
                faces[cnt,2] = diff+cntF;
                cnt += 1;
                faces[cnt,0] = i*numVertexHeight+j+1; //LU
                faces[cnt,1] = i*numVertexHeight+j+1+numVertexHeight; // RU
                faces[cnt,2] = diff+cntF;
                cnt += 1;
                faces[cnt,0] = i*numVertexHeight+j+numVertexHeight+1; // RU
                faces[cnt,1] = i*numVertexHeight+j+numVertexHeight; //RD
                faces[cnt,2] = diff+cntF;
                cnt += 1;
                cntF += 1;
            }
        }
        cntF = 0;
        for (int i = 0; i < numVertexWidth-1; ++i) {
            for (int j = 0; j < numVertexHeight-1; ++j) {
                faces[cnt,0] = i*numVertexHeight+j+1 + N;
                faces[cnt,1] = i*numVertexHeight+j + N;
                faces[cnt,2] = diff+cntF + N;
                cnt += 1;
                faces[cnt,1] = i*numVertexHeight+j+numVertexHeight + N;
                faces[cnt,0] = i*numVertexHeight+j + N;
                faces[cnt,2] = diff+cntF + N;
                cnt += 1;
                faces[cnt,0] = i*numVertexHeight+j+1+numVertexHeight + N;
                faces[cnt,1] = i*numVertexHeight+j+1 + N;
                faces[cnt,2] = diff+cntF + N;
                cnt += 1;
                faces[cnt,1] = i*numVertexHeight+j+numVertexHeight+1 + N;
                faces[cnt,0] = i*numVertexHeight+j+numVertexHeight + N;
                faces[cnt,2] = diff+cntF + N;
                cnt += 1;
                cntF += 1;
            }
        }
        return faces;
    }

    // four corners of a face: [face id, node ids]
    public int[,] getFaces() {
        int cnt = 0;
        int[,] faces = new int[(numVertexHeight-1)*(numVertexWidth-1), 4];
        for (int i = 0; i < numVertexWidth-1; ++i) {
            for (int j = 0; j < numVertexHeight-1; ++j) {
                faces[cnt,0] = i*numVertexHeight+j;
                faces[cnt,1] = i*numVertexHeight+j+1;
                faces[cnt,2] = i*numVertexHeight+j+numVertexHeight+1;
                faces[cnt,3] = i*numVertexHeight+j+numVertexHeight;
                cnt += 1;
            }
        }
        return faces;
    }

    // getEdges: inspired by getFaces method
    public int[,] getEdges()
    {
        int numVerticalEdges = numVertexWidth * (numVertexHeight - 1);
        int numHorizontalEdges = (numVertexWidth - 1) * numVertexHeight;

        int numEdges = numVerticalEdges + numHorizontalEdges;

        int[,] edges = new int[numEdges, 2]; // shape (numedges, 2)

        int cnt = 0;

        // Edges along the height direction
        for (int i = 0; i < numVertexWidth; i++)
        {
            for (int j = 0; j < numVertexHeight - 1; j++)
            {
                int n0 = i * numVertexHeight + j;
                int n1 = i * numVertexHeight + j + 1;

                edges[cnt, 0] = n0;
                edges[cnt, 1] = n1;

                cnt++;
            }
        }

        // Edges along the width direction
        for (int i = 0; i < numVertexWidth - 1; i++)
        {
            for (int j = 0; j < numVertexHeight; j++)
            {
                int n0 = i * numVertexHeight + j;
                int n1 = (i + 1) * numVertexHeight + j;

                edges[cnt, 0] = n0;
                edges[cnt, 1] = n1;

                cnt++;
            }
        }

        return edges;
    }

#endregion

#region Handle and Mesh

    // previous methods (Lucia): Commented
    void CreateHandle() {        
        int[] idxArray = {0, numVertexHeight-1, (numVertexWidth-1)*numVertexHeight, (numVertexWidth-1)*numVertexHeight+numVertexHeight-1};
        if (allHandles) {
            numHandles = numVertexHeight * numVertexWidth;
            idxArray = new int[numHandles];
            for (int i = 0; i < numHandles; ++i) {
                idxArray[i] = i;
            }
        }

        Vector3[] corners = new Vector3[numHandles];
        for (int i = 0; i < numHandles; i++) {
            corners[i] = meshUnity.vertices[idxArray[i]];
            // Instantiate creates a new copy of a prefab or object in the scene
            // In this case, draghandle is created for every node
            GameObject handle = Instantiate(handlePrefab, corners[i], Quaternion.identity, transform.parent);

            DragHandle dh = handle.AddComponent<DragHandle>();
            dh.manipulator = this; // TriangleMesh script instance
            dh.nodeIndex = idxArray[i];
            dh.cornerIndex = i;
            handles.Add(dh);
        }
    }

    void CreateGrid() {
        meshFilter = GetComponent<MeshFilter>();
        meshUnity = new Mesh();
        N = numVertexHeight*numVertexWidth + (numVertexHeight-1)*(numVertexWidth-1);
        Vector3[] vertices = new Vector3[N];
        Vector2[] uv = new Vector2[N];
        if (doubleSided) {
            vertices = new Vector3[2*N];
            uv = new Vector2[2*N];
        }
        int[,] faces = getFacesTri();
        int[] nodeIndex = new int[numFaces*3];
        int cnt = 0;
        for (int x = 0; x < numVertexWidth; x++) {
            for (int y = 0; y < numVertexHeight; y++) {
                float posX = x, posY = y;
                posX /= (numVertexWidth-1); posY /= (numVertexHeight-1);
                Vector3 worldPos = originPosition + new Vector3(posX*gridWidth, 1, posY*gridHeight);

                vertices[cnt] = worldPos;
                uv[cnt] = new Vector2(posX, posY);
                uv[cnt] = new Vector2(0, 0);

                if (doubleSided) {
                    vertices[cnt+N] = worldPos;
                    uv[cnt+N] = new Vector2(posX, posY);
                }
                cnt += 1;
            }
        }
        for (int x = 1; x < numVertexWidth; x++) {
            for (int y = 1; y < numVertexHeight; y++) {
                float posX = x, posY = y;
                posX -= 0.5f; posY -= 0.5f;
                posX /= (numVertexWidth-1); posY /= (numVertexHeight-1);
                Vector3 worldPos = originPosition + new Vector3(posX*gridWidth, 1, posY*gridHeight);
                
                uv[cnt] = new Vector2(posX, posY);
                uv[cnt] = new Vector2(1, 1);
                vertices[cnt] = worldPos;

                if (doubleSided) {
                    vertices[cnt+N] = worldPos;
                    uv[cnt+N] = new Vector2(posX, posY);
                }
                cnt += 1;
            }
        }

        for (int i = 0; i < numFaces; ++i) {
            nodeIndex[i*3] = faces[i, 0];
            nodeIndex[i*3+1] = faces[i, 1];
            nodeIndex[i*3+2] = faces[i, 2];

        }
        meshUnity.vertices = vertices;
        meshUnity.triangles = nodeIndex;
        meshUnity.uv = uv;
        meshUnity.RecalculateNormals(); 
        meshUnity.RecalculateBounds();
        meshFilter.mesh = meshUnity;
        // Debug.Log($"Grid created: {numVertexWidth} x {numVertexHeight}");

        CreateHandle();
    }
#endregion



#region Awake
    void Awake() {
        simulated = new Dictionary<int, Vector3>();
        CreateGrid();
        // Unity makes the mesh first, then prepares the Python
        // simulation object that will control/update that mesh
        connection = new PythonConnection(this);
        connection.InitializePython();
        meshPython = connection.ImportClothScript();
    }

    private double[] V3ToArray(Vector3 vector) {
        double[] arr = new double[3];
        arr[0] = (double)vector.x;
        arr[1] = (double)vector.z;
        arr[2] = (double)vector.y - 1.0;
        return arr;
    }

    private Vector3 ArrayToV3(double[] vector) {
        Vector3 position;
        position.x = (float)vector[0];
        position.y = (float)(vector[2] + 1.0);
        position.z = (float)vector[1];
        return position;
    }

    // private float[] V3ToArray(Vector3 vector) {
    //     float[] arr = new float[3];
    //     arr[0] = vector.x;
    //     arr[1] = vector.z;
    //     arr[2] = vector.y - 1.0f;
    //     return arr;
    // }

    // private Vector3 ArrayToV3(float[] vector) {
    //     Vector3 position;
    //     position.x = vector[0];
    //     position.y = vector[2] + 1.0f;
    //     position.z = vector[1];
    //     return position;
    // }

    // private Vector3 ArrayToV3Vector(float[] vector)
    // {
    //     Vector3 v;
    //     v.x = vector[0];
    //     v.y = vector[2];
    //     v.z = vector[1];
    //     return v;
    // }

    // public float[,] getMeshPositions() 
    // {
    //     Vector3[] v = meshUnity.vertices;
    //     // Conversion done here (Not in Exporter.cs)
    //     float[,] pos = new float[numVertexHeight*numVertexWidth, 3];
    //     for (int i = 0; i < numVertexHeight*numVertexWidth; ++i) {
    //         float[] posI = V3ToArray(v[i]);
    //         for (int j = 0; j < 3; ++j) {
    //             pos[i, j] = posI[j];
    //         }
    //     }
    //     return pos;
    // }

    // //  used once to create meshPython:
    // public double[,] getInitialMeshPositionsDouble()
    // {
    //     Vector3[] v = meshUnity.vertices;

    //     int n = numVertexHeight * numVertexWidth;
    //     double[,] pos = new double[n, 3];

    //     for (int i = 0; i < n; i++)
    //     {
    //         // Unity coordinates -> Python coordinates
    //         pos[i, 0] = (double)v[i].x;
    //         pos[i, 1] = (double)v[i].z;
    //         pos[i, 2] = (double)v[i].y - 1.0;
    //     }

    //     return pos;
    // }
    public double[,] getInitialMeshPositionsDouble()
    {
        int n = numVertexHeight * numVertexWidth;
        double[,] pos = new double[n, 3];

        int cnt = 0;

        for (int x = 0; x < numVertexWidth; x++)
        {
            for (int y = 0; y < numVertexHeight; y++)
            {
                double posX = (double)x / (double)(numVertexWidth - 1);
                double posY = (double)y / (double)(numVertexHeight - 1);

                double xUnity = (double)originPosition.x + posX * (double)gridWidth;
                double yUnity = (double)originPosition.y + 1.0;
                double zUnity = (double)originPosition.z + posY * (double)gridHeight;

                // Unity coordinates -> Python coordinates: (x, y, z) -> (x, z, y - 1)
                pos[cnt, 0] = xUnity;
                pos[cnt, 1] = zUnity;
                pos[cnt, 2] = yUnity - 1.0;

                cnt++;
            }
        }

        return pos;
    }

    // used later for export, after meshPython exists
    public double[,] getMeshPositionsDouble()
    {
        // PYTHON TO UNITY
        double[][] pos = meshPython.getPositionsUnity(0).AsManagedObject(typeof(double[][])) as double[][];

        int n = numVertexHeight * numVertexWidth;
        double[,] outPos = new double[n, 3];

        for (int i = 0; i < n; i++)
        {
            outPos[i, 0] = pos[i][0];
            outPos[i, 1] = pos[i][1];
            outPos[i, 2] = pos[i][2];
        }

        return outPos;
    }

    public List<Vector3> getTotalMeshPositions() {
        return new List<Vector3> (meshUnity.vertices);
        // Conversion done in Exporter.cs
    }

    private void updateHandlePosition(int i, Vector3 newWorldPosition) {
        handles[i].transform.position = newWorldPosition;
    }
    // Asks the Python cloth object for the updated node positions,
    // converts them back into Unity coordinates, writes them into the 
    // Unity mesh, recalculates the mesh, and then moves the handles 
    // so they stay attached to the right nodes.
    private void loadPositionsFromMesh() {

        if (Input.GetKeyDown(KeyCode.C))
        {
            textureChanged = true;
        }

        // float[][] pos = meshPython.getPositionsUnity(smooth).AsManagedObject(typeof(float[][])) as float[][];
        double[][] pos = meshPython.getPositionsUnity(0).AsManagedObject(typeof(double[][])) as double[][];

        Vector3[] meshVertices = new Vector3[N];
        Vector2[] uv = new Vector2[N];
        for (int i = 0; i < N; ++i) {
            meshVertices[i] = ArrayToV3(pos[i]);
        }
        if (doubleSided) {
            meshVertices = new Vector3[2*N];
            uv = new Vector2[2*N];
            for (int i = 0; i < N; ++i) {
                meshVertices[i] = ArrayToV3(pos[i]);
                meshVertices[i+N] = ArrayToV3(pos[i]);

                if (textureChanged) {
                    uv[i] = new Vector2(0.0f, 0.0f);
                    uv[i+N] = new Vector2(1.0f, 1.0f);
                }
            }
            if (textureChanged) meshUnity.uv = uv;
        }
        meshUnity.vertices = meshVertices;
        meshUnity.RecalculateNormals(); 
        meshUnity.RecalculateBounds();

        for (int i = 0; i < numHandles; ++i) {
            // Debug.Log(meshVertices[handles[i].nodeIndex]);
            updateHandlePosition(i, meshVertices[handles[i].nodeIndex]);
        }

    }

    public void DragHandle(int idx, Vector3 newWorldPosition) {
        if (simulated.ContainsKey(idx)) simulated[idx] = newWorldPosition;
        else simulated.Add(idx, newWorldPosition);
    }

    public void LetHandleGo(int idx)
    {
        if (simulated.ContainsKey(idx))
            simulated.Remove(idx);

        if (previousControlTargets.ContainsKey(idx))
            previousControlTargets.Remove(idx);
    }
#endregion

#region Update

    void Update() {
        if (meshPython == null) {
            return;
        }
        // Debug.Log("Se actualiza la tela");
        float d = Time.deltaTime;
        // Debug.Log("Ultima llamada hace: " + d);

        List<double[]> positions = new List<double[]>();
        // List<float[]> positions = new List<float[]>();

        List<int> controlNodes = new List<int>();
        
        foreach (var x in simulated) {
            int curN = x.Key;
            controlNodes.Add(curN);
            positions.Add(V3ToArray(x.Value));
        }

        // collect controlled nodes in a deterministic order

        // foreach (var x in simulated.OrderBy(kv => kv.Key))
        // {
        //     int curN = x.Key;
        //     controlNodes.Add(curN);
        //     positions.Add(V3ToArray(x.Value));
        // }

        int[] control = controlNodes.ToArray();
        int nums = positions.Count;

        double[] pos = new double[nums*3];
        // float[] pos = new float[nums*3];

        for (int i = 0; i < nums; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                pos[i * 3 + j] = positions[i][j];
            }
        }

        //UNITY TO PYTHON

        GCHandle cHandle = GCHandle.Alloc(control, GCHandleType.Pinned);
        long cPtr = (long)cHandle.AddrOfPinnedObject();

        GCHandle vHandle = GCHandle.Alloc(pos, GCHandleType.Pinned);
        long vPtr = (long)vHandle.AddrOfPinnedObject();

        meshPython.simulate(vPtr, cPtr, nums);

        vHandle.Free();
        cHandle.Free();

        loadPositionsFromMesh();
    }
#endregion
}


