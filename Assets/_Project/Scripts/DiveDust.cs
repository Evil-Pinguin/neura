using UnityEngine;

// Пыль в воздухе погружения и сна: частицы вокруг камеры. Bootstrap.
public class DiveDust : MonoBehaviour
{
    public static DiveDust Instance { get; private set; }

    ParticleSystem ps;
    ParticleSystem.EmissionModule emission;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<DiveDust>() != null) return;
        new GameObject("DiveDust (auto)").AddComponent<DiveDust>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (ps == null) return;
        var cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam != null) transform.position = cam.transform.position;
        emission.enabled = MemoryDive.IsInMemory || SleepSystem.IsDreaming;
    }

    void Build()
    {
        ps = gameObject.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 4f;
        main.startSpeed = 0.15f;
        main.startSize = 0.03f;
        main.startColor = new Color(0.7f, 0.8f, 1f, 0.35f);
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        emission = ps.emission;
        emission.rateOverTime = 10f;
        emission.enabled = false;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 5f;
    }
}
