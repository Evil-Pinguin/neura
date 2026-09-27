using UnityEngine;

// Покачивание камеры: шаговая качка + дыхание на месте.
// Работает в LateUpdate абсолютными значениями — не копит ошибку
// и не конфликтует с PlayerController (pitch) и CameraShake (тряска).
public class HeadBob : MonoBehaviour
{
    public event System.Action Stepped;

    [Header("Шаг")]
    [SerializeField] float walkStepLength = 2.2f;   // метров на шаг шагом
    [SerializeField] float sprintStepLength = 2.7f; // метров на шаг бегом

    [Header("Амплитуды")]
    [SerializeField] float bobHeight = 0.038f;  // вертикальная качка
    [SerializeField] float swayWidth = 0.028f;  // боковое шатание
    [SerializeField] float rollDegrees = 0.5f;  // крен камеры
    [SerializeField] float idleBreath = 0.008f; // дыхание на месте

    [Header("Приземление")]
    [SerializeField] float landDip = 0.09f; // глубина просадки
    [SerializeField] float dipReturn = 6f;  // скорость возврата

    CharacterController cc;
    PlayerController pc;
    CameraShake shake;
    Vector3 basePos;
    float phase; // радианы, π на шаг
    int lastStepIndex;
    float amp;   // 0 покой — 1 ходьба
    float dip;
    float dipVel;

    void Awake()
    {
        cc = GetComponentInParent<CharacterController>();
        pc = GetComponentInParent<PlayerController>();
        shake = GetComponent<CameraShake>();
        basePos = transform.localPosition;
    }

    public void AddDip(float strength)
    {
        dipVel -= landDip * Mathf.Clamp01(strength) * 12f;
    }

    void LateUpdate()
    {
        bool control = pc != null && pc.enabled && cc != null && cc.enabled;
        Vector3 hv = cc != null ? new Vector3(cc.velocity.x, 0f, cc.velocity.z) : Vector3.zero;
        float speed = hv.magnitude;
        bool moving = control && cc.isGrounded && speed > 0.4f;

        float stepLen = speed > 5.5f ? sprintStepLength : walkStepLength;
        if (moving)
        {
            phase += speed * Time.deltaTime / stepLen * Mathf.PI;
            int idx = Mathf.FloorToInt(phase / Mathf.PI);
            if (idx != lastStepIndex)
            {
                lastStepIndex = idx;
                if (Stepped != null) Stepped();
            }
            amp = Mathf.MoveTowards(amp, 1f, Time.deltaTime * 5f);
        }
        else
        {
            amp = Mathf.MoveTowards(amp, 0f, Time.deltaTime * 5f);
        }

        // пружина просадки от приземления
        dipVel += (-dip * 90f - dipVel * 10f) * Time.deltaTime;
        dip += dipVel * Time.deltaTime * dipReturn / 6f;

        float y = -Mathf.Abs(Mathf.Sin(phase)) * bobHeight * amp + dip;
        float x = Mathf.Sin(phase) * swayWidth * amp;
        float br = 1f - amp * 0.6f; // дыхание слабеет на ходу
        y += Mathf.Sin(Time.time * 1.5f) * idleBreath * br;
        x += Mathf.Sin(Time.time * 0.9f + 1.3f) * idleBreath * 0.7f * br;
        float roll = Mathf.Sin(phase) * rollDegrees * amp;

        // позиция — абсолютная: база + тряска + качка
        Vector3 so = shake != null ? shake.CurrentOffset : Vector3.zero;
        transform.localPosition = basePos + so + new Vector3(x, y, 0f);

        // крен — абсолютный: забираем pitch из текущей ротации, Z ставим свой
        Vector3 e = transform.localRotation.eulerAngles;
        transform.localRotation = Quaternion.Euler(e.x, e.y, roll);
    }
}
