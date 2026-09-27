using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Полоса быстрых слотов: всегда видимые глифы носимых вещей.
// UI строится кодом поверх существующего Canvas — сцену трогать не нужно.
// Шрифт забираем у любого существующего TMP-текста, чтобы не тащить ассеты.
public class QuickBar : MonoBehaviour
{
    public static QuickBar Instance { get; private set; }

    [SerializeField] int maxSlots = 6;

    InventorySystem inv;
    GameObject root;
    readonly List<TMP_Text> glyphs = new List<TMP_Text>();
    readonly List<Image> backs = new List<Image>();

    void Awake()
    {
        Instance = this;
        inv = GetComponent<InventorySystem>();
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Build()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        TMP_FontAsset font = null;
        var any = canvas.GetComponentInChildren<TMP_Text>(true);
        if (any != null) font = any.font;

        root = new GameObject("QuickBar", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 18f);
        rt.sizeDelta = new Vector2(maxSlots * 50f, 46f);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        for (int i = 0; i < maxSlots; i++)
        {
            var slot = new GameObject("Slot" + i, typeof(RectTransform));
            slot.transform.SetParent(root.transform, false);
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 44f);
            var img = slot.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.3f);
            img.raycastTarget = false;
            backs.Add(img);

            var tg = new GameObject("Glyph", typeof(RectTransform));
            tg.transform.SetParent(slot.transform, false);
            var trt = tg.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var label = tg.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = 26f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.text = "";
            glyphs.Add(label);
        }
        Refresh();
    }

    void Update()
    {
        if (root == null) return;
        bool hide = inv != null && inv.IsOpenPublic;
        if (DialogRunner.Instance != null && DialogRunner.Instance.Active) hide = true;
        if (FrequencyConsole.Instance != null && FrequencyConsole.Instance.IsOpen) hide = true;
        if (JournalSystem.Instance != null && JournalSystem.Instance.IsOpen) hide = true;
        root.SetActive(!hide);
    }

    public void Refresh()
    {
        if (root == null || inv == null) return;
        var visible = inv.Items.Where(i => i != null && i.realm == inv.CurrentRealm)
            .Take(maxSlots).ToList();
        for (int i = 0; i < maxSlots; i++)
        {
            bool has = i < visible.Count;
            glyphs[i].text = has ? visible[i].glyph : "";
            backs[i].color = has
                ? new Color(0.05f, 0.12f, 0.14f, 0.6f)
                : new Color(0f, 0f, 0f, 0.3f);
        }
    }
}
