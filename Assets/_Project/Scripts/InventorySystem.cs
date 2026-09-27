using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

        public bool IsOpenPublic => open;

    [System.Serializable]
    public class RealmSkin
    {
        public string title;
        public Color panelBg;
        public Color slotBg;
        public Color slotSelected;
        public Color glyph;
        public Color nameColor;
    }

    [Header("Ссылки")]
    [SerializeField] GameObject panel;
    [SerializeField] Image panelImage;
    [SerializeField] TMP_Text titleLabel;
    [SerializeField] Button[] slots;
    [SerializeField] TMP_Text itemName;
    [SerializeField] TMP_Text itemDesc;
    [SerializeField] Button detailButton;
    [SerializeField] PlayerController playerController;
    [SerializeField] PlayerInteraction playerInteraction;

    [Header("Скины")]
    [SerializeField] RealmSkin realitySkin;
    [SerializeField] RealmSkin memorySkin;

    [Header("Стартовые предметы")]
    [SerializeField] ItemAsset[] startingItems;

    readonly List<ItemAsset> items = new List<ItemAsset>();
    ItemRealm realm = ItemRealm.Reality;
    bool open;
    int selectedSlot = -1;
    float flickerTimer;
    bool flickering;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false); // панель прячется сама — галочку снимать не надо было
        foreach (var it in startingItems)
            if (it != null && !items.Contains(it)) items.Add(it);
    }

    void Update()
    {
        // ОТЛАДКА: временный переключатель мира. Удалим, когда появится система сна.
        if (Input.GetKeyDown(KeyCode.F9))
            SetRealm(realm == ItemRealm.Reality ? ItemRealm.Memory : ItemRealm.Reality);

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (open) Close();
            else
            {
                // переключаемся с дневника без конфликта клавиш
                if (JournalSystem.Instance != null && JournalSystem.Instance.IsOpen)
                    JournalSystem.Instance.ClosePublic();
                TryOpen();
            }
            return;
        }

        if (!open) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

        if (realm == ItemRealm.Memory) Flicker();
    }

    // предметы памяти нестабильны: глифы изредка мигают
    void Flicker()
    {
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f)
        {
            if (flickering) { flickering = false; flickerTimer = Random.Range(2f, 5f); ApplyGlyphAlpha(1f); }
            else { flickering = true; flickerTimer = 0.12f; ApplyGlyphAlpha(0.25f); }
        }
    }

    void ApplyGlyphAlpha(float a)
    {
        var skin = CurrentSkin;
        foreach (var b in slots)
        {
            var t = b.GetComponentInChildren<TMP_Text>();
            if (t != null) { var c = skin.glyph; c.a = a; t.color = c; }
        }
    }

    RealmSkin CurrentSkin => realm == ItemRealm.Reality ? realitySkin : memorySkin;

    public void SetRealm(ItemRealm r)
    {
        realm = r;
        selectedSlot = -1;
        if (open) { ApplySkin(); Refresh(); }
        if (QuickBar.Instance != null) QuickBar.Instance.Refresh();
    }

    public void AddItem(ItemAsset item)
    {
        if (item == null || items.Contains(item)) return;
        items.Add(item);
        Debug.Log("Взято: " + item.displayName);
        if (open) Refresh();
    }

    public bool Has(ItemAsset item) => items.Contains(item);

    void TryOpen()
    {
        // не открываемся поверх диалога, консоли и дневника
        if (DialogRunner.Instance != null && DialogRunner.Instance.Active) return;
        if (FrequencyConsole.Instance != null && FrequencyConsole.Instance.IsOpen) return;
        if (JournalSystem.Instance != null && JournalSystem.Instance.IsOpen) return;

        open = true;
        panel.SetActive(true);
        playerController.enabled = false;
        playerInteraction.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ApplySkin();
        Refresh();
    }

    void Close()
    {
        open = false;
        panel.SetActive(false);
        playerController.enabled = true;
        playerInteraction.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenPublic() { TryOpen(); }
    public void ClosePublic() { if (open) Close(); }

    void ApplySkin()
    {
        var skin = CurrentSkin;
        panelImage.color = skin.panelBg;
        titleLabel.text = skin.title;
        titleLabel.color = skin.glyph;
        itemName.color = skin.nameColor;
        foreach (var b in slots)
        {
            b.image.color = skin.slotBg;
            var t = b.GetComponentInChildren<TMP_Text>();
            if (t != null) { t.color = skin.glyph; t.text = ""; }
        }
        detailButton.gameObject.SetActive(false);
    }

    void Refresh()
    {
        var visible = items.Where(i => i.realm == realm).ToList();

        for (int i = 0; i < slots.Length; i++)
        {
            var t = slots[i].GetComponentInChildren<TMP_Text>();
            t.text = i < visible.Count ? visible[i].glyph : "";
            slots[i].image.color = (i == selectedSlot) ? CurrentSkin.slotSelected : CurrentSkin.slotBg;
        }

        if (selectedSlot >= 0 && selectedSlot < visible.Count) Show(visible[selectedSlot]);
        else Show(null);
    }

    void Show(ItemAsset item)
    {
        if (item == null)
        {
            itemName.text = "—";
            itemDesc.text = realm == ItemRealm.Reality ? "Пусто." : "Здесь когда-то что-то было.";
            detailButton.gameObject.SetActive(false);
            return;
        }

        itemName.text = item.displayName;
        itemDesc.text = item.description;
        detailButton.gameObject.SetActive(!string.IsNullOrEmpty(item.detail));
    }

    public void SelectSlot(int i)
    {
        if (!open) return;
        selectedSlot = i;
        Refresh();
    }

    public void ShowItemDetail()
    {
        if (!open) return;
        var visible = items.Where(i => i.realm == realm).ToList();
        if (selectedSlot < 0 || selectedSlot >= visible.Count) return;

        var item = visible[selectedSlot];
        if (!string.IsNullOrEmpty(item.detail))
        {
            itemDesc.text = item.detail;
            detailButton.gameObject.SetActive(false);
            Debug.Log("Осмотрено: " + item.displayName + " — обнаружена деталь.");
        }
    }
}