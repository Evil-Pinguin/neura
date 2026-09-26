using UnityEngine;

// Точка сна на диване: зовёт SleepSystem в момент нажатия.
public class SleepPoint : MonoBehaviour
{
    public void Sleep()
    {
        if (SleepSystem.Instance != null) SleepSystem.Instance.Sleep();
    }
}
