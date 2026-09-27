using UnityEngine;

// Синдром архивариуса как прогрессия: стадии 0-4.
// Растёт от погружений, сбрасывается калибровкой.
public static class Syndrome
{
    public static int Stage { get; private set; }
    public static bool DejaVuPending { get; private set; }
    public static bool WorriedPending { get; private set; }
    static bool bledOnce;

    // Вызывается при выходе из погружения.
    public static void OnDiveSurfaced()
    {
        Raise();
        if (Stage >= 2)
            SyndromeVoice.Nosebleed(!bledOnce);
        bledOnce = true;
    }

    public static void Raise()
    {
        if (Stage >= 4) return;
        Stage++;
        ApplyTremor();
        JournalSystem.Unlock("syndrome_" + Stage);
        Announcer.Say(StageTitle(Stage), 4f, new Color(1f, 0.5f, 0.5f));
        DejaVuPending = true;
        if (Stage >= 3) WorriedPending = true;
        Debug.Log("Синдром архивариуса: стадия " + Stage);
    }

    public static void Reset()
    {
        Stage = 0;
        DejaVuPending = false;
        WorriedPending = false;
        bledOnce = false;
        ApplyTremor();
    }

    public static bool ConsumeDejaVu()
    {
        if (!DejaVuPending) return false;
        DejaVuPending = false;
        return true;
    }

    public static bool ConsumeWorried()
    {
        if (!WorriedPending) return false;
        WorriedPending = false;
        return true;
    }

    static void ApplyTremor()
    {
        float[] t = { 0f, 0f, 0.22f, 0.32f, 0.45f };
        CameraShake.SyndromeTremor = t[Mathf.Clamp(Stage, 0, 4)];
    }

    static string StageTitle(int s)
    {
        switch (s)
        {
            case 1: return "Симптом: дежавю. Ты это уже видел.";
            case 2: return "Симптом: тремор. Руки дрожат. Из носа кровь.";
            case 3: return "Симптом: чужой голос. Не верь ему. Иногда.";
            default: return "СТАДИЯ КРИТИЧЕСКАЯ. Ты ещё ты?";
        }
    }
}
