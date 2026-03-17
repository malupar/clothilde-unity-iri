using UnityEngine;
using Python.Runtime;
using System;
using System.IO;
using System.Collections.Generic;

public class MeshQuad : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1.0f;
    public Vector3 originPosition = Vector3.zero;

    [Header("Visuals")]
    public GameObject nodeVisualPrefab;
    public Transform gridParent;

    public Node[,] grid;
    public GameObject[,] nodeGrid;

    private dynamic mesh;
    private dynamic clothModule;

    // Dependencies for conda env
    public const string CondaEnvPath = @"C:\Users\apari\miniconda3\envs\cholmod_env";
    public const string PythonDllName = "python311.dll";
    // "C:\Users\apari\miniconda3\envs\cholmod_env\python311.dll"
    public const string PythonScripts = @"C:\Users\apari\My project\Assets\Scripts";

    private Dictionary<Tuple<int, int>, Vector3> simulated;

    public void InitializePython()
    {
        if (PythonEngine.IsInitialized) return;

        string libraryBinPath = Path.Combine(CondaEnvPath, "Library", "bin");
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

        if (!currentPath.Contains(libraryBinPath))
        {
            Environment.SetEnvironmentVariable("PATH", libraryBinPath + Path.PathSeparator + currentPath, EnvironmentVariableTarget.Process);
        }

        string dllPath = Path.Combine(CondaEnvPath, PythonDllName);
        if (!File.Exists(dllPath))
        {
            Debug.LogError($"Could not find Python DLL at: {dllPath}");
            return;
        }
        Runtime.PythonDLL = dllPath;

        try
        {
            PythonEngine.Initialize();
            Debug.Log("Python Initialized Successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Python: {e}");
        }
        try
        {
            using (Py.GIL()) 
            {
                dynamic sys = Py.Import("sys");
                sys.path.append(Path.Combine(Application.dataPath, "Scripts"));
            }
        }
        catch (PythonException e)
        {
            Debug.LogError("Python Error: " + e.Message);
        }
    }

    void Awake() {
        /*simulated = new Dictionary<Tuple<int,int>, Vector3>();
        CreateGrid();
        InitializePython();
        ImportClothScript();
        */
    }

    // Distintos ejes de coord.
    private float[] V3ToArray(Vector3 vector) {
        float[] floatArray = new float[3];
        floatArray[0] = vector.x;
        floatArray[1] = vector.z;
        floatArray[2] = vector.y;
        return floatArray;
    }

    private Vector3 ArrayToV3(float[] vector) {
        Vector3 position;
        position.x = vector[0];
        position.y = vector[2];
        position.z = vector[1];
        Debug.Log(position.y);
        return position;
    }

    float[,] getMeshPositions() {
        float[,] gridPositions = new float[gridWidth*gridHeight, 3];
        for (int i = 0; i < gridWidth; ++i) {
            for (int j = 0; j < gridHeight; ++j) {
                Vector3 pos = nodeGrid[i, j].transform.position;
                float[] node = V3ToArray(pos);
                for (int k = 0; k < 3; ++k) {
                    gridPositions[i*gridHeight+j,k] = node[k];
                }
            }
        }
        return gridPositions;
    }

    int[,] getFaces() {
        int cnt = 0;
        int[,] faces = new int[(gridHeight-1)*(gridWidth-1), 4];
        for (int i = 0; i < gridWidth-1; ++i) {
            for (int j = 0; j < gridHeight-1; ++j) {
                faces[cnt,0] = i*gridHeight+j;
                faces[cnt,1] = i*gridHeight+j+1;
                faces[cnt,2] = i*gridHeight+j+gridHeight+1;
                faces[cnt,3] = i*gridHeight+j+gridHeight;
                cnt += 1;
            }
        }
        return faces;
    }

    private void loadPositionsFromMesh() {
        float[][] pos = mesh.getPositionsUnity().AsManagedObject(typeof(float[][])) as float[][];
        for (int i = 0; i < gridWidth; ++i) {
            for (int j = 0; j < gridHeight; ++j) {
                float[] vec3 = pos[i*gridHeight+j];
                nodeGrid[i, j].transform.position = ArrayToV3(vec3);
            }
        }
    }

    void ImportClothScript() {
        try {
            using (Py.GIL()) {
                clothModule = Py.Import("Cloth");
                Debug.Log("Cloth.py imported successfully.");

                mesh = clothModule.Cloth(getMeshPositions(), getFaces());
                Debug.Log(mesh);

                mesh.setSimulatorParameters();
            }
        }
        catch (PythonException e) {
            Debug.LogError("Python Error: " + e.Message);
        }
    }

    void CreateGrid() {
        grid = new Node[gridWidth, gridHeight];
        nodeGrid = new GameObject[gridWidth, gridHeight];

        if (gridParent == null) {
            gridParent = new GameObject("GridVisuals").transform;
            gridParent.SetParent(this.transform);
        }

        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                Vector3 worldPos = transform.position + new Vector3(x * cellSize, 0, y * cellSize);
                
                grid[x, y] = new Node(x, y, worldPos);

                if (nodeVisualPrefab != null) {
                    GameObject visual = Instantiate(
                        nodeVisualPrefab, 
                        worldPos, 
                        Quaternion.Euler(90, 0, 0)
                    );

                    visual.transform.SetParent(gridParent);
                    
                    visual.transform.localScale = new Vector3(cellSize, cellSize, 1);
                    nodeGrid[x, y] = visual;
                }
            }
        }


        Debug.Log($"Grid created: {gridWidth} x {gridHeight}");
    }

    void Update() {
        if (mesh == null) {
            return;
        }
        Debug.Log("Se actualiza la tela");
        List<int> controlNodes = new List<int>();
        controlNodes.Add(0);
        float[,] pos = new float[1, 3];
        float[] poso0;
        poso0 = V3ToArray(nodeGrid[0, 0].transform.position);
        poso0[1] = 1.0f;
        for (int i = 0; i < 3; ++i) pos[0, i] = poso0[i];
        /*List<float[]> positions = ;
        foreach (var x in simulated) {
            int curN = x.Key.Item1*gridHeight + x.Key.Item1;
            controlNodes.Add(curN);
            positions.Add(V3ToArray(x.Value));
        }
        int[] control = controlNodes.ToArray();
        int nums = positions.Count;
        float[,] pos = new float[nums, 3];
        for (int i = 0; i < nums; ++i) {
            for (int j = 0; j < 3; ++j) {
                pos[i, j] = positions[i][j];
            }
        }*/
        int[] control = controlNodes.ToArray();
        mesh.simulate(pos, control);

        loadPositionsFromMesh();
    }

    public void MoveNode(int x, int y, Vector3 newPosition) {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight) {
            nodeGrid[x, y].transform.position = newPosition;
        }
    }

    public GameObject GetNode(int x, int y) {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight && mesh != null) {
            return nodeGrid[x, y];
        }
        
        return null; 
    }

    public void registerNode(int x, int y, Vector3 newPos) {
        Tuple<int,int> z = new Tuple<int,int>(x, y);
        if (simulated.ContainsKey(z)) simulated[z] = newPos;
        else simulated.Add(new Tuple<int,int>(x, y), newPos);
    }

    public void unregisterNode(int x, int y) {
        simulated.Remove(new Tuple<int,int>(x, y));
    }

    void OnDestroy() {
        // Always shut down the engine cleanly
        if (PythonEngine.IsInitialized) {
            try {
                using (Py.GIL()) {
                    if (clothModule != null) {
                        clothModule.Dispose(); 
                        clothModule = null;
                    }
                    if (mesh != null) {
                        mesh.selfDelete();
                    }
                }
            }
            catch (Exception e) {
                Debug.LogError($"Error during Python object disposal: {e.Message}");
            }
            finally {
                PythonEngine.Shutdown();
                Debug.Log("Python Engine cleanly shut down.");
            }
        }
    }
}