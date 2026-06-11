using UnityEngine;
using Python.Runtime;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Collections.Generic;

public class PythonConnection : MonoBehaviour
{

    // Dependencies for conda env
    // public const string CondaEnvPath = @"C:\Users\maparicio\miniconda3\envs\cholmod_env";
    // public const string PythonDllName = "python311.dll";
    // public const string PythonScripts = @"C:\Users\maparicio\Documents\My project\Assets\Scripts";

    // public const string CondaEnvPath = @"C:\Users\abhil\miniconda3\envs\clothilde_env";
    // public const string PythonDllName = "python311.dll";
    // public const string PythonScripts = @"Z:\vs\clothilde-unity-iri\Assets\Scripts";

    public const string CondaEnvPath = @"C:\Users\apari\miniconda3\envs\cholmod_env";
    public const string PythonDllName = "python311.dll";
    public const string PythonScripts = @"C:\Users\apari\Documents\GitHub\clothilde-unity-iri\Assets\Scripts";

    private dynamic meshPython;
    private dynamic clothModule;

    private TriangleMesh mesh;

    private float[] V3ToArray(Vector3 vector) {
        float[] floatArray = new float[3];
        floatArray[0] = vector.x;
        floatArray[1] = vector.z;
        floatArray[2] = vector.y;
        return floatArray;
    }

    public PythonConnection(TriangleMesh triangleMesh)
    {
        mesh = triangleMesh;
    }

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

    public dynamic ImportClothScript() {
        try {
            using (Py.GIL()) {
                if (clothModule == null) {
                    clothModule = Py.Import("Cloth_speed");
                    Debug.Log("Cloth.py imported successfully.");
                }

                meshPython = clothModule.Cloth(mesh.getMeshPositions(), mesh.getFaces());
                Debug.Log(meshPython);

                // meshPython.setSimulatorParameters(dt: mesh.dt/mesh.numIter, 
                //                                   numIterSmooth: mesh.numIter, 
                //                                   mu_s: mesh.mu_s, 
                //                                   mu_f: mesh.mu_f, 
                //                                   thck: mesh.thck, 
                //                                   tol: mesh.tol, 
                //                                   rho: mesh.rho, 
                //                                   delta: mesh.delta, 
                //                                   kappa: mesh.kappa, 
                //                                   kappa_bnd: mesh.kappa_bnd, 
                //                                   alpha: mesh.alpha, 
                //                                   shr: mesh.shr, 
                //                                   str: mesh.str);

                GameObject targetObj = GameObject.Find("Table");
                Table table = targetObj.GetComponent<Table>();
                meshPython.setSimulatorParameters(dt: mesh.dt,
                                                //numIterSmooth: mesh.numIter,
                                                  sub_steps: mesh.sub_steps,
                                                  mu_s: mesh.mu_s, 
                                                  mu_f: mesh.mu_f, 
                                                  thck: mesh.thck, 
                                                  tol: mesh.tol, 
                                                  rho: mesh.rho, 
                                                  delta: mesh.delta, 
                                                  kappa: mesh.kappa, 
                                                  kappa_bnd: mesh.kappa_bnd, 
                                                  alpha: mesh.alpha, 
                                                  shr: mesh.shr, 
                                                  str: mesh.str,
                                                  slf: mesh.slf,
                                                  cusick: mesh.testCusick,
                                                  from_unity: true);

                if (table.generateTable) {
                    float[] dimensions = V3ToArray(table.dimensions);
                    Vector3 tablePos = new Vector3(
                        mesh.transform.position.x + mesh.gridWidth/2,
                        mesh.transform.position.y - dimensions[2]/2,
                        mesh.transform.position.z + mesh.gridHeight/2
                    );

                    float[] offset = V3ToArray(tablePos + table.offset);
                    GCHandle oHandle = GCHandle.Alloc(offset, GCHandleType.Pinned);
                    long oPtr = (long)oHandle.AddrOfPinnedObject();
                    GCHandle dHandle = GCHandle.Alloc(dimensions, GCHandleType.Pinned);
                    long dPtr = (long)dHandle.AddrOfPinnedObject();
                    meshPython.addTable(center: oPtr, dimensions: dPtr, mu: table.mu);
                }
            }
        }
        catch (PythonException e) {
            Debug.LogError("Python Error: " + e.Message);
        }
        return meshPython;
    }
}