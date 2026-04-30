using UnityEngine;
using Python.Runtime;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Collections.Generic;

public class PythonConnection : MonoBehaviour
{

    // Dependencies for conda env
    public const string CondaEnvPath = @"C:\Users\maparicio\miniconda3\envs\cholmod_env";
    public const string PythonDllName = "python311.dll";
    public const string PythonScripts = @"C:\Users\maparicio\Documents\My project\Assets\Scripts";

    private dynamic meshPython;
    private dynamic clothModule;

    private TriangleMesh mesh;

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
                    clothModule = Py.Import("Cloth");
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

                meshPython.setSimulatorParameters(dt: mesh.dt,
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
                                                  str: mesh.str);
            }
        }
        catch (PythonException e) {
            Debug.LogError("Python Error: " + e.Message);
        }
        return meshPython;
    }
}