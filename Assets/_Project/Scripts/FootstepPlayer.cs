using UnityEngine;

// Звуки шагов без аудиофайлов: все сэмплы синтезируются в Awake
// (фильтрованный шум + резонанс). Поверхность определяется рейкастом:
// дерево в доме звучит иначе, чем бетон лаборатории. Плюс глухой удар
// при приземлении после прыжка.
public class FootstepPlayer : MonoBehaviour
{
    [Header("Громкость")]
    [SerializeField] float walkVolume = 0.45f;
    [SerializeField] float sprintVolume = 0.7f;

    CharacterController cc;
    HeadBob bob;
    AudioSource src;
    AudioClip[] concrete = new AudioClip[3];
    AudioClip[] wood = new AudioClip[3];
    AudioClip landClip;
    bool wasGrounded = true;
    float fallSpeed;

    static readonly string[] WoodHints =
    {
        "Wood", "DFloor", "DTable", "DBed", "DWall",
        "DWin", "DBeam", "DGround", "Bench", "SideTable", "Sofa", "Counter"
    };

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        bob = GetComponentInChildren<HeadBob>();
        src = GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        MakeClips();
    }

    void OnEnable()
    {
        if (bob != null) bob.Stepped += OnStep;
    }

    void OnDisable()
    {
        if (bob != null) bob.Stepped -= OnStep;
    }

    void Update()
    {
        if (cc == null || !cc.enabled) { wasGrounded = true; return; }
        if (!cc.isGrounded)
        {
            fallSpeed = Mathf.Min(fallSpeed, cc.velocity.y);
            wasGrounded = false;
            return;
        }
        if (!wasGrounded)
        {
            wasGrounded = true;
            if (fallSpeed < -4f)
            {
                float v = Mathf.Clamp01((-fallSpeed - 3f) / 9f);
                src.pitch = Random.Range(0.9f, 1f);
                src.PlayOneShot(landClip, 0.25f + v * 0.6f);
                if (bob != null) bob.AddDip(0.4f + v * 0.6f);
            }
            fallSpeed = 0f;
        }
    }

    void OnStep()
    {
        if (cc == null || !cc.enabled || !cc.isGrounded) return;
        Vector3 hv = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
        float speed = hv.magnitude;
        if (speed < 0.4f) return;
        bool sprint = speed > 5.5f;
        var bank = IsWood() ? wood : concrete;
        src.pitch = Random.Range(0.92f, 1.08f);
        src.PlayOneShot(bank[Random.Range(0, bank.Length)], sprint ? sprintVolume : walkVolume);
    }

    bool IsWood()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.4f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f))
        {
            string n = hit.collider.name;
            for (int i = 0; i < WoodHints.Length; i++)
                if (n.Contains(WoodHints[i])) return true;
        }
        return false;
    }

    // ---------- синтез ----------

    const int SR = 22050;

    void MakeClips()
    {
        for (int i = 0; i < 3; i++)
        {
            concrete[i] = MakeConcrete(1000 + i * 77);
            wood[i] = MakeWood(2000 + i * 131);
        }
        landClip = MakeLand();
    }

    // бетон: глухой шумовой удар + низкий «бум»
    AudioClip MakeConcrete(int seed)
    {
        int n = Mathf.RoundToInt(0.13f * SR);
        float[] d = new float[n];
        System.Random r = new System.Random(seed);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float noise = (float)r.NextDouble() * 2f - 1f;
            lp += (noise - lp) * 0.22f;
            float thump = Mathf.Sin(2f * Mathf.PI * 82f * t * (1f - t * 2f))
                * Mathf.Exp(-t * 26f) * 0.55f;
            d[i] = (lp * Mathf.Exp(-t * 30f) * 0.8f + thump) * 0.55f;
        }
        var c = AudioClip.Create("step_c" + seed, n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // дерево: короткий яркий стук + гул доски
    AudioClip MakeWood(int seed)
    {
        int n = Mathf.RoundToInt(0.11f * SR);
        float[] d = new float[n];
        System.Random r = new System.Random(seed);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float noise = (float)r.NextDouble() * 2f - 1f;
            lp += (noise - lp) * 0.55f;
            float body = Mathf.Sin(2f * Mathf.PI * 175f * t) * Mathf.Exp(-t * 30f) * 0.35f
                       + Mathf.Sin(2f * Mathf.PI * 348f * t) * Mathf.Exp(-t * 38f) * 0.18f;
            d[i] = (lp * Mathf.Exp(-t * 42f) * 0.7f + body) * 0.5f;
        }
        var c = AudioClip.Create("step_w" + seed, n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // приземление: низкий толчок
    AudioClip MakeLand()
    {
        int n = Mathf.RoundToInt(0.2f * SR);
        float[] d = new float[n];
        System.Random r = new System.Random(777);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float f = 68f - t * 120f;
            float noise = (float)r.NextDouble() * 2f - 1f;
            lp += (noise - lp) * 0.18f;
            d[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 16f) * 0.8f
                + lp * Mathf.Exp(-t * 30f) * 0.35f) * 0.6f;
        }
        var c = AudioClip.Create("step_land", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }
}
