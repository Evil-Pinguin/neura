using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Бегущая строка субтитров сверху экрана: селектор, чужой голос, системное.
// Работает без настройки сцены (bootstrap), API — статическое.
public class Announcer : MonoBehaviour
{
    public static Announcer Instance { get; private set; }

    class Msg
    {
        public string text;
        public float dur;
        public Color color;
    }

    static readonly Queue<Msg> queue = new Queue<Msg>();

    TextMeshProUGUI label;
    Transform barTransform;
    TMP_FontAsset templateFont;
    float timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<Announcer>() != null) return;
        new GameObject("Announcer (auto)").AddComponent<Announcer>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Say(string text, float dur)
    {
        Say(text, dur, new Color(0.75f, 0.9f, 1f));
    }

    public static void Say(string text, float dur, Color color)
    {
        queue.Enqueue(new Msg { text = text, dur = dur, color = color });
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f && barTransform != null)
                barTransform.gameObject.SetActive(false);
            return;
        }
        if (queue.Count == 0 || label == null || barTransform == null) return;
        var m = queue.Dequeue();
        label.text = m.text;
        label.color = m.color;
        barTransform.gameObject.SetActive(true);
        timer = m.dur;
    }

    void BuildUI()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        foreach (var t in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            if (t != null && t.font != null) { templateFont = t.font; break; }
        }

        var bar = new GameObject("AnnouncerBar");
        bar.transform.SetParent(canvas.transform, false);
        var rt = bar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(-430f, -72f);
        rt.offsetMax = new Vector2(430f, -28f);
        var bg = bar.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.02f, 0.04f, 0.06f, 0.85f);
        bg.raycastTarget = false;

        var tgo = new GameObject("Text");
        tgo.transform.SetParent(bar.transform, false);
        var trt = tgo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14f, 4f);
        trt.offsetMax = new Vector2(-14f, -4f);
        label = tgo.AddComponent<TextMeshProUGUI>();
        if (templateFont != null) label.font = templateFont;
        label.fontSize = 19;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;

        bar.SetActive(false);
        barTransform = bar.transform;
    }
}
