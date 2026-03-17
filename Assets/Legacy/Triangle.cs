using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TriangleCreator : MonoBehaviour
{
    void Start()
    {
        // 1. Get the MeshFilter component (automatically added by [RequireComponent])
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        
        // 2. Create the new Mesh object
        Mesh mesh = new Mesh();
        
        // 3. Define the Vertices (Geometry)
        // These are the three corner points of the triangle in local space.
        Vector3[] vertices = new Vector3[3]
        {
            new Vector3(0, 0, 0),    // Vertex 0 (Bottom-Left)
            new Vector3(0, 1, 0),    // Vertex 1 (Top)
            new Vector3(1, 0, 0)     // Vertex 2 (Bottom-Right)
        };
        
        // 4. Define the Triangles (Topology)
        // Triangles specify the order in which vertices are connected to form surfaces.
        // They are listed in CLOCKWISE order (or counter-clockwise, depending on face orientation)
        // when viewed from the front (the direction of the face normal).
        int[] triangles = new int[3]
        {
            0, // Index of Vertex 0
            1, // Index of Vertex 1
            2  // Index of Vertex 2
        };
        
        // 5. Define UVs (Texturing)
        // UV coordinates map the 2D texture space to the 3D mesh vertices.
        Vector2[] uv = new Vector2[3]
        {
            new Vector2(0, 0), // UV for Vertex 0
            new Vector2(0.5f, 1), // UV for Vertex 1 (Center Top)
            new Vector2(1, 0)  // UV for Vertex 2
        };

        // 6. Assign data to the Mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        // 7. Recalculate Normals
        // Normals define the direction the surface is facing, necessary for lighting.
        mesh.RecalculateNormals(); 

        // 8. Assign the final Mesh to the MeshFilter
        meshFilter.mesh = mesh;
    }
}