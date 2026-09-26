using UnityEngine;

// Эхо памяти: ключевой предмет в воспоминании.
// Осмотр архивирует запись в дневник.
public class MemoryEcho : MonoBehaviour
{
    public string echoId = "memory_no_people";
    bool used; // видно в инспекторе во время Play

    public void Inspect()
    {
        if (used) return;
        used = true;
        JournalSystem.Unlock(echoId);
        var shake = FindObjectOfType<CameraShake>();
        if (shake != null) shake.Shake(0.45f);
        var inter = GetComponent<Interactable>();
        if (inter != null) inter.prompt = "...";
        Debug.Log("Эхо памяти: рисунок. Людей здесь нет.");
    }
}
