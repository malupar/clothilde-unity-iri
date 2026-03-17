using UnityEngine;
using Python.Runtime;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TriangleMesh : MonoBehaviour
{
    [Header("Grid Settings")]
    public int numVertexWidth = 25;
    public int numVertexHeight = 25;
    public float gridWidth = 1.0f;
    public float gridHeight = 1.0f;
    public Vector3 originPosition = Vector3.zero;
    public bool doubleSided = true;
    public bool allHandles = true;

    //Runtime modifiable parameters
    public int numIter = 2;
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
    public float thck = 1.2f;
    public bool selfCollisions = true;

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
    private dynamic meshPython;
    private PythonConnection connection;

    // Saved parameters
    private bool textureChanged = false;

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
            GameObject handle = Instantiate(handlePrefab, corners[i], Quaternion.identity, transform.parent);

            DragHandle dh = handle.AddComponent<DragHandle>();
            dh.manipulator = this;
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
                Vector3 worldPos = originPosition + new Vector3(posX*gridWidth, 0, posY*gridHeight);
                
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
        Debug.Log($"Grid created: {numVertexWidth} x {numVertexHeight}");

        CreateHandle();
    }

    void Awake() {
        simulated = new Dictionary<int, Vector3>();
        CreateGrid();
        connection = new PythonConnection(this);
        connection.InitializePython();
        meshPython = connection.ImportClothScript();
    }

    // Distintos ejes de coord.
    private float[] V3ToArray(Vector3 vector) {
        float[] floatArray = new float[3];
        floatArray[0] = vector.x;
        floatArray[1] = vector.z;
        floatArray[2] = vector.y-1;
        return floatArray;
    }

    private Vector3 ArrayToV3(float[] vector) {
        Vector3 position;
        position.x = vector[0];
        position.y = vector[2]+1;
        position.z = vector[1];
        return position;
    }

    public float[,] getMeshPositions() {
        Vector3[] v = meshUnity.vertices;
        float[,] pos = new float[numVertexHeight*numVertexWidth, 3];
        for (int i = 0; i < numVertexHeight*numVertexWidth; ++i) {
            float[] posI = V3ToArray(v[i]);
            for (int j = 0; j < 3; ++j) {
                pos[i, j] = posI[j];
            }
        }
        return pos;
    }

    public List<Vector3> getTotalMeshPositions() {
        return new List<Vector3> (meshUnity.vertices);
    }

    int[,] getFacesTri() {
        numFaces = (numVertexHeight-1)*(numVertexWidth-1)*8;
        int cnt = 0, cntF = 0, diff = numVertexHeight*numVertexWidth;
        int[,] faces = new int[numFaces, 3];
        Debug.Log("Numero caras: " + numFaces);
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

    private void updateHanldePosition(int i, Vector3 newWorldPosition) {
        handles[i].transform.position = newWorldPosition;
    }

    private void loadPositionsFromMesh() {

        if (Input.GetKeyDown(KeyCode.C))
        {
            textureChanged = true;
        }

        float[][] pos = meshPython.getPositionsUnity().AsManagedObject(typeof(float[][])) as float[][];
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
        //meshFilter.mesh = meshUnity;

        for (int i = 0; i < numHandles; ++i) {
            updateHanldePosition(i, meshVertices[handles[i].nodeIndex]);
        }
    }

    public void DragHandle(int idx, Vector3 newWorldPosition) {
        if (simulated.ContainsKey(idx)) simulated[idx] = newWorldPosition;
        else simulated.Add(idx, newWorldPosition);
    }

    public void LetHandleGo(int idx) {
        if (simulated.ContainsKey(idx)) simulated.Remove(idx);
    }

    void Update() {
        if (meshPython == null) {
            return;
        }
        Debug.Log("Se actualiza la tela");
        float d = Time.deltaTime;
        Debug.Log("Ultima llamada hace: " + d);
        List<float[]> positions = new List<float[]>();
        List<int> controlNodes = new List<int>();

        foreach (var x in simulated) {
            int curN = x.Key;
            controlNodes.Add(curN);
            positions.Add(V3ToArray(x.Value));
        }

        int[] control = controlNodes.ToArray();
        int nums = positions.Count;
        float[] pos = new float[nums*3];
        for (int i = 0; i < nums; ++i) {
            for (int j = 0; j < 3; ++j) {
                pos[i*3 + j] = positions[i][j];
            }
        }

        GCHandle cHandle = GCHandle.Alloc(control, GCHandleType.Pinned);
        long cPtr = (long)cHandle.AddrOfPinnedObject();

        for (int i = 0; i < nums*3; ++i){
            Debug.Log(pos[i]);
        }

        for (int it = 0; it < numIter; ++it) {
            for (int i = 0; i < nums; ++i) {
                float[] p = V3ToArray(meshUnity.vertices[control[i]]);
                for (int j = 0; j < 3; ++j) {
                    pos[i*3 + j] = (1+it)*(positions[i][j]-p[j])/numIter + p[j];
                }
            }
            GCHandle vHandle = GCHandle.Alloc(pos, GCHandleType.Pinned);
            long vPtr = (long)vHandle.AddrOfPinnedObject();
            meshPython.simulate(vPtr, cPtr, nums);
            vHandle.Free();
        }

        cHandle.Free();
        loadPositionsFromMesh();
    }
}