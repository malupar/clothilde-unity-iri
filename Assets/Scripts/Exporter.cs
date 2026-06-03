using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class Exporter : MonoBehaviour
{
    [Header("Export")]
    public string exportFolder = @"Z:\IRI_2026\clothilde-sim\python_code\exported_data3";

    [Header("Scene references")]
    public TriangleMesh cloth;

    private bool isExporting = false;
    private int frame = 0;

    private StringBuilder clothFrames;

    // Gripper data
    public Gripper gripper;
    private StringBuilder gripperPoses;

    void Awake()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    }

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

    void StartRecording()
    {
        isExporting = true;
        frame = 0;

        Directory.CreateDirectory(exportFolder);

        ExportMeshOnce();
        ExportSimulatorParametersOnce();

        clothFrames = new StringBuilder("frame,t,node_index,x,y,z\n"); // header
        gripperPoses = new StringBuilder("frame,t,px,py,pz,qw,qx,qy,qz,jaw_open\n");

        Debug.Log("Cloth export started. Press P again to stop and save.");
    }

    void StopRecordingAndSave()
    {
        isExporting = false;

        File.WriteAllText(
            Path.Combine(exportFolder, "cloth_frames.csv"),
            clothFrames.ToString()
        );

        File.WriteAllText(
            Path.Combine(exportFolder, "gripper_poses.csv"),
            gripperPoses.ToString()
        );

        Debug.Log("Export stopped. Files saved to: " + exportFolder);
    }

    void ExportMeshOnce()
    {
        List<Vector3> verticesUnity = cloth.getTotalMeshPositions();

        int n = cloth.numVertexWidth * cloth.numVertexHeight;

        StringBuilder verts = new StringBuilder("node_index,x,y,z\n");

        for (int i = 0; i < n; i++)
        {
            Vector3 p = UnityPointToPython(verticesUnity[i]);
            verts.AppendLine($"{i},{p.x},{p.y},{p.z}");
        }

        int[,] faces = cloth.getFaces();

        StringBuilder faceSb = new StringBuilder("face_id,n0,n1,n2,n3\n");

        int rows = faces.GetLength(0);

        for (int i = 0; i < rows; i++)
        {
            faceSb.AppendLine(
                $"{i},{faces[i, 0]},{faces[i, 1]},{faces[i, 2]},{faces[i, 3]}"
            );
        }

        File.WriteAllText(Path.Combine(exportFolder, "mesh_vertices.csv"), verts.ToString());
        File.WriteAllText(Path.Combine(exportFolder, "mesh_faces.csv"), faceSb.ToString());
    }

    void ExportSimulatorParametersOnce()
    {
        string path = Path.Combine(exportFolder, "simulator_parameters.csv");

        string header =
            "dt,tol,sub_steps,rho,delta,alpha,kappa,kappa_bnd,str,shr,slf,mu_f,mu_s,thck";

        string values = string.Join(",",
            cloth.dt.ToString(CultureInfo.InvariantCulture),
            cloth.tol.ToString(CultureInfo.InvariantCulture),
            cloth.sub_steps.ToString(CultureInfo.InvariantCulture),
            cloth.rho.ToString(CultureInfo.InvariantCulture),
            cloth.delta.ToString(CultureInfo.InvariantCulture),
            cloth.alpha.ToString(CultureInfo.InvariantCulture),
            cloth.kappa.ToString(CultureInfo.InvariantCulture),
            cloth.kappa_bnd.ToString(CultureInfo.InvariantCulture),
            cloth.str.ToString(CultureInfo.InvariantCulture),
            cloth.shr.ToString(CultureInfo.InvariantCulture),
            cloth.slf.ToString(CultureInfo.InvariantCulture),
            cloth.mu_f.ToString(CultureInfo.InvariantCulture),
            cloth.mu_s.ToString(CultureInfo.InvariantCulture),
            cloth.thck.ToString(CultureInfo.InvariantCulture)        
            );

        File.WriteAllLines(path, new string[] { header, values });
    }

    void RecordFrame()
    {
        List<Vector3> verticesUnity = cloth.getTotalMeshPositions();

        int n = cloth.numVertexWidth * cloth.numVertexHeight;

        float t = Time.time;

        for (int i = 0; i < n; i++)
        {
            Vector3 p = UnityPointToPython(verticesUnity[i]);
            clothFrames.AppendLine($"{frame},{t},{i},{p.x},{p.y},{p.z}");
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

        gripperPoses.AppendLine(
            $"{frame},{t}," + 
            $"{pGripper.x},{pGripper.y},{pGripper.z}," +
            $"{qGripper.w}, {qGripper.x}, {qGripper.y}, {qGripper.z}," +
            $"{jawOpen}"
        );
    }

    // Unity point (x, y, z) -> clothilde-sim point (x, z, y - 1)
    Vector3 UnityPointToPython(Vector3 p)
    {
        return new Vector3(p.x, p.z, p.y - 1.0f);
    }
    Vector4 UnityQuaternionToPython(Quaternion q)
    {
        return new Vector4(q.w, -q.x, -q.z, -q.y);
    }
}