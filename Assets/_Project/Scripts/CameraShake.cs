using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    float trauma;
    float fovKick;
    float sedated;   // сколько ещё секунд действует "седация"
    float baseFov;
    Vector3 basePos;

    public Vector3 CurrentOffset { get; private set; } // текущее смещение для HeadBob
    public static float SyndromeTremor; // базовый тремор от синдрома (глушится седацией)

    void Awake()
    {
        basePos = transform.localPosition;
        baseFov = GetComponent<Camera>().fieldOfView;
    }

    public void Shake(float strength)
    {
        if (sedated > 0f) return; // лекарство глушит симптом
        trauma = Mathf.Clamp01(Mathf.Max(trauma, strength));
    }

    public void Nausea(float strength)
    {
        if (sedated > 0f) return;
        trauma = Mathf.Clamp01(Mathf.Max(trauma, strength));
        fovKick = Mathf.Max(fovKick, 16f);
    }

    public void SetSedation(float seconds)
    {
        sedated = Mathf.Max(sedated, seconds);
    }

    void Update()
    {
        if (sedated > 0f) sedated -= Time.deltaTime;

        trauma = Mathf.Max(0f, trauma - Time.deltaTime * 0.45f);
        float eff = Mathf.Max(trauma, sedated > 0f ? 0f : SyndromeTremor);
        if (eff <= 0f)
        {
            CurrentOffset = Vector3.zero;
            transform.localPosition = basePos;
            return;
        }

        float s = eff * eff;

        Vector3 offset = new Vector3(
            (Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f) * 0.3f,
            (Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f) * 0.3f,
            0f) * s;

        CurrentOffset = offset;
        transform.localPosition = basePos + offset;
        GetComponent<Camera>().fieldOfView = baseFov + fovKick * s;
    }
}