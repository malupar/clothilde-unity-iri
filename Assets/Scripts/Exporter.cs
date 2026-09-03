using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class Exporter : MonoBehaviour
{
    [Header("Export")]
    public string exportFolder = @"Z:\IRI_2026\clothilde-sim\python_code\exported_data3_binary";

    [Header("Scene references")]
    public TriangleMesh cloth;
    public Gripper gripper;

    private bool isExporting = false;
    private int frame = 0;
    private float recordingStartTime = 0.0f;



    private List<float> timesBin;
    private List<double> clothFramesBin; // nFrames x nVertices x 3: 
    // flattened as (frame, vertex, xyz)
    // private List<float> clothFramesBin;
    private List<float> gripperPosesBin; // 
    private List<int> jawStatusBin;

    private double[] meshVerticesBin; // nVertices x 3: flattened
    // private float[] meshVerticesBin;
    private int[] meshFacesBin; // nFaces x 4: flattened

    private int nVertices;
    private int nFaces;

    // void Awake()
    // {
    //     // CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    // }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!isExporting)
            {
                StartRecording();
            }
            else
            {
                StopRecordingAndSave();
            }
        }

        if (isExporting)
        {
            // Save cloth node positions at every frame
            RecordFrame();
            frame++;
        }
    }
#region StartRecord
    void StartRecording()
    {
        recordingStartTime = Time.time;
        isExporting = true;
        frame = 0;

        Directory.CreateDirectory(exportFolder);

        timesBin = new List<float>();
        clothFramesBin = new List<double>();
        // clothFramesBin = new List<float>();
        gripperPosesBin = new List<float>();
        jawStatusBin = new List<int>();

        ExportMeshOnceBinary();

        Debug.Log("Cloth export started. Press P again to stop and save.");
    }
#endregion
#region StopRecord
    void StopRecordingAndSave()
    {
        isExporting = false;

        string path = Path.Combine(exportFolder, "replay_cloth.bin");
        SaveReplayBinary(path);

        Debug.Log("Export stopped. Files saved to: " + exportFolder);
    }

    void ExportMeshOnceBinary()
    {
        // float[,] verticesUnity = cloth.getMeshPositions();
        // // int nVertices = cloth.numVertexWidth * cloth.numVertexHeight;
        // nVertices = verticesUnity.GetLength(0);

        // meshVerticesBin = new float[nVertices * 3];

        // for (int i = 0; i < nVertices; i++)
        // {
        //     // conversion Unity point to python point already done in getMeshPositions().
        //     meshVerticesBin[3 * i + 0] = verticesUnity[i, 0];
        //     meshVerticesBin[3 * i + 1] = verticesUnity[i, 1];
        //     meshVerticesBin[3 * i + 2] = verticesUnity[i, 2];
        // }

        double[,] verticesUnity = cloth.getMeshPositionsDouble();
        // float[,] verticesUnity = cloth.getMeshPositions();

        nVertices = verticesUnity.GetLength(0);
        meshVerticesBin = new double[nVertices * 3];
        // meshVerticesBin = new float[nVertices * 3];

        for (int i = 0; i < nVertices; i++)
        {
            // Vector3 p = UnityPointToPython(verticesUnity[i]);

            meshVerticesBin[3 * i + 0] = verticesUnity[i, 0];
            meshVerticesBin[3 * i + 1] = verticesUnity[i, 1];
            meshVerticesBin[3 * i + 2] = verticesUnity[i, 2];
        }

        int[,] faces = cloth.getFaces();

        nFaces = faces.GetLength(0);
        meshFacesBin = new int[nFaces * 4];

        for (int i = 0; i < nFaces; i++)
        {
            meshFacesBin[4 * i + 0] = faces[i, 0];
            meshFacesBin[4 * i + 1] = faces[i, 1];
            meshFacesBin[4 * i + 2] = faces[i, 2];
            meshFacesBin[4 * i + 3] = faces[i, 3];
        }
    }
#endregion
#region RecordFrame
    void RecordFrame()
    {
        // float[,] verticesUnity = cloth.getMeshPositions();

        // float t = Time.time - recordingStartTime;
        // timesBin.Add(t);

        // for (int i = 0; i < nVertices; i++)
        // {
        //     clothFramesBin.Add(verticesUnity[i, 0]);
        //     clothFramesBin.Add(verticesUnity[i, 1]);
        //     clothFramesBin.Add(verticesUnity[i, 2]);
        // }

        double[,] vertices = cloth.getMeshPositionsDouble();
        // float[,] vertices = cloth.getMeshPositions();

        float t = Time.time - recordingStartTime;
        timesBin.Add(t);

        for (int i = 0; i < nVertices; i++)
        {
            // Vector3 p = UnityPointToPython(verticesUnity[i]);

            clothFramesBin.Add(vertices[i, 0]);
            clothFramesBin.Add(vertices[i, 1]);
            clothFramesBin.Add(vertices[i, 2]);
        }

        // gripper data export
        Transform gripperFrame = gripper.transform;
        Vector3 pGripper = UnityPointToPython(gripperFrame.position);
        Vector4 qGripper = UnityQuaternionToPython(gripperFrame.rotation);
        // Default to open if assembly is missing
        int jawOpen = 1;

        if (gripper.gripperAssembly != null)
        {
            jawOpen = gripper.gripperAssembly.IsOpen ? 1 : 0;
        }

        // gripper_poses order: px, py, pz, qw, qx, qy, qz
        gripperPosesBin.Add(pGripper.x);
        gripperPosesBin.Add(pGripper.y);
        gripperPosesBin.Add(pGripper.z);

        gripperPosesBin.Add(qGripper.x);
        gripperPosesBin.Add(qGripper.y);
        gripperPosesBin.Add(qGripper.z);
        gripperPosesBin.Add(qGripper.w);

        jawStatusBin.Add(jawOpen);
    }
#endregion

    // Unity point (x, y, z) -> clothilde-sim point (x, z, y - 1)
    Vector3 UnityPointToPython(Vector3 p)
    {
        return new Vector3(p.x, p.z, p.y - 1.0f);
    }
    Vector3 UnityVectorToPython(Vector3 v)
    {
        return new Vector3(v.x, v.z, v.y);
    }
    Vector4 UnityQuaternionToPython(Quaternion q)
    {
        return new Vector4(q.w, -q.x, -q.z, -q.y);
    }
#region Save
    private void SaveReplayBinary(string fileName)
    {
        int version = 1; // to check in Python in case the version is later changed

        int nFrames = timesBin.Count;
        int nParams = 14;

        float[] simParams = new float[]
        {
            cloth.dt,
            cloth.tol,
            cloth.sub_steps,
            cloth.rho,
            cloth.delta,
            cloth.alpha,
            cloth.kappa,
            cloth.kappa_bnd,
            cloth.str,
            cloth.shr,
            cloth.slf,
            cloth.mu_f,
            cloth.mu_s,
            cloth.thck
        };

    Debug.Log("nFrames = " + nFrames);
    Debug.Log("nVertices = " + nVertices);
    Debug.Log("nFaces = " + nFaces);


    using (var fileStream = File.Open(fileName, FileMode.Create))
    using (var writer = new BinaryWriter(fileStream))
        {
        // This is a file signature. Python reads the first 8 bytes and checks.
        // to prevent accidentally reading a wrong file
        writer.Write(Encoding.ASCII.GetBytes("CLTHSIM1"));

        // Metadata: binary int32
        writer.Write(version);
        writer.Write(nFrames);
        writer.Write(nVertices);
        writer.Write(nFaces);
        writer.Write(nParams);

        // mesh vertices: shape (nVertices, 3)
        for (int i = 0; i < meshVerticesBin.Length; i++)
            {
                writer.Write(meshVerticesBin[i]);
            }

        // mesh_faces: shape (nFaces, 4)
        for (int i = 0; i < meshFacesBin.Length; i++)
        {
            writer.Write(meshFacesBin[i]);
        }

        // cloth_frames: shape (nFrames, nVertices, 3)
        for (int i = 0; i < clothFramesBin.Count; i++)
        {
            writer.Write(clothFramesBin[i]);
        }

        // gripper_poses: shape (nFrames, 7)
        for (int i = 0; i < gripperPosesBin.Count; i++)
        {
            writer.Write(gripperPosesBin[i]);
        }

        // jaw_status: shape (nFrames,)
        for (int i = 0; i < jawStatusBin.Count; i++)
        {
            writer.Write(jawStatusBin[i]);
        }

        // times: shape (nFrames,)
        for (int i = 0; i < timesBin.Count; i++)
        {
            writer.Write(timesBin[i]);
        }

        // simulator parameters: shape (14,)
        for (int i = 0; i < simParams.Length; i++)
        {
            writer.Write(simParams[i]);
        }
        }

    }
#endregion
}