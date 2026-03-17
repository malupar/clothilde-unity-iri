using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ColorChange : MonoBehaviour
{
    int resolution = 20;
    int curText = 0;
    Texture2D red_blue;
    Texture2D orange_blue;
    public Texture2D external_texture;

    void Start()
    {
        red_blue = GenerateTexture(new Color(1.0f, 0.0f, 0.0f), new Color(0.0f, 0.0f, 1.0f));
        orange_blue = GenerateTexture(new Color(1.0f, 0.5f, 0.0f), new Color(0.0f, 0.0f, 1.0f));
    }

    Texture2D GenerateTexture(Color a, Color b)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.wrapMode = TextureWrapMode.Clamp; // crucial to avoid ugly lines at edges
        tex.filterMode = FilterMode.Bilinear; // makes it smooth even at low res

        Color[] colors = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                if (y < 10) colors[y * resolution + x] = a;
                else colors[y * resolution + x] = b;
            }
        }

        // Apply pixels in one batch (Faster than SetPixel)
        tex.SetPixels(colors);
        tex.Apply();

        return tex;
    }

    public void ApplyTexture(Texture2D newTexture)
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;
        
        rend.material.mainTexture = newTexture;
        
        if (rend.material.HasProperty("_BaseMap"))
        {
            rend.material.SetTexture("_BaseMap", newTexture);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (curText == 1)
            {
                ApplyTexture(orange_blue);
            }
            else
            {
                ApplyTexture(red_blue);
            }
            curText = 1 - curText;
        }
    }
}