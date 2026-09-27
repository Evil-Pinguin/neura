using System.Collections;
using UnityEngine;

// Протокол «Глубокое погружение»: перемещение в воспоминание пациента.
// Подписывается на Interactable станции реставрации, телепортирует игрока
// в пространство памяти (строит MemorySpace), переключает мир на Memory.
// Выход — через MemoryExit в дальней комнате.
// Условие: аномалия Уилла зафиксирована (GameFlags.AnomalySeen).
// Если локация запечена в сцену (меню NeuralCanvas -> Bake), используется она.
public class MemoryDive : MonoBehaviour
{
    public static MemoryDive Instance { get; private set; }
    public static bool IsInMemory { get; private set; }

    [Header("Настройки")]
    [SerializeField] string stationName = "RestoreStation";
    [SerializeField] Vector3 memoryOrigin = MemorySpace.DefaultOrigin;
    [SerializeField] float fadeTime = 0.6f;

    PlayerController playerController;
    PlayerInteraction playerInteraction;
    CharacterController cc;
    Camera playerCam;
    CameraShake shake;
    UnityEngine.UI.Image fadeImage;

    Vector3 savedPos;
    float savedYaw;
    bool transitioning;
    bool spaceBuilt;
    Vector3 spawnPos;
    float spawnYaw;

    // окружение лаборатории — восстановить при выходе
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
        if (FindObjectOfType<MemoryDive>() != null) return;
        new GameObject("MemoryDive (auto)").AddComponent<MemoryDive>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        playerController = FindObjectOfType<PlayerController>();
        playerInteraction = FindObjectOfType<PlayerInteraction>();
        if (playerController != null) cc = playerController.GetComponent<CharacterController>();
        playerCam = FindObjectOfType<Camera>();
        shake = FindObjectOfType<CameraShake>();

        BuildFade();

        var station = GameObject.Find(stationName);
        if (station == null)
        {
            Debug.LogWarning("[Dive] станция не найдена: " + stationName);
            return;
        }
        var inter = station.GetComponent<Interactable>();
        if (inter == null)
        {
            Debug.LogWarning("[Dive] на станции нет Interactable.");
            return;
        }
        inter.onInteract.AddListener(Dive);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Dive()
    {
        if (transitioning || IsInMemory) return;
        if (playerController == null) return;
        if (!GameFlags.AnomalySeen)
        {
            JournalSystem.Notify("Протокол недоступен: сначала стабилизируй частоту пациента");
            Debug.Log("Погружение невозможно: аномалия не зафиксирована.");
            return;
        }
        StartCoroutine(DiveRoutine());
    }

    public void Surface()
    {
        if (transitioning || !IsInMemory) return;
        if (playerController == null) return;
        StartCoroutine(SurfaceRoutine());
    }

    IEnumerator DiveRoutine()
    {
        transitioning = true;
        if (!spaceBuilt)
        {
            if (!TryUseBakedSpace(out spawnPos, out spawnYaw))
                MemorySpace.Build(memoryOrigin, out spawnPos, out spawnYaw);
            spaceBuilt = true;
        }

        savedPos = playerController.transform.position;
        savedYaw = playerController.transform.eulerAngles.y;

        if (!GameFlags.FirstDiveDone && CutsceneDirector.Instance != null)
            yield return StartCoroutine(CutsceneDirector.Instance.PlayRoutine(CutsceneDirector.DiveShots()));

        if (playerController != null) playerController.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;
        fadeImage.transform.SetAsLastSibling();

        yield return Fade(0f, 1f, fadeTime);

        if (!savedEnv) SaveEnv();
        ApplyMemoryEnv();
        Teleport(spawnPos, spawnYaw);
        IsInMemory = true;
        if (InventorySystem.Instance != null) InventorySystem.Instance.SetRealm(ItemRealm.Memory);
        if (shake != null) shake.Nausea(0.5f);

        if (!GameFlags.FirstDiveDone)
        {
            GameFlags.FirstDiveDone = true;
            JournalSystem.Unlock("dive_first");
        }
        Debug.Log("ГЛУБОКОЕ ПОГРУЖЕНИЕ: пространство памяти Уилла.");
        SyndromeVoice.OnDive();

        yield return Fade(1f, 0f, fadeTime + 0.3f);

        if (playerController != null) playerController.enabled = true;
        if (playerInteraction != null) playerInteraction.enabled = true;
        transitioning = false;
    }

    IEnumerator SurfaceRoutine()
    {
        transitioning = true;

        if (playerController != null) playerController.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;
        fadeImage.transform.SetAsLastSibling();

        yield return Fade(0f, 1f, fadeTime);

        RestoreEnv();
        Teleport(savedPos, savedYaw);
        IsInMemory = false;
        if (InventorySystem.Instance != null) InventorySystem.Instance.SetRealm(ItemRealm.Reality);
        JournalSystem.Unlock("dive_return");
        Syndrome.OnDiveSurfaced();
        SyndromeVoice.OnSurface();
        Debug.Log("Возвращение из воспоминания.");

        yield return Fade(1f, 0f, fadeTime + 0.3f);

        if (playerController != null) playerController.enabled = true;
        if (playerInteraction != null) playerInteraction.enabled = true;
        transitioning = false;
    }

    // сцена уже содержит запечённую локацию — строить ничего не надо,
    // спавн берём с маркера (он дитя корня и едет вместе с ним)
    bool TryUseBakedSpace(out Vector3 spawn, out float yaw)
    {
        var root = GameObject.Find("MemorySpace");
        if (root == null)
        {
            spawn = memoryOrigin + new Vector3(0f, 1.1f, 1.2f);
            yaw = 0f;
            return false;
        }
        var marker = GameObject.Find("MemorySpawn");
        if (marker != null)
        {
            spawn = marker.transform.position;
            yaw = marker.transform.eulerAngles.y;
        }
        else
        {
            spawn = root.transform.position + new Vector3(0f, 1.1f, 1.2f);
            yaw = root.transform.eulerAngles.y;
        }
        Debug.Log("[Dive] использую запечённую локацию из сцены.");
        return true;
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
        var go = new GameObject("DiveFade");
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

    void ApplyMemoryEnv()
    {
        Color voidBg = new Color(0.008f, 0.015f, 0.03f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = voidBg;
        RenderSettings.fogDensity = 0.035f;
        RenderSettings.ambientIntensity = 0.25f;
        if (dirLight != null)
        {
            dirLight.color = new Color(0.55f, 0.70f, 0.90f);
            dirLight.intensity = 0.35f;
        }
        if (playerCam != null)
        {
            playerCam.clearFlags = CameraClearFlags.SolidColor;
            playerCam.backgroundColor = voidBg;
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
