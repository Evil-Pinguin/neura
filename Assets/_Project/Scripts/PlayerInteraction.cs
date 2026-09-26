using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] Transform cam;
    [SerializeField] float range = 3f;
    [SerializeField] float hitRadius = 0.15f;
    [SerializeField] TextMeshProUGUI promptLabel;
    [SerializeField] Image crosshair; // точка в центре

    [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.39f);       // обычная
    [SerializeField] Color activeColor = new Color(0.3f, 0.85f, 1f, 0.95f); // наведено

    Interactable current;

    void Update()
    {
        Debug.DrawRay(cam.position, cam.forward * range, Color.yellow);

        if (Physics.SphereCast(cam.position, hitRadius, cam.forward, out RaycastHit hit, range))
            current = hit.collider.GetComponentInParent<Interactable>();
        else
            current = null;

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