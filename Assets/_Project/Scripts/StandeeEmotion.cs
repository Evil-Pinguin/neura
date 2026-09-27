using System.Collections;
using UnityEngine;

// Эмоции стенда Кима: переключает текстуру по репликам диалога,
// держит фиксированный рост пересчётом ширины, моргает, после
// диалога возвращается к бумагам.
public class StandeeEmotion : MonoBehaviour
{
    public static StandeeEmotion Instance { get; private set; }

    [Header("Эмоции (параллельные массивы: ключ -> текстура)")]
    [SerializeField] string[] emotionKeys;
    [SerializeField] Texture2D[] emotionTextures;

    [Header("Моргание: закрытые глаза для ключей")]
    [SerializeField] string[] blinkKeys;
    [SerializeField] Texture2D[] blinkClosed;

    [Header("Настройки")]
    [SerializeField] string defaultEmotion = "papers";
    [SerializeField] string kimSpeaker = "Ким";
    [SerializeField] float standHeight = 1.7f;
    [SerializeField] float revertDelay = 1.2f;
    [SerializeField] Vector2 blinkInterval = new Vector2(2.5f, 5f);
    [SerializeField] float blinkDuration = 0.12f;

    Material mat;
    string current;
    Coroutine revertRoutine;
    float blinkTimer;

    void Awake()
    {
        Instance = this;
        mat = GetComponent<MeshRenderer>().material; // инстанс — общий ассет не трогаем
        SetEmotion(defaultEmotion);
        blinkTimer = Random.Range(blinkInterval.x, blinkInterval.y);
    }

    void Update()
    {
        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            blinkTimer = Random.Range(blinkInterval.x, blinkInterval.y);
            if (Find(blinkKeys, blinkClosed, current) != null)
                StartCoroutine(Blink());
        }
    }

    IEnumerator Blink()
    {
        string key = current;
        mat.mainTexture = Find(blinkKeys, blinkClosed, key);
        yield return new WaitForSeconds(blinkDuration);
        if (current == key) // эмоцию не сменили за время моргания
            mat.mainTexture = Find(emotionKeys, emotionTextures, key);
    }

    public void SetEmotion(string key)
    {
        Texture2D tex = Find(emotionKeys, emotionTextures, key);
        if (tex == null) return;
        if (revertRoutine != null) { StopCoroutine(revertRoutine); revertRoutine = null; }
        current = key;
        mat.mainTexture = tex;
        transform.localScale = new Vector3(standHeight * tex.width / tex.height, standHeight, 1f);
    }

    // Вызывается из DialogRunner на каждой реплике.
    public void OnDialogLine(string speaker, string emotion)
    {
        if (speaker != kimSpeaker) return;
        if (!string.IsNullOrEmpty(emotion)) SetEmotion(emotion);
    }

    // Вызывается из DialogRunner при закрытии диалога.
    public void OnDialogClosed()
    {
        if (revertRoutine != null) StopCoroutine(revertRoutine);
        revertRoutine = StartCoroutine(RevertSoon());
    }

    IEnumerator RevertSoon()
    {
        yield return new WaitForSeconds(revertDelay);
        revertRoutine = null;
        SetEmotion(defaultEmotion);
    }

    static Texture2D Find(string[] keys, Texture2D[] textures, string key)
    {
        if (keys == null || textures == null || string.IsNullOrEmpty(key)) return null;
        for (int i = 0; i < keys.Length && i < textures.Length; i++)
            if (keys[i] == key) return textures[i];
        return null;
    }
}
