using System.Collections.Generic;
using System.IO;
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

[System.Serializable]
public class SceneDataWrapper
{
    public List<ObjectData> objects = new List<ObjectData>();
}

public class JsonExporter : MonoBehaviour
{
    [Header("Settings")]
    public string targetTag = "Exportable"; // Only objects with this tag will be saved
    public string fileName = "LiveExport.json";
    private string exportPath = @"C:\Users\maparicio\Documents\My project\Assets\Exports";

    private bool isExporting = false;
    private SceneDataWrapper dataWrapper = new SceneDataWrapper();

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
        ObjectData f = new ObjectData();
        f.name = "Mesh - Distribution";
        var meshObject = Object.FindObjectsByType(typeof(TriangleMesh), FindObjectsSortMode.None)[0];
        TriangleMesh mesh = (TriangleMesh) meshObject;
        int[,] faces = mesh.getFaces();
        f.faces = turnToMatrix(faces);
        f.timestamp = 0;
        dataWrapper.objects.Add(f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isExporting) 
                isExporting = false;
            else isExporting = true;
        }
        if (!isExporting)
        {
            StartExportProcess();
        }
        else
        {
            StopExportProcess();
        }
    }

    void StartExportProcess()
    {
        Debug.Log("Exporting Started... Press 'E' again to save and stop.");
    }

    void StopExportProcess()
    {
        SaveToCsv();
        Debug.Log("Exporting Stopped. Data saved to CSV.");
    }

    void SaveToCsv()
    {
        string sessionFolder = Path.Combine(exportPath, "Session_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        if (!Directory.Exists(sessionFolder)) Directory.CreateDirectory(sessionFolder);

        StringBuilder leftCont = new StringBuilder("Timestamp,X,Y,Z,Xangle,Yangle,Zangle\n");
        StringBuilder rightCont = new StringBuilder("Timestamp,X,Y,Z,Xangle,Yangle,Zangle\n");
        StringBuilder mesh = new StringBuilder("X,Y,Z\n");
        StringBuilder faceSb = new StringBuilder("FaceIndex,VertexIndices\n");

        foreach (var data in collectedData)
        {
            if (data.rotation.Count > 0) {
                if (data.name == "Controller (right)") {
                    rightCont.AppendLine($"{data.timestamp},{data.position[0].x},{data.position[0].y},{data.position[0].z},{data.rotation[0].x},{data.rotation[0].y},{data.rotation[0].z}");
                } else {
                    leftCont.AppendLine($"{data.timestamp},{data.position[0].x},{data.position[0].y},{data.position[0].z},{data.rotation[0].x},{data.rotation[0].y},{data.rotation[0].z}");
                }
            } else {
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
        File.WriteAllText(Path.Combine(sessionFolder, "Left.csv"), rightCont.ToString());
        File.WriteAllText(Path.Combine(sessionFolder, "Faces.csv"), faceSb.ToString());
    }
}