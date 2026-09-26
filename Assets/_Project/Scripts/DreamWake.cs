using UnityEngine;

// Пробуждение: кровать в деревянном доме.
public class DreamWake : MonoBehaviour
{
    public void WakeUp()
    {
        if (SleepSystem.Instance != null) SleepSystem.Instance.Wake();
    }
}
