using System.Collections;
using UnityEngine;

// Сон на диване в кабинете: Майк засыпает и оказывается
// в старом деревянном доме. Пробуждение — через DreamWake на кровати.
public class SleepSystem : MonoBehaviour
{
    public static SleepSystem Instance { get; private set; }
    public static bool IsDreaming { get; private set; }

    [Header("Настройки")]
    [SerializeField] float fadeTime = 1.5f;

    PlayerController playerController;
    PlayerInteraction playerInteraction;
    CharacterController cc;
    Camera playerCam;
    UnityEngine.UI.Image fadeImage;

    Vector3 savedPos;
    float savedYaw;
    bool transitioning;

    // окружение кабинета — восстановить при пробуждении
    bool savedEnv;
    bool fogOn;
    Color fogColor;
    FogMode fogMode;
    float fogDensity;
    float ambientIntensity;
    Light dirLight;
    Color dirColor;
    float dirIntensity;
    CameraClearFlags camFlags;
    Color camBg;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<SleepSystem>() != null) return;
        new GameObject("SleepSystem (auto)").AddComponent<SleepSystem>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        playerController = FindObjectOfType<PlayerController>();
        playerInteraction = FindObjectOfType<PlayerInteraction>();
        if (playerController != null) cc = playerController.GetComponent<CharacterController>();
        playerCam = FindObjectOfType<Camera>();

        BuildFade();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Sleep()
    {
        if (transitioning || IsDreaming || MemoryDive.IsInMemory) return;
        if (playerController == null) return;
        var spawn = GameObject.Find("DreamSpawn");
        if (spawn == null)
        {
            Debug.LogWarning("[Sleep] в сцене нет DreamSpawn.");
            return;
        }
        StartCoroutine(SleepRoutine(spawn.transform));
    }

    public void Wake()
    {
        if (transitioning || !IsDreaming) return;
        if (playerController == null) return;
        StartCoroutine(WakeRoutine());
    }

    IEnumerator SleepRoutine(Transform spawn)
    {
        transitioning = true;
        savedPos = playerController.transform.position;
        savedYaw = playerController.transform.eulerAngles.y;

        if (playerController != null) playerController.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;
        fadeImage.transform.SetAsLastSibling();

        yield return Fade(0f, 1f, fadeTime);

        if (!savedEnv) SaveEnv();
        ApplyDreamEnv();
        Teleport(spawn.position, spawn.eulerAngles.y);
        IsDreaming = true;
        if (InventorySystem.Instance != null) InventorySystem.Instance.SetRealm(ItemRealm.Memory);
        JournalSystem.Unlock("dream_house");
        Debug.Log("Сон: старый деревянный дом.");

        yield return Fade(1f, 0f, fadeTime);

        if (playerController != null) playerController.enabled = true;
        if (playerInteraction != null) playerInteraction.enabled = true;
        transitioning = false;
    }

    IEnumerator WakeRoutine()
    {
        transitioning = true;

        if (playerController != null) playerController.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;
        fadeImage.transform.SetAsLastSibling();

        yield return Fade(0f, 1f, fadeTime);

        RestoreEnv();
        Teleport(savedPos, savedYaw);
        IsDreaming = false;
        if (InventorySystem.Instance != null) InventorySystem.Instance.SetRealm(ItemRealm.Reality);
        Debug.Log("Майк проснулся на диване.");

        yield return Fade(1f, 0f, fadeTime);

        if (playerController != null) playerController.enabled = true;
        if (playerInteraction != null) playerInteraction.enabled = true;
        transitioning = false;
    }

    void Teleport(Vector3 pos, float yaw)
    {
        if (cc != null) cc.enabled = false;
        playerController.transform.position = pos;
        playerController.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cc != null) cc.enabled = true;
    }

    // ---------- фейд ----------

    void BuildFade()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        var go = new GameObject("SleepFade");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        fadeImage = go.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
        go.SetActive(false);
    }

    IEnumerator Fade(float from, float to, float dur)
    {
        if (fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        float t = 0f;
        Color c = fadeImage.color;
        while (t < dur)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
        if (to <= 0f) fadeImage.gameObject.SetActive(false);
    }

    // ---------- окружение ----------

    void SaveEnv()
    {
        savedEnv = true;
        fogOn = RenderSettings.fog;
        fogColor = RenderSettings.fogColor;
        fogMode = RenderSettings.fogMode;
        fogDensity = RenderSettings.fogDensity;
        ambientIntensity = RenderSettings.ambientIntensity;
        foreach (var l in FindObjectsOfType<Light>())
        {
            if (l.type == LightType.Directional) { dirLight = l; break; }
        }
        if (dirLight != null)
        {
            dirColor = dirLight.color;
            dirIntensity = dirLight.intensity;
        }
        if (playerCam != null)
        {
            camFlags = playerCam.clearFlags;
            camBg = playerCam.backgroundColor;
        }
    }

    void ApplyDreamEnv()
    {
        Color nightBg = new Color(0.005f, 0.008f, 0.016f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = nightBg;
        RenderSettings.fogDensity = 0.045f;
        RenderSettings.ambientIntensity = 0.15f;
        if (dirLight != null)
        {
            dirLight.color = new Color(0.35f, 0.45f, 0.65f);
            dirLight.intensity = 0.2f;
        }
        if (playerCam != null)
        {
            playerCam.clearFlags = CameraClearFlags.SolidColor;
            playerCam.backgroundColor = nightBg;
        }
    }

    void RestoreEnv()
    {
        if (!savedEnv) return;
        RenderSettings.fog = fogOn;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.ambientIntensity = ambientIntensity;
        if (dirLight != null)
        {
            dirLight.color = dirColor;
            dirLight.intensity = dirIntensity;
        }
        if (playerCam != null)
        {
            playerCam.clearFlags = camFlags;
            playerCam.backgroundColor = camBg;
        }
    }
}
