using UnityEngine;

// Выход из воспоминания — светящийся портал в дальней комнате.
public class MemoryExit : MonoBehaviour
{
    public void Exit()
    {
        if (MemoryDive.Instance != null) MemoryDive.Instance.Surface();
    }
}
