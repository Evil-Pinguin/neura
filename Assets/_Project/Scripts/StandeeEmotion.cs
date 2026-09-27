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
        EnsureQuadMesh(); // вместо тонкого куба — плоский квад: иначе видны грани/зеркальная спина
        mat = GetComponent<MeshRenderer>().material; // инстанс — общий ассет не трогаем
        SetEmotion(defaultEmotion);
        blinkTimer = Random.Range(blinkInterval.x, blinkInterval.y);
    }

    // Односторонний квад 1x1 строго анфас (+z). Не зависим от меша в сцене.
    void EnsureQuadMesh()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null) return;
        var mesh = new Mesh { name = "StandeeQuad" };
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
        };
        mesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
        mesh.normals = new Vector3[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
        mf.mesh = mesh;
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
