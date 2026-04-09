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
            string json = JsonUtility.ToJson(dataWrapper, true);
            string path = Path.Combine(exportPath, fileName);
            if (dataWrapper.objects.Count > 1) {
                File.WriteAllText(path, json);
                dataWrapper = new SceneDataWrapper();
                Start();
            }
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
        SaveToJson();
        Debug.Log("Exporting Stopped. Data saved to JSON.");
    }

    void SaveToJson()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        
        if (targets.Length == 0)
        {
            Debug.LogWarning("No objects found with tag: " + targetTag);
            return;
        }

        foreach (GameObject obj in targets)
        {
            Debug.Log(obj.name);
            ObjectData toAdd = new ObjectData();
            toAdd.name = obj.name;
            toAdd.timestamp = Time.time;
            if (obj.name == "MeshRenderer") {
                var m = obj.GetComponent<TriangleMesh>();
                toAdd.position = m.getTotalMeshPositions();
            } else {
                toAdd.position.Add(transform.position);
                toAdd.rotation.Add(transform.rotation);
            }
            dataWrapper.objects.Add(toAdd);
        }

    }
}