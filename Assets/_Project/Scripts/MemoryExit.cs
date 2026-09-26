using UnityEngine;

// Выход из воспоминания — светящийся портал в дальней комнате.
// Сам подписывается на Interactable в Awake — работает и в запечённой сцене,
// где рантайм-подписки не сохраняются.
public class MemoryExit : MonoBehaviour
{
    void Awake()
    {
        var inter = GetComponent<Interactable>();
        if (inter != null) inter.onInteract.AddListener(Exit);
    }

    public void Exit()
    {
        if (MemoryDive.Instance != null) MemoryDive.Instance.Surface();
    }
}
