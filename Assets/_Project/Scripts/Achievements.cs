using UnityEngine;

// Достижения за необязательное: все документы, все сны, болтовня с охранником.
public static class Achievements
{
    static readonly string[] DocIds = {
        "report_will_713", "memo_protocol", "archivist_syndrome", "will_anomaly",
        "note_coord_1", "note_coord_2", "note_kim",
    };
    static readonly string[] DreamIds = { "dream_house", "dream_depths", "dream_static" };

    static int guardTalks;
    static bool docsDone, dreamsDone, guardDone;

    // Вызывается из JournalSystem при каждой разблокировке.
    public static void OnJournal(string id)
    {
        if (!docsDone && IsAll(DocIds))
        {
            docsDone = true;
            Grant("achievement_docs", "Достижение: архивариус — все документы собраны");
        }
        if (!dreamsDone && IsAll(DreamIds))
        {
            dreamsDone = true;
            Grant("achievement_dreams", "Достижение: сновидец — все сны увидены");
        }
    }

    // Вызывается из DialogStarter (talkCounterId).
    public static void OnTalk(string counterId)
    {
        if (counterId != "guard" || guardDone) return;
        guardTalks++;
        if (guardTalks >= 5)
        {
            guardDone = true;
            Grant("achievement_guard", "Достижение: свой в холле — 5 разговоров с охранником");
        }
    }

    static bool IsAll(string[] ids)
    {
        foreach (var id in ids)
            if (!JournalSystem.Has(id)) return false;
        return true;
    }

    static void Grant(string entryId, string toast)
    {
        JournalSystem.Unlock(entryId);
        JournalSystem.Notify(toast);
        Debug.Log(toast);
    }
}
