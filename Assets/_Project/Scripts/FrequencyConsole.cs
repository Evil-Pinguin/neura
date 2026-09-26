using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FrequencyConsole : MonoBehaviour
{
    public static FrequencyConsole Instance { get; private set; }
    public bool IsOpen => active;
    [Header("Ссылки")]
    [SerializeField] GameObject panel;
    [SerializeField] RawImage waveImage;
    [SerializeField] TextMeshProUGUI freqLabel;
    [SerializeField] TextMeshProUGUI statusLabel;
    [SerializeField] PlayerController playerController;
    [SerializeField] PlayerInteraction playerInteraction;
    [SerializeField] ToneOscillator tone;
    [SerializeField] CameraShake shake;

    [Header("Настройки подбора")]
    [SerializeField] float minFreq = 1f;
    [SerializeField] float maxFreq = 10f;
    [SerializeField] float scrollSpeed = 3f;
    [SerializeField] float captureThreshold = 0.08f;
    [SerializeField] float captureHoldTime = 1.5f;

    [Header("Сюжет: дело Уилла")]
    [SerializeField] bool willCase = false;
    [SerializeField, TextArea] string echoText = "…ИНЦИДЕНТ 7-Б ЛОКАЛИЗОВАН. СВИДЕТЕЛЕЙ: 143. ЗАПУСКАЮ ПРОТОКОЛ…";

    const int W = 512;
    const int H = 256;

    Texture2D tex;
    Color[] background;
    RectTransform panelRT;

    public float targetFreq = 4f;
    float playerFreq = 8f;
    float holdTimer;
    float echoTimer;
    float glitchTimer;
    bool active;
    public bool Captured { get; private set; }

    // аномалия жива, пока игрок не принял калибровку
    bool AnomalyActive => willCase && !GameFlags.CalibrationAccepted;

    void Awake()
    {
        Instance = this;
        tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        waveImage.texture = tex;
        panelRT = panel.transform as RectTransform;

        background = new Color[W * H];
        Color bg = new Color(0.01f, 0.02f, 0.03f);
        for (int i = 0; i < background.Length; i++) background[i] = bg;
    }

    public void Open()
    {
        if (active) return;
        active = true;
        Captured = false;
        holdTimer = 0f;
        echoTimer = 0f;
        glitchTimer = 0f;

        do { playerFreq = Random.Range(minFreq, maxFreq); }
        while (Mathf.Abs(playerFreq - targetFreq) < captureThreshold * 5f);

        panel.SetActive(true);
        panelRT.anchoredPosition = Vector2.zero;
        playerController.enabled = false;
        playerInteraction.enabled = false;
        tone.audioOn = true;
        tone.detune = 0f;
    }

    public void Close()
    {
        if (!active) return;
        active = false;
        panel.SetActive(false);
        playerController.enabled = true;
        playerInteraction.enabled = true;
        tone.audioOn = false;
        tone.detune = 0f;
    }

    void Update()
    {
        if (!active) return;

        float scroll = Input.mouseScrollDelta.y * scrollSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) scroll *= 0.1f;
        if (scroll != 0f) playerFreq = Mathf.Clamp(playerFreq + scroll, minFreq, maxFreq);

        // дрожь памяти: слабая — обычно, сильная — после отказа от калибровки
        float target = targetFreq;
        if (AnomalyActive && !Captured)
            target += Mathf.Sin(Time.time * 7f) * (GameFlags.CalibrationRefused ? 0.05f : 0.02f);

        float diff = Mathf.Abs(playerFreq - target);
        bool inZone = diff <= captureThreshold && !Captured;

        if (inZone)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= captureHoldTime) Capture();
        }
        else holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 3f);

        if (Input.GetKeyDown(KeyCode.Escape)) Close();

        tone.freqA = target;
        tone.freqB = playerFreq;

        if (echoTimer > 0f) echoTimer -= Time.deltaTime;
        if (glitchTimer > 0f) glitchTimer -= Time.deltaTime;

        Draw(target, diff, inZone);
    }

    void Capture()
    {
        Captured = true;

        if (AnomalyActive)
        {
            GameFlags.AnomalySeen = true; // ← теперь Ким знает, что обсуждать

            echoTimer = 1.0f;
            glitchTimer = GameFlags.CalibrationRefused ? 2.2f : 1.4f;
            shake.Shake(0.8f);
            tone.detune = GameFlags.CalibrationRefused ? 0.12f : 0.06f;

            Debug.Log("АНОМАЛИЯ: посторонний фрагмент в потоке пациента. Источник не определён.");
            StartCoroutine(CloseAfter(3f, true));
        }
        else
        {
            string note = willCase ? " Аномалий не обнаружено. Файл закрыт." : " — файл пациента обновлён.";
            Debug.Log("ЧАСТОТА ЗАХВАЧЕНА: " + targetFreq.ToString("0.00") + " Hz" + note);
            StartCoroutine(CloseAfter(2f, false));
        }
    }

    System.Collections.IEnumerator CloseAfter(float seconds, bool nausea)
    {
        yield return new WaitForSeconds(seconds);
        Close();
        if (nausea) shake.Nausea(0.45f);
    }

    void Draw(float target, float diff, bool inZone)
    {
        tex.SetPixels(background);
        for (int x = 0; x < W; x += 6) tex.SetPixel(x, H / 2, new Color(0f, 0.25f, 0.3f));

        float glitchChance = GameFlags.CalibrationRefused ? 0.15f : 0.08f;
        bool glitch = glitchTimer > 0f || (AnomalyActive && inZone && Random.value < glitchChance);

        Color targetColor = Captured ? new Color(0.3f, 1f, 0.5f) : new Color(0.2f, 0.45f, 0.55f);
        Color playerColor = inZone ? new Color(0.4f, 1f, 0.6f) : new Color(0.3f, 0.85f, 1f);

        if (glitch)
        {
            DrawWave(target, new Color(1f, 0.15f, 0.25f), -4);
            DrawWave(target, new Color(0.2f, 0.6f, 1f), 4);
            DrawWave(playerFreq, new Color(1f, 0.15f, 0.25f), -4);
            DrawWave(playerFreq, new Color(0.2f, 0.6f, 1f), 4);
        }

        DrawWave(target, targetColor, 0);
        DrawWave(playerFreq, playerColor, 0);

        if (glitch) GlitchPass();

        tex.Apply();

        if (glitch) panelRT.anchoredPosition = new Vector2(Random.Range(-8, 8), Random.Range(-6, 6));
        else panelRT.anchoredPosition = Vector2.zero;

        freqLabel.text = playerFreq.ToString("0.00") + " Hz";

        if (echoTimer > 0f)
        {
            statusLabel.text = echoText;
            statusLabel.color = new Color(1f, 0.25f, 0.25f);
        }
        else if (Captured)
        {
            statusLabel.text = AnomalyActive ? Corrupt("РЕЗОНАНС — ЧАСТОТА ЗАХВАЧЕНА") : "РЕЗОНАНС — ЧАСТОТА ЗАХВАЧЕНА";
            statusLabel.color = new Color(0.3f, 1f, 0.5f);
        }
        else if (diff <= captureThreshold)
        {
            int percent = Mathf.RoundToInt(holdTimer / captureHoldTime * 100f);
            statusLabel.text = "СИНХРОНИЗАЦИЯ: " + percent + "%";
            statusLabel.color = new Color(0.3f, 1f, 0.5f);
        }
        else
        {
            statusLabel.text = "ПОИСК ЧАСТОТЫ...";
            statusLabel.color = new Color(0.7f, 0.7f, 0.7f);
        }
    }

    void DrawWave(float freq, Color color, int xShift)
    {
        float mid = H * 0.5f;
        float amp = H * 0.38f;
        for (int x = 0; x < W; x++)
        {
            float t = (float)x / W;
            float y = mid + Mathf.Sin(t * freq * 2f * Mathf.PI + Time.time * 2f) * amp;
            int py = (int)y;
            for (int dy = -1; dy <= 1; dy++)
            {
                int yy = py + dy;
                int xx = x + xShift;
                if (yy >= 0 && yy < H && xx >= 0 && xx < W) tex.SetPixel(xx, yy, color);
            }
        }
    }

    void GlitchPass()
    {
        for (int i = 0; i < 4; i++)
        {
            int y = Random.Range(0, H - 30);
            int h = Random.Range(4, 24);
            int shift = Random.Range(-60, 60);
            int w = W - Mathf.Abs(shift);
            if (w <= 0) continue;
            int srcX = shift < 0 ? -shift : 0;
            int dstX = shift > 0 ? shift : 0;
            Color[] band = tex.GetPixels(srcX, y, w, h);
            tex.SetPixels(dstX, y, w, h, band);
        }

        for (int i = 0; i < 6; i++)
        {
            int w = Random.Range(4, 26);
            int h = Random.Range(2, 6);
            Color[] noise = new Color[w * h];
            Color c = Random.value > 0.5f ? new Color(1f, 0.2f, 0.3f) : new Color(0.3f, 0.85f, 1f);
            for (int p = 0; p < noise.Length; p++) noise[p] = c;
            tex.SetPixels(Random.Range(0, W - w), Random.Range(0, H - h), w, h, noise);
        }
    }

    string Corrupt(string s)
    {
        char[] junk = { '#', '%', '&', '@', '?' };
        var sb = new System.Text.StringBuilder();
        foreach (char ch in s)
            sb.Append(Random.value < 0.12f ? junk[Random.Range(0, junk.Length)] : ch);
        return sb.ToString();
    }
}