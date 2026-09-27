using UnityEngine;
using UnityEngine.UI;

// Глитч-эффекты без шейдеров: шумовая вуаль + дрожание камеры (стадия 3+),
// рваные чёрные полосы (стадия 4 / всплески). Bootstrap, API статическое.
public class GlitchFx : MonoBehaviour
{
    public static GlitchFx Instance { get; private set; }

    static float burstUntil;

    RawImage noiseImg;
    Texture2D noiseTex;
    Image[] bars = new Image[3];
    float[] barTimers = new float[3];
    float nextBar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<GlitchFx>() != null) return;
        new GameObject("GlitchFx (auto)").AddComponent<GlitchFx>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildNoise();
        BuildBars();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Форсированный глитч на dur секунд (кат-сцены, скримеры).
    public static void Burst(float dur)
    {
        burstUntil = Time.time + dur;
    }

    void Update()
    {
        int stage = Syndrome.Stage;
        bool burst = Time.time < burstUntil;
        bool active = stage >= 3 || burst;
        bool tearing = stage >= 4 || burst;
        float amp = burst ? 1f : (stage >= 4 ? 1f : 0.5f);

        CameraShake.GlitchJitter = active ? 0.012f * amp : 0f;

        if (noiseImg != null)
        {
            noiseImg.gameObject.SetActive(active);
            if (active)
            {
                var c = noiseImg.color;
                c.a = (0.04f + 0.05f * amp) * (0.6f + 0.4f * Random.value);
                noiseImg.color = c;
                noiseImg.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
            }
        }

        if (!tearing)
        {
            foreach (var b in bars)
                if (b != null) b.gameObject.SetActive(false);
            return;
        }
        nextBar -= Time.deltaTime;
        if (nextBar <= 0f)
        {
            nextBar = Random.Range(0.3f, 1.4f);
            int i = Random.Range(0, bars.Length);
            if (bars[i] != null)
            {
                var rt = (RectTransform)bars[i].transform;
                rt.anchoredPosition = new Vector2(0f, Random.Range(-400f, 400f));
                bars[i].gameObject.SetActive(true);
                barTimers[i] = Random.Range(0.05f, 0.12f);
            }
        }
        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] == null || !bars[i].gameObject.activeSelf) continue;
            barTimers[i] -= Time.deltaTime;
            if (barTimers[i] <= 0f) bars[i].gameObject.SetActive(false);
        }
    }

    void BuildNoise()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        noiseTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        noiseTex.filterMode = FilterMode.Point;
        var px = new Color32[128 * 128];
        for (int i = 0; i < px.Length; i++)
        {
            byte v = (byte)Random.Range(0, 256);
            px[i] = new Color32(v, v, v, 255);
        }
        noiseTex.SetPixels32(px);
        noiseTex.Apply();

        var go = new GameObject("GlitchNoise");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        noiseImg = go.AddComponent<RawImage>();
        noiseImg.texture = noiseTex;
        noiseImg.color = new Color(1f, 1f, 1f, 0f);
        noiseImg.raycastTarget = false;
        go.SetActive(false);
    }

    void BuildBars()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        for (int i = 0; i < bars.Length; i++)
        {
            var go = new GameObject("TearBar" + i);
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 6f + i * 4f);
            rt.anchoredPosition = Vector2.zero;
            bars[i] = go.AddComponent<Image>();
            bars[i].color = new Color(0f, 0f, 0f, 0.75f);
            bars[i].raycastTarget = false;
            go.SetActive(false);
        }
    }
}
