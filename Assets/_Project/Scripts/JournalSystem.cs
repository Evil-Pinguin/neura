using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Дневник Майка: «Мои отчёты».
// Записи — ScriptableObject'ы в Resources/Journal, открываются по id:
//   JournalSystem.Unlock("will_anomaly");        // с варнингом, если id нет
//   JournalSystem.TryUnlock("pickup_" + item.name); // тихо, для generic-хуков
// UI строится кодом поверх существующего Canvas — сцену настраивать не нужно.
// [J] открыть/закрыть, ↑↓ или клик — выбор, [E] — раскрыть анализ, [Tab] — к вещам.
public class JournalSystem : MonoBehaviour
{
    public static JournalSystem Instance { get; private set; }
    public bool IsOpen => open;

    [Header("Ссылки (можно не заполнять)")]
    [SerializeField] GameObject panel; // пусто = UI построится сам
    [SerializeField] PlayerController playerController; // пусто = найдётся сам
    [SerializeField] PlayerInteraction playerInteraction;

    [Header("Стартовые записи (id из Resources/Journal)")]
    [SerializeField] string[] starterIds = { "report_will_713", "memo_protocol" };

    [Header("Клавиши")]
    [SerializeField] KeyCode toggleKey = KeyCode.J;

    const string ResPath = "Journal/";

    // прогресс — статика, чтобы пережить перезагрузку сцены
    static readonly HashSet<string> unlockedIds = new HashSet<string>();
    static readonly HashSet<string> readIds = new HashSet<string>();
    static readonly List<string> unlockOrder = new List<string>();

    readonly Dictionary<string, JournalEntry> cache = new Dictionary<string, JournalEntry>();

    bool open;
    int selected = -1;
    bool detailShown;
    float corruptTimer;

    // рантайм-UI
    ScrollRect listScroll;
    RectTransform listContent;
    ScrollRect bodyScroll;
    RectTransform bodyContent;
    TextMeshProUGUI headerLabel;
    TextMeshProUGUI titleLabel;
    TextMeshProUGUI metaLabel;
    TextMeshProUGUI bodyLabel;
    Button detailButton;
    CanvasGroup toastGroup;
    TextMeshProUGUI toastLabel;
    TMP_FontAsset templateFont;
    float toastTimer;
    readonly Queue<string> toastQueue = new Queue<string>();

    static readonly Color BgColor = new Color(0.02f, 0.04f, 0.06f, 0.97f);
    static readonly Color RowColor = new Color(0.05f, 0.08f, 0.11f, 1f);
    static readonly Color SelColor = new Color(0.07f, 0.20f, 0.24f, 1f);
    static readonly Color DimColor = new Color(0.55f, 0.60f, 0.62f, 1f);

    // ---------- bootstrap: журнал работает без настройки сцены ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<JournalSystem>() != null) return;
        new GameObject("JournalSystem (auto)").AddComponent<JournalSystem>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (playerController == null) playerController = FindObjectOfType<PlayerController>();
        if (playerInteraction == null) playerInteraction = FindObjectOfType<PlayerInteraction>();
        if (panel == null) BuildUI();
        panel.SetActive(false);

        if (starterIds != null)
            foreach (var id in starterIds) UnlockInternal(id, true);

        if (CurrentList().Count > 0) selected = 0;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- публичное API ----------

    public static void Unlock(string id)
    {
        if (Instance != null) Instance.UnlockInternal(id, false);
    }

    public static void TryUnlock(string id)
    {
        if (Instance != null) Instance.UnlockInternal(id, true);
    }

    public static void Notify(string text)
    {
        if (Instance != null) Instance.EnqueueToast(text);
    }

    public static bool Has(string id)
    {
        return !string.IsNullOrEmpty(id) && unlockedIds.Contains(id);
    }

    public static int UnreadCount()
    {
        int n = 0;
        foreach (var id in unlockOrder)
            if (!readIds.Contains(id)) n++;
        return n;
    }

    public void OpenPublic() { TryOpen(); }
    public void ClosePublic() { if (open) Close(); }
    public void TogglePublic() { if (open) Close(); else TryOpen(); }

    // ---------- прогресс ----------

    void UnlockInternal(string id, bool silent)
    {
        if (string.IsNullOrEmpty(id) || unlockedIds.Contains(id)) return;
        var e = LoadEntry(id);
        if (e == null)
        {
            if (!silent) Debug.LogWarning("[Journal] нет записи: " + id);
            return;
        }
        unlockedIds.Add(id);
        unlockOrder.Add(id);
        Achievements.OnJournal(id);
        if (selected < 0) selected = 0;
        Debug.Log("Архивировано: " + e.title);
        if (open) Refresh();
        else EnqueueToast("Новый отчёт архивирован — [" + toggleKey + "]");
    }

    JournalEntry LoadEntry(string id)
    {
        if (cache.TryGetValue(id, out var e)) return e;
        e = Resources.Load<JournalEntry>(ResPath + id);
        cache[id] = e;
        return e;
    }

    List<JournalEntry> CurrentList()
    {
        var list = new List<JournalEntry>();
        foreach (var id in unlockOrder)
        {
            var e = LoadEntry(id);
            if (e != null) list.Add(e);
        }
        list.Sort((a, b) =>
        {
            int byOrder = a.sortOrder.CompareTo(b.sortOrder);
            if (byOrder != 0) return byOrder;
            return unlockOrder.IndexOf(a.entryId).CompareTo(unlockOrder.IndexOf(b.entryId));
        });
        return list;
    }

    // ---------- открытие/закрытие ----------

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (open) Close();
            else
            {
                // переключаемся с инвентаря без конфликта клавиш
                if (InventorySystem.Instance != null && InventorySystem.Instance.IsOpenPublic)
                    InventorySystem.Instance.ClosePublic();
                TryOpen();
            }
            return;
        }

        if (!open) { TickToast(); return; }
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Close();
            if (InventorySystem.Instance != null) InventorySystem.Instance.OpenPublic();
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(1);
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)) ShowDetail();

        TickToast();

        // повреждённые записи мерцают, пока их читают
        var list = CurrentList();
        if (selected >= 0 && selected < list.Count && list[selected].corruption > 0f)
        {
            corruptTimer -= Time.deltaTime;
            if (corruptTimer <= 0f)
            {
                corruptTimer = 0.35f;
                RenderBody(list[selected]);
            }
        }
    }

    void TryOpen()
    {
        // не открываемся поверх диалога, консоли и инвентаря
        if (DialogRunner.Instance != null && DialogRunner.Instance.Active) return;
        if (FrequencyConsole.Instance != null && FrequencyConsole.Instance.IsOpen) return;
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsOpenPublic) return;

        open = true;
        detailShown = false;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        if (playerController != null) playerController.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Refresh();
    }

    void Close()
    {
        open = false;
        detailShown = false;
        panel.SetActive(false);
        if (playerController != null) playerController.enabled = true;
        if (playerInteraction != null) playerInteraction.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ---------- отрисовка ----------

    void Refresh()
    {
        var list = CurrentList();
        if (selected < 0 && list.Count > 0) selected = 0;
        if (selected >= list.Count) selected = list.Count - 1;

        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);

        for (int i = 0; i < list.Count; i++)
            BuildRow(list[i], i == selected, !readIds.Contains(list[i].entryId));

        LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        listScroll.verticalNormalizedPosition = 1f;

        // выбранную считаем прочитанной до подсчёта счётчика
        if (selected >= 0 && selected < list.Count)
            readIds.Add(list[selected].entryId);

        int unread = UnreadCount();
        headerLabel.text = unread > 0 ? "МОИ ОТЧЁТЫ  •  " + unread + " нов." : "МОИ ОТЧЁТЫ";

        if (selected >= 0 && selected < list.Count) RenderDetail(list[selected]);
        else
        {
            titleLabel.text = "—";
            metaLabel.text = "";
            bodyLabel.text = "Нет записей.";
            detailButton.gameObject.SetActive(false);
        }
    }

    void BuildRow(JournalEntry e, bool isSelected, bool isUnread)
    {
        var go = new GameObject("Row_" + e.entryId);
        MakeRT(go, listContent, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 54f;
        le.flexibleWidth = 1f;

        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = btn.colors;
        colors.normalColor = isSelected ? SelColor : RowColor;
        colors.highlightedColor = new Color(0.09f, 0.24f, 0.29f, 1f);
        colors.pressedColor = new Color(0.06f, 0.16f, 0.20f, 1f);
        colors.selectedColor = SelColor;
        btn.colors = colors;
        img.color = isSelected ? SelColor : RowColor;

        btn.onClick.AddListener(() => SelectEntry(e));

        var tmp = MakeLabel("Label", go.transform,
            Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4),
            19, TextAlignmentOptions.Left, CatColor(e.category), false, false);
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.text = isUnread ? e.title + "  • новое" : e.title;
    }

    void RenderDetail(JournalEntry e)
    {
        titleLabel.text = e.title;
        titleLabel.color = CatColor(e.category);
        metaLabel.text = e.dateStamp + "   •   оператор " + e.operatorId;
        RenderBody(e);
        detailButton.gameObject.SetActive(!string.IsNullOrEmpty(e.detail) && !detailShown);
        corruptTimer = 0f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent);
        bodyScroll.verticalNormalizedPosition = 1f;
    }

    void RenderBody(JournalEntry e)
    {
        string t = ApplyCorruption(e.body, e.corruption);
        if (detailShown && !string.IsNullOrEmpty(e.detail))
            t += "\n\n— — —\n" + ApplyCorruption(e.detail, e.corruption);
        bodyLabel.text = t;
    }

    public void ShowDetail()
    {
        if (!open) return;
        var list = CurrentList();
        if (selected < 0 || selected >= list.Count) return;
        var e = list[selected];
        if (string.IsNullOrEmpty(e.detail) || detailShown) return;
        detailShown = true;
        RenderBody(e);
        detailButton.gameObject.SetActive(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent);
        Debug.Log("Анализ раскрыт: " + e.title);
    }

    void SelectEntry(JournalEntry e)
    {
        var list = CurrentList();
        int i = list.IndexOf(e);
        if (i < 0) return;
        selected = i;
        detailShown = false;
        Refresh();
    }

    void MoveSelection(int dir)
    {
        var list = CurrentList();
        if (list.Count == 0) return;
        if (selected < 0) selected = 0;
        else selected = (selected + dir + list.Count) % list.Count;
        detailShown = false;
        Refresh();
    }

    string ApplyCorruption(string s, float amount)
    {
        if (amount <= 0f || string.IsNullOrEmpty(s)) return s;
        char[] junk = { '#', '%', '&', '@', '?', '*', '=' };
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char ch in s)
        {
            if (char.IsLetterOrDigit(ch) && Random.value < amount)
                sb.Append(junk[Random.Range(0, junk.Length)]);
            else
                sb.Append(ch);
        }
        return sb.ToString();
    }

    static Color CatColor(JournalCategory c)
    {
        switch (c)
        {
            case JournalCategory.Anomaly: return new Color(1f, 0.42f, 0.42f);
            case JournalCategory.Symptom: return new Color(1f, 0.85f, 0.45f);
            default: return new Color(0.55f, 0.90f, 1f);
        }
    }

    // ---------- тост «архивировано» ----------

    void EnqueueToast(string text)
    {
        toastQueue.Enqueue(text);
    }

    void TickToast()
    {
        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f)
                toastGroup.gameObject.SetActive(false);
            else
                toastGroup.alpha = Mathf.Clamp01(toastTimer);
        }
        else if (toastQueue.Count > 0)
        {
            toastLabel.text = toastQueue.Dequeue();
            toastGroup.gameObject.SetActive(true);
            toastGroup.alpha = 1f;
            toastTimer = 3f;
        }
    }

    // ---------- построение UI ----------

    void BuildUI()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("JournalCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<GraphicRaycaster>();
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // шрифт берём у существующих TMP, чтобы кириллица выглядела так же, как в игре
        foreach (var t in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            if (t != null && t.font != null) { templateFont = t.font; break; }
        }

        var root = new GameObject("JournalPanel");
        MakeRT(root, canvas.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-470, -300), new Vector2(470, 300));
        var bg = root.AddComponent<Image>();
        bg.color = BgColor;
        panel = root;

        headerLabel = MakeLabel("Header", root.transform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -48), new Vector2(660, -10),
            30, TextAlignmentOptions.Left, new Color(0.80f, 0.93f, 0.96f), true, false);
        headerLabel.text = "МОИ ОТЧЁТЫ";

        var closeHint = MakeLabel("CloseHint", root.transform,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-280, -44), new Vector2(-24, -12),
            16, TextAlignmentOptions.Right, DimColor, false, false);
        closeHint.text = "[J] / [Esc] — закрыть";

        // список слева
        var listObj = new GameObject("EntryList");
        MakeRT(listObj, root.transform,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(24, 56), new Vector2(320, -64));
        var listBg = listObj.AddComponent<Image>();
        listBg.color = new Color(0.04f, 0.06f, 0.09f, 1f);
        MakeScroll(listObj, out listScroll, out listContent);
        var lg = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        lg.padding = new RectOffset(6, 6, 6, 6);
        lg.spacing = 6;
        lg.childControlWidth = true;
        lg.childControlHeight = false;
        lg.childForceExpandWidth = true;
        lg.childForceExpandHeight = false;
        var lf = listContent.gameObject.AddComponent<ContentSizeFitter>();
        lf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // текст справа
        var right = new GameObject("Detail");
        MakeRT(right, root.transform,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(344, 56), new Vector2(-24, -64));

        titleLabel = MakeLabel("DetailTitle", right.transform,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(0, -4),
            26, TextAlignmentOptions.Left, Color.white, true, true);
        metaLabel = MakeLabel("DetailMeta", right.transform,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -66), new Vector2(0, -42),
            16, TextAlignmentOptions.Left, DimColor, false, false);

        var bodyObj = new GameObject("BodyScroll");
        MakeRT(bodyObj, right.transform,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 52), new Vector2(0, -72));
        MakeScroll(bodyObj, out bodyScroll, out bodyContent);
        bodyLabel = bodyContent.gameObject.AddComponent<TextMeshProUGUI>();
        if (templateFont != null) bodyLabel.font = templateFont;
        bodyLabel.fontSize = 20;
        bodyLabel.alignment = TextAlignmentOptions.TopLeft;
        bodyLabel.color = new Color(0.85f, 0.90f, 0.92f);
        bodyLabel.enableWordWrapping = true;
        bodyLabel.raycastTarget = false;
        var bf = bodyContent.gameObject.AddComponent<ContentSizeFitter>();
        bf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var detailObj = new GameObject("DetailButton");
        MakeRT(detailObj, right.transform,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 44));
        var detailImg = detailObj.AddComponent<Image>();
        detailImg.color = new Color(0.07f, 0.20f, 0.24f, 1f);
        detailButton = detailObj.AddComponent<Button>();
        detailButton.targetGraphic = detailImg;
        detailButton.navigation = new Navigation { mode = Navigation.Mode.None };
        var btnLabel = MakeLabel("Label", detailObj.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            19, TextAlignmentOptions.Center, new Color(0.55f, 0.90f, 1f), false, false);
        btnLabel.text = "Раскрыть анализ";
        detailButton.onClick.AddListener(ShowDetail);

        var hint = MakeLabel("Hint", root.transform,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 8), new Vector2(-24, 44),
            16, TextAlignmentOptions.Center, DimColor, false, false);
        hint.text = "[J] закрыть   •   [Tab] вещи   •   ↑↓ выбор   •   [E] раскрыть анализ";

        // тост — дитя канваса, виден и при закрытом дневнике
        var toastObj = new GameObject("JournalToast");
        MakeRT(toastObj, canvas.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-300, 240), new Vector2(300, 284));
        var toastBg = toastObj.AddComponent<Image>();
        toastBg.color = new Color(0.02f, 0.05f, 0.07f, 0.92f);
        toastBg.raycastTarget = false;
        toastGroup = toastObj.AddComponent<CanvasGroup>();
        toastLabel = MakeLabel("ToastText", toastObj.transform,
            Vector2.zero, Vector2.one, new Vector2(16, 4), new Vector2(-16, -4),
            20, TextAlignmentOptions.Center, new Color(0.55f, 0.90f, 1f), false, true);
        toastObj.SetActive(false);
    }

    static RectTransform MakeRT(GameObject go, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    TextMeshProUGUI MakeLabel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        float size, TextAlignmentOptions align, Color color, bool bold, bool wrap)
    {
        var go = new GameObject(name);
        MakeRT(go, parent, anchorMin, anchorMax, offsetMin, offsetMax);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (templateFont != null) tmp.font = templateFont;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.enableWordWrapping = wrap;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void MakeScroll(GameObject scrollObj, out ScrollRect scroll, out RectTransform content)
    {
        scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        var vp = new GameObject("Viewport");
        var vpRT = MakeRT(vp, scrollObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        vp.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        vp.AddComponent<Mask>().showMaskGraphic = false;

        var ct = new GameObject("Content");
        content = MakeRT(ct, vp.transform,
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1f);

        scroll.viewport = vpRT;
        scroll.content = content;
    }
}
