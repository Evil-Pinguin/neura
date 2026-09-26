using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] Transform cam;
    [SerializeField] float range = 3.5f;
    [SerializeField] float hitRadius = 0.25f;
    [SerializeField] TextMeshProUGUI promptLabel;
    [SerializeField] Image crosshair; // точка в центре

    [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.39f);       // обычная
    [SerializeField] Color activeColor = new Color(0.3f, 0.85f, 1f, 0.95f); // наведено

    Camera playerCam;
    Interactable current;

    void Awake()
    {
        if (cam != null) playerCam = cam.GetComponent<Camera>();
        if (playerCam == null) playerCam = FindObjectOfType<Camera>();
    }

    void Update()
    {
        // луч строго через центр экрана — не зависит от сдвига камеры
        Ray ray = playerCam != null
            ? playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(cam.position, cam.forward);
        Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow);

        // ближайшее попадание выигрывает (стены блокируют), себя пропускаем
        float bestDist = float.MaxValue;
        Interactable best = null;
        foreach (var hit in Physics.SphereCastAll(ray, hitRadius, range))
        {
            if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (hit.distance >= bestDist) continue;
            bestDist = hit.distance;
            best = hit.collider.GetComponentInParent<Interactable>();
        }
        current = best;

        promptLabel.text = current != null ? current.prompt : "";

        // точка загорается, когда есть цель
        if (crosshair != null)
            crosshair.color = current != null ? activeColor : idleColor;

        if (current != null && Input.GetKeyDown(KeyCode.E))
            current.Interact();
    }

    void OnDisable()
    {
        if (promptLabel != null) promptLabel.text = "";
    }
}
