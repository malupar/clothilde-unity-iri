using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TextureGradient : MonoBehaviour
{
    public enum GradientDirection { Vertical, Horizontal, Diagonal }

    [Header("Settings")]
    [Tooltip("Resolution of the generated texture. 256 is usually enough for simple gradients.")]
    public int resolution = 256;
    
    [Tooltip("Define colors using the Unity Gradient Editor")]
    public Gradient gradient;
    
    public GradientDirection direction = GradientDirection.Vertical;

    public bool applyGradient = true;

    void Start()
    {

        if (true)
        {
            gradient = new Gradient();
            
            // Set Colors: Green at start, Red at end
            GradientColorKey[] colors = new GradientColorKey[2];
            colors[0] = new GradientColorKey(Color.green, 0.0f);
            colors[1] = new GradientColorKey(Color.red, 1.0f);
            
            // Set Alphas: Opaque at start, Opaque at end
            GradientAlphaKey[] alphas = new GradientAlphaKey[2];
            alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphas[1] = new GradientAlphaKey(1.0f, 1.0f);

            gradient.SetKeys(colors, alphas);
        }
        if (applyGradient) ApplyGradientTexture();
    }

    public void ApplyGradientTexture()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        Texture2D texture = GenerateTexture();
        
        rend.material.mainTexture = texture;
        
        if (rend.material.HasProperty("_BaseMap"))
        {
            rend.material.SetTexture("_BaseMap", texture);
        }
    }

    Texture2D GenerateTexture()
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.wrapMode = TextureWrapMode.Clamp; // crucial to avoid ugly lines at edges
        tex.filterMode = FilterMode.Bilinear; // makes it smooth even at low res

        Color[] colors = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Calculate normalized coordinates (0.0 to 1.0)
                float u = x / (float)(resolution - 1);
                float v = y / (float)(resolution - 1);

                // Determine "Time" (t) based on direction
                float t = 0f;
                switch (direction)
                {
                    case GradientDirection.Horizontal:
                        t = u;
                        break;
                    case GradientDirection.Vertical:
                        t = v;
                        break;
                    case GradientDirection.Diagonal:
                        t = (u + v) * 0.5f; // Average them for diagonal
                        break;
                }

                // Sample the gradient
                colors[y * resolution + x] = gradient.Evaluate(t);
            }
        }

        // Apply pixels in one batch (Faster than SetPixel)
        tex.SetPixels(colors);
        tex.Apply();

        return tex;
    }
}