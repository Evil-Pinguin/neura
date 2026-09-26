using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 7f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpHeight = 1.2f;

    [Header("Мышь")]
    [SerializeField] Transform cam;
    [SerializeField] float sens = 2f;

    [Header("Стамина")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float drainRate = 22f;    // расход в сек при беге
    [SerializeField] float regenRate = 15f;    // восстановление в сек
    [SerializeField] float regenDelay = 1f;    // пауза после бега

    CharacterController cc;
    float verticalVelocity;
    float pitch;
    float stamina;
    float regenTimer;
    bool sprinting;

    public float Stamina01 => Mathf.Clamp01(stamina / maxStamina); // 0..1 для UI

    void Start()
    {
        cc = GetComponent<CharacterController>();
        stamina = maxStamina;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

        Look();
        Move();
        Stamina();
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * sens;
        float my = Input.GetAxis("Mouse Y") * sens;

        transform.Rotate(Vector3.up * mx);
        pitch = Mathf.Clamp(pitch - my, -80f, 80f);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        if (move.sqrMagnitude > 1f) move.Normalize();

        // прыжок: только с земли, только если есть силы
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space) && stamina > 5f)
        {
            // формула "хочу подпрыгнуть на jumpHeight метров"
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            stamina -= 5f;
        }

        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        // бег: держим Shift, есть стамина и куда-то идём
        sprinting = Input.GetKey(KeyCode.LeftShift) && stamina > 0f && move.sqrMagnitude > 0.01f;
        float speed = sprinting ? sprintSpeed : walkSpeed;

        cc.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    void Stamina()
    {
        if (sprinting)
        {
            stamina -= drainRate * Time.deltaTime;
            regenTimer = regenDelay; // пока бежим — таймер восстановления заморожен
        }
        else
        {
            regenTimer -= Time.deltaTime;
            if (regenTimer <= 0f)
                stamina += regenRate * Time.deltaTime;
        }
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }
}