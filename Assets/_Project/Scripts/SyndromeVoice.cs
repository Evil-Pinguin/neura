using System.Collections;
using UnityEngine;

// «Чужой голос» — третья стадия и выше: комментирует действия, иногда врёт.
// Помеченное [ЛОЖЬ] — заведомо ложные подсказки.
public class SyndromeVoice : MonoBehaviour
{
    public static SyndromeVoice Instance { get; private set; }

    static readonly Color VoiceColor = new Color(0.85f, 0.55f, 1f);

    static readonly string[] Ambient = {
        "Ким тебе врёт.", // ЛОЖЬ
        "Частота Уилла — это твой пульс.", // ЛОЖЬ
        "Координатор — это ты.", // ЛОЖЬ?
        "Не закрывай кейс.",
        "За дверью никого нет. Проверь.",
        "Ты уже читал эту записку.",
        "143. Запомни число.",
        "Она не помнит. Ты помнишь за двоих.",
        "Кровь — это чернила. Записывай.",
        "Седьмой час. Восьмой. Девятый. Считай.",
    };

    float ambientTimer = 60f;
    UnityEngine.UI.Image bleedImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<SyndromeVoice>() != null) return;
        new GameObject("SyndromeVoice (auto)").AddComponent<SyndromeVoice>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildBleed();
        ambientTimer = Random.Range(50f, 80f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Syndrome.Stage < 3) return;
        ambientTimer -= Time.deltaTime;
        if (ambientTimer <= 0f)
        {
            ambientTimer = Random.Range(50f, 100f) - Syndrome.Stage * 8f;
            Say(Ambient[Random.Range(0, Ambient.Length)]);
        }
    }

    public static void OnDive()
    {
        if (Syndrome.Stage >= 1) Say("Снова вниз. Тебе здесь нравится. Признай.");
    }

    public static void OnSurface()
    {
        if (Syndrome.Stage >= 1) Say("Ты что-то забыл там. Что-то важное.");
    }

    public static void OnEcho()
    {
        if (Syndrome.Stage >= 2) Say("Смотри. Запоминай. Тебе пригодится. Наверное.");
    }

    public static void OnPickup(string what)
    {
        if (Syndrome.Stage < 2) return;
        if (Syndrome.Stage >= 3 && Random.value < 0.4f)
            Say("«" + what + "» тебе не понадобится. Выброси."); // ЛОЖЬ
        else
            Say("«" + what + "». Бери. Всё равно не твоё.");
    }

    public static void OnSleep()
    {
        if (Syndrome.Stage >= 1) Say("Спи. Дом ждёт.");
    }

    public static void OnWake()
    {
        if (Syndrome.Stage >= 2) Say("Ты храпел. Датчики сработали.");
    }

    public static void OnCalibrated()
    {
        Say("Тихо. Слишком тихо. Я подожду.");
    }

    // Кровотечение после погружения (стадия 2+): красная вспышка + тошнота.
    public static void Nosebleed(bool first)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.BleedRoutine(first));
    }

    IEnumerator BleedRoutine(bool first)
    {
        if (first) JournalSystem.Notify("Носовое кровотечение. Уже рутина.");
        var shake = FindObjectOfType<CameraShake>();
        if (shake != null) shake.Nausea(0.55f);
        if (bleedImage == null) yield break;
        bleedImage.gameObject.SetActive(true);
        float t = 0f;
        const float dur = 1.6f;
        Color c = bleedImage.color;
        while (t < dur)
        {
            t += Time.deltaTime;
            c.a = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI) * 0.45f;
            bleedImage.color = c;
            yield return null;
        }
        c.a = 0f;
        bleedImage.color = c;
        bleedImage.gameObject.SetActive(false);
    }

    static void Say(string line)
    {
        Announcer.Say("«" + line + "»", 4.5f, VoiceColor);
    }

    void BuildBleed()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        var go = new GameObject("NosebleedFlash");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        bleedImage = go.AddComponent<UnityEngine.UI.Image>();
        bleedImage.sprite = VignetteSprite();
        bleedImage.color = new Color(0.45f, 0.02f, 0.03f, 0f);
        bleedImage.raycastTarget = false;
        go.SetActive(false);
    }

    // Радиальная виньетка: прозрачный центр, плотные края.
    static UnityEngine.Sprite vignette;
    static UnityEngine.Sprite VignetteSprite()
    {
        if (vignette != null) return vignette;
        int SZ = 256;
        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                float dx = (x / (float)SZ - 0.5f) * 2f;
                float dy = (y / (float)SZ - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((d - 0.45f) / 0.55f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        vignette = UnityEngine.Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f));
        return vignette;
    }
}
