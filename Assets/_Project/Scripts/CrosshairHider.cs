using UnityEngine;

public class CrosshairHider : MonoBehaviour
{
    void Update()
    {
        bool anyUI =
            (DialogRunner.Instance != null && DialogRunner.Instance.Active) ||
            (FrequencyConsole.Instance != null && FrequencyConsole.Instance.IsOpen) ||
            (InventorySystem.Instance != null && InventorySystem.Instance.IsOpenPublic) ||
            (JournalSystem.Instance != null && JournalSystem.Instance.IsOpen);

        gameObject.SetActive(!anyUI);
    }
}