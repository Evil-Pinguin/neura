using UnityEngine;
using UnityEngine.UI;

// Портрет говорящего в диалоге (слева внизу, поверх мира, под панелью).
// Текстуры — оригиналы Ким, привязка по (speaker, emotion) реплики.
public class DialogPortrait : MonoBehaviour
{
    public static DialogPortrait Instance { get; private set; }

    [Header("Привязка (параллельные массивы: ключ эмоции -> текстура)")]
    [SerializeField] string portraitSpeaker = "Ким";
    [SerializeField] string[] emotionKeys;
    [SerializeField] Texture2D[] emotionTextures;

    [Header("Настройки")]
    [SerializeField] float portraitHeight = 340f;
    [SerializeField] Vector2 anchoredPos = new Vector2(36f, 30f);

    RawImage img;

    void Awake()
    {
        Instance = this;
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("DialogPortrait: Canvas не найден"); return; }

        var go = new GameObject("DialogPortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = anchoredPos;
        img = go.GetComponent<RawImage>();
        img.raycastTarget = false;
        go.SetActive(false);
    }

    // Вызывается из DialogRunner на каждой реплике.
    public void ShowForLine(string speaker, string emotion)
    {
        if (img == null) return;
        if (speaker != portraitSpeaker) { Hide(); return; }
        Texture2D tex = Find(emotion);
        if (tex != null)
        {
            img.texture = tex;
            img.rectTransform.sizeDelta = new Vector2(portraitHeight * tex.width / tex.height, portraitHeight);
        }
        // пустая/неизвестная эмоция — оставляем прошлый портрет
        img.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (img != null) img.gameObject.SetActive(false);
    }

    Texture2D Find(string key)
    {
        if (emotionKeys == null || emotionTextures == null || string.IsNullOrEmpty(key)) return null;
        for (int i = 0; i < emotionKeys.Length && i < emotionTextures.Length; i++)
            if (emotionKeys[i] == key) return emotionTextures[i];
        return null;
    }
}
