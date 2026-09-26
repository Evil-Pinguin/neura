using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class StainBlob : MonoBehaviour
{
    void Awake()
    {
        const int S = 128;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color coffee = new Color(0.24f, 0.16f, 0.09f, 0.85f); // тёмный кофе
        var px = new Color[S * S];

        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            // координаты от -1 до 1 относительно центра
            float dx = (x - S / 2f) / (S / 2f);
            float dy = (y - S / 2f) / (S / 2f);
            float r = Mathf.Sqrt(dx * dx + dy * dy);

            // рваный край: радиус пятна гуляет в зависимости от угла
            float a = Mathf.Atan2(dy, dx);
            float edge = 0.55f + 0.25f * Mathf.PerlinNoise(Mathf.Cos(a) * 2f + 10f, Mathf.Sin(a) * 2f + 10f);
            float body = Mathf.SmoothStep(edge, edge - 0.08f, r); // 1 внутри, 0 снаружи

            // брызги вокруг
            float drops = 0f;
            drops = Mathf.Max(drops, Drop(dx - 0.75f, dy - 0.6f, 0.09f));
            drops = Mathf.Max(drops, Drop(dx + 0.8f,  dy - 0.45f, 0.06f));
            drops = Mathf.Max(drops, Drop(dx + 0.65f, dy + 0.7f,  0.05f));

            float m = Mathf.Max(body, drops);
            px[y * S + x] = new Color(coffee.r, coffee.g, coffee.b, coffee.a * m);
        }

        tex.SetPixels(px);
        tex.Apply();

        // материал с прозрачностью, создаётся прямо в игре
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = tex;
        GetComponent<MeshRenderer>().material = mat;
    }

    float Drop(float dx, float dy, float radius)
    {
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        return Mathf.SmoothStep(radius, radius - 0.03f, d);
    }
}