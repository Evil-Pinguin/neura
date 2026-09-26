using UnityEngine;

// Дверь с замком: отъезжает в сторону по E.
// Закрыта, пока нет ключа (requiredItem) и/или флага кабинета (requireOffice).
public class LockedDoor : MonoBehaviour
{
    [Header("Замок")]
    [SerializeField] ItemAsset requiredItem; // null = ключ не нужен
    [SerializeField] bool requireOffice;    // true = нужен флаг OfficeUnlocked
    [SerializeField] string needKeyText = "Заперто. Нужен синий ключ доступа.";
    [SerializeField] string needOfficeText = "Кабинет закрыт. Выполни задачи смены.";

    [Header("Движение")]
    [SerializeField] Vector3 openOffset = new Vector3(1.45f, 0f, 0f);
    [SerializeField] float slideTime = 0.8f;

    Vector3 closedPos;
    bool open;
    bool moving;
    Interactable inter;
    Collider solid;

    void Awake()
    {
        closedPos = transform.position;
        inter = GetComponent<Interactable>();
        solid = GetComponent<Collider>();
    }

    public void Toggle()
    {
        if (moving) return;
        if (requiredItem != null && (InventorySystem.Instance == null || !InventorySystem.Instance.Has(requiredItem)))
        {
            JournalSystem.Notify(needKeyText);
            Debug.Log("Дверь заперта: нет ключа.");
            return;
        }
        if (requireOffice && !GameFlags.OfficeUnlocked)
        {
            JournalSystem.Notify(needOfficeText);
            Debug.Log("Дверь заперта: кабинет не закреплён.");
            return;
        }
        StartCoroutine(Slide(!open));
    }

    System.Collections.IEnumerator Slide(bool toOpen)
    {
        moving = true;
        open = toOpen;
        if (open && solid != null) solid.enabled = false; // проход свободен сразу

        Vector3 from = transform.position;
        Vector3 to = closedPos + (open ? openOffset : Vector3.zero);
        float t = 0f;
        while (t < slideTime)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / slideTime)));
            yield return null;
        }
        transform.position = to;

        if (!open && solid != null) solid.enabled = true;
        if (inter != null) inter.prompt = open ? "Закрыть [E]" : "Открыть [E]";
        moving = false;
        Debug.Log(open ? "Дверь открыта." : "Дверь закрыта.");
    }
}

// Выдача кабинета: разговор с Ким + кофе + частоты.
public static class OfficeAccess
{
    public static void TryUnlock()
    {
        if (GameFlags.OfficeUnlocked) return;
        if (!(GameFlags.TalkedKim && GameFlags.CoffeeDone && GameFlags.FreqDone)) return;
        GameFlags.OfficeUnlocked = true;
        JournalSystem.Unlock("office_ready");
        Debug.Log("Ким закрепила кабинет за Майком.");
    }
}
