using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Кат-сцены: наезд камеры + субтитры. Очередь для моментов, когда играть
// сразу нельзя (открыт диалог, идёт фейд). Пропуск — E / Esc.
public class CutsceneDirector : MonoBehaviour
{
    public static CutsceneDirector Instance { get; private set; }
    public static bool IsPlaying { get; private set; }

    public struct Shot
    {
        public float dur;
        public Vector3 camPos;
        public Vector3 lookAt;
        public string sub;
    }

    static string pendingId;
    static float pendingAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<CutsceneDirector>() != null) return;
        new GameObject("CutsceneDirector (auto)").AddComponent<CutsceneDirector>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Отложенный запуск: когда закроется диалог и пройдёт 1.5 c.
    public static void Queue(string id)
    {
        if (pendingId == null)
        {
            pendingId = id;
            pendingAt = Time.time;
        }
    }

    void Update()
    {
        if (IsPlaying || pendingId == null) return;
        if (Time.time - pendingAt < 1.5f) return;
        if (DialogRunner.Instance != null && DialogRunner.Instance.Active) return;
        if (MemoryDive.IsInMemory || SleepSystem.IsDreaming) return;
        string p = pendingId;
        pendingId = null;
        if (p == "refusal") StartCoroutine(PlayRoutine(RefusalShots()));
        else if (p == "critical") StartCoroutine(PlayRoutine(CriticalShots()));
    }

    public IEnumerator PlayRoutine(List<Shot> shots)
    {
        IsPlaying = true;
        var pc = FindObjectOfType<PlayerController>();
        var pi = FindObjectOfType<PlayerInteraction>();
        var cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        var shake = cam != null ? cam.GetComponent<CameraShake>() : null;
        var bob = cam != null ? cam.GetComponent<HeadBob>() : null;

        Vector3 savedPos = Vector3.zero;
        Quaternion savedRot = Quaternion.identity;
        if (cam != null)
        {
            savedPos = cam.transform.localPosition;
            savedRot = cam.transform.localRotation;
        }
        if (pc != null) pc.enabled = false;
        if (pi != null) pi.enabled = false;
        if (shake != null) shake.enabled = false;
        if (bob != null) bob.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        bool skip = false;
        if (cam != null)
        {
            foreach (var sh in shots)
            {
                Vector3 p0 = cam.transform.position;
                Quaternion r0 = cam.transform.rotation;
                Quaternion r1 = Quaternion.LookRotation(sh.lookAt - sh.camPos);
                if (!string.IsNullOrEmpty(sh.sub)) Announcer.Say(sh.sub, sh.dur);
                float t = 0f;
                while (t < sh.dur)
                {
                    if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) { skip = true; break; }
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / sh.dur));
                    cam.transform.position = Vector3.Lerp(p0, sh.camPos, k);
                    cam.transform.rotation = Quaternion.Slerp(r0, r1, k);
                    yield return null;
                }
                if (skip) break;
            }
            cam.transform.localPosition = savedPos;
            cam.transform.localRotation = savedRot;
        }

        if (shake != null) shake.enabled = true;
        if (bob != null) bob.enabled = true;
        if (pc != null) pc.enabled = true;
        if (pi != null) pi.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        IsPlaying = false;
    }

    // Первое погружение: камера подходит к станции. Позиции — от мира.
    public static List<Shot> DiveShots()
    {
        var shots = new List<Shot>();
        var station = GameObject.Find("RestoreStation");
        var cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (station == null || cam == null) return shots;
        Vector3 sp = station.transform.position;
        Vector3 to = sp - cam.transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.01f) to = cam.transform.forward;
        to.Normalize();
        Vector3 up = Vector3.up;
        shots.Add(new Shot
        {
            dur = 2.2f,
            camPos = sp - to * 1.7f + up * 1.5f,
            lookAt = sp + up * 1.2f,
            sub = "Ты кладёшь ладони на холодный металл.",
        });
        shots.Add(new Shot
        {
            dur = 2f,
            camPos = sp - to * 0.9f + up * 1.5f,
            lookAt = sp + up * 1.2f,
            sub = "Частота 7.13. Падение.",
        });
        return shots;
    }

    // Отказ от калибровки: наезд на Ким.
    public static List<Shot> RefusalShots()
    {
        var shots = new List<Shot>();
        var kim = GameObject.Find("KimStandee");
        var cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (kim == null || cam == null) return shots;
        Vector3 face = kim.transform.position + Vector3.up * 1.45f;
        Vector3 to = face - cam.transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.01f) to = cam.transform.forward;
        to.Normalize();
        shots.Add(new Shot
        {
            dur = 2.6f,
            camPos = face - to * 2.3f,
            lookAt = face,
            sub = "Ты же понимаешь, что теперь я обязана доложить координатору.",
        });
        shots.Add(new Shot
        {
            dur = 2.4f,
            camPos = face - to * 1.4f,
            lookAt = face,
            sub = "Ничего личного, Майк.",
        });
        return shots;
    }

    // Критическая стадия: камера плывёт, глитч.
    public static List<Shot> CriticalShots()
    {
        var shots = new List<Shot>();
        var cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return shots;
        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 p = cam.transform.position;
        GlitchFx.Burst(5f);
        shots.Add(new Shot
        {
            dur = 2.6f,
            camPos = p + fwd * 1.2f - Vector3.up * 0.25f,
            lookAt = p + fwd * 3f - Vector3.up * 1f,
            sub = "ТЫ ЕЩЁ ТЫ?",
        });
        shots.Add(new Shot
        {
            dur = 2.4f,
            camPos = p + fwd * 2f - Vector3.up * 0.5f,
            lookAt = p + fwd * 4f - Vector3.up * 1.5f,
            sub = "143.",
        });
        return shots;
    }
}
