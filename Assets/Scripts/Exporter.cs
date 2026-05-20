using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using UnityEngine;

[System.Serializable]
public class Face
{
    public List<int> face = new List<int>();
}

[System.Serializable]
public class ObjectData
{
    public string name;
    public List<Vector3> position = new List<Vector3>();
    public List<Quaternion> rotation = new List<Quaternion>();
    public List<Face> faces = new List<Face>();
    public float timestamp;
}

public class Exporter : MonoBehaviour
{
    [Header("Settings")]
    public string targetTag = "Exportable"; // Only objects with this tag will be saved
    private string exportPath = @"C:\Users\maparicio\Documents\My project\Assets\Exports";

    private bool isExporting = false;
    private List<ObjectData> objects = new List<ObjectData>();

    private List<Face> turnToMatrix(int[,] faces) {
        int rows = faces.GetLength(0);
        int cols = faces.GetLength(1);

        List<Face> faceM = new List<Face>();
        for (int i = 0; i < rows; ++i) {
            Face faceMi = new Face();
            for (int j = 0; j < cols; ++j)
                faceMi.face.Add(faces[i, j]); 
            faceM.Add(faceMi);
        }
        return faceM;
    }

    void Start() {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        // Avoid accumulating old data when Start() is called again after saving.
        objects.Clear();

        ObjectData f = new ObjectData();
        f.name = "Mesh - Distribution";
        var meshObject = Object.FindObjectsByType(typeof(TriangleMesh), FindObjectsSortMode.None)[0];
        TriangleMesh mesh = (TriangleMesh) meshObject;
        int[,] faces = mesh.getFaces();
        f.faces = turnToMatrix(faces);
        f.timestamp = 0;
        objects.Add(f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isExporting) {
                isExporting = false;
                SaveToCsv();
                Start();
            }
            else isExporting = true;
        }

        if (!isExporting)
        {
            StartExportProcess();
        }
        else
        {
            processData();
        }
    }

    void StartExportProcess()
    {
        Debug.Log("Exporting Started... Press 'E' again to save and stop.");
    }

    void StopExportProcess()
    {
        Debug.Log("Exporting Stopped. Data saved to CSV.");
    }

    void processData() {
    GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
    Debug.Log("Guardando datos...");

    if (targets.Length == 0)
    {
        Debug.LogWarning("No objects found with tag: " + targetTag);
        return;
    }

    foreach (GameObject obj in targets) {
        ObjectData toAdd = new ObjectData();
        toAdd.name = obj.name;
        toAdd.timestamp = Time.time;

        if (obj.name == "MeshRenderer") {
            var m = obj.GetComponent<TriangleMesh>();
            toAdd.position = m.getTotalMeshPositions();
            // continue; // To avoid recording mesh every frame
        } 
        else {
            Transform exportFrame = obj.transform;

            GripperVR gripper = obj.GetComponent<GripperVR>();

            if (gripper == null)
                gripper = obj.GetComponentInParent<GripperVR>();

            if (gripper == null)
                gripper = obj.GetComponentInChildren<GripperVR>();

            if (gripper != null && gripper.graspFrame != null) {
                exportFrame = gripper.graspFrame;
            }

            toAdd.position.Add(exportFrame.position);
            toAdd.rotation.Add(exportFrame.rotation);
        }

        objects.Add(toAdd);
    }
}

    void SaveToCsv()
    {
        string sessionFolder = Path.Combine(exportPath, "Session_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        if (!Directory.Exists(sessionFolder)) Directory.CreateDirectory(sessionFolder);

        StringBuilder leftCont = new StringBuilder("Timestamp,X,Y,Z,Qx,Qy,Qz,Qw\n");
        StringBuilder rightCont = new StringBuilder("Timestamp,X,Y,Z,Qx,Qy,Qz,Qw\n");
        StringBuilder mesh = new StringBuilder("X,Y,Z\n");
        StringBuilder faceSb = new StringBuilder("FaceIndex,VertexIndices\n");

        foreach (var data in objects)
        {
            if (data.rotation.Count > 0) {
                string lowerName = data.name.ToLowerInvariant();

                string line = $"{data.timestamp}," +
                              $"{data.position[0].x},{data.position[0].y},{data.position[0].z}," +
                              $"{data.rotation[0].x},{data.rotation[0].y},{data.rotation[0].z},{data.rotation[0].w}";

                if (lowerName.Contains("right")) {
                    rightCont.AppendLine(line);
                } 
                else {
                    leftCont.AppendLine(line);
                }
            } 
            else {
                for (int i = 0; i < data.position.Count; ++i) {
                    mesh.AppendLine($"{data.position[i].x},{data.position[i].y},{data.position[i].z}");
                }
            }

            for (int i = 0; i < data.faces.Count; i++)
            {
                string indices = string.Join(";", data.faces[i].face);
                faceSb.AppendLine($"{i},{indices}");
            }
        }

        File.WriteAllText(Path.Combine(sessionFolder, "Mesh.csv"), mesh.ToString());
        File.WriteAllText(Path.Combine(sessionFolder, "Right.csv"), rightCont.ToString());
        File.WriteAllText(Path.Combine(sessionFolder, "Left.csv"), leftCont.ToString());
        File.WriteAllText(Path.Combine(sessionFolder, "Faces.csv"), faceSb.ToString());
    }
}