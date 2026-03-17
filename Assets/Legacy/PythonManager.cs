using UnityEngine;
using Python.Runtime;
using System;
using System.IO;

public class PythonManager : MonoBehaviour
{
    // Dependencies for conda env
    public const string CondaEnvPath = @"C:\Users\apari\miniconda3\envs\cholmod_env";
    public const string PythonDllName = "python311.dll";
    // "C:\Users\apari\miniconda3\envs\cholmod_env\python311.dll"

    public void InitializePython()
    {
        if (PythonEngine.IsInitialized) return;

        // 4. CRITICAL FIX FOR CONDA: Add Library/bin to system PATH
        //    Many Conda packages (like Numpy) rely on DLLs in 'Library/bin' that standard Python doesn't use.
        string libraryBinPath = Path.Combine(CondaEnvPath, "Library", "bin");
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

        if (!currentPath.Contains(libraryBinPath))
        {
            Environment.SetEnvironmentVariable("PATH", libraryBinPath + Path.PathSeparator + currentPath, EnvironmentVariableTarget.Process);
        }

        // 1. Set the path to the Python DLL *before* calling Initialize
        //    This tells pythonnet explicitly which Python version to load.
        string dllPath = Path.Combine(CondaEnvPath, PythonDllName);
        if (!File.Exists(dllPath))
        {
            Debug.LogError($"Could not find Python DLL at: {dllPath}");
            return;
        }
        Runtime.PythonDLL = dllPath;

        // 2. Set PythonHome to the Conda environment root
        //PythonEngine.PythonHome = CondaEnvPath;

        // 3. Set PythonPath manually
        //    Conda needs: EnvRoot, EnvRoot/Lib, EnvRoot/Lib/site-packages, and EnvRoot/DLLs
        string[] pythonPath = new string[] {
            CondaEnvPath,
            Path.Combine(CondaEnvPath, "Lib"),
            Path.Combine(CondaEnvPath, "Lib", "site-packages"),
            Path.Combine(CondaEnvPath, "DLLs")
        };
        //PythonEngine.PythonPath = string.Join(Path.PathSeparator.ToString(), pythonPath);

        // 5. Initialize the Engine
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

    void Start()
    {
        InitializePython();
    }
    
    void OnDestroy()
    {
        // Always shut down the engine cleanly
        if (PythonEngine.IsInitialized)
        {
            PythonEngine.Shutdown();
        }
    }
}