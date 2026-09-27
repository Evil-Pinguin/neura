using UnityEngine;

public class DialogStarter : MonoBehaviour
{
    [SerializeField] DialogAsset firstDialog;
    [SerializeField] DialogAsset repeatDialog;
    [SerializeField] DialogAsset anomalyDialog; // приоритетный: аномалия видена, решения ещё нет
    [SerializeField] DialogAsset dejaVuDialog;   // синдром 1+: «мы это уже обсуждали»
    [SerializeField] DialogAsset worriedDialog;  // синдром 3+: Ким замечает симптомы
    [SerializeField] DialogAsset refusedDialog;  // после отказа от калибровки (флаер)
    [SerializeField] DialogAsset acceptedDialog; // после согласия (флаер)
    [SerializeField] string journalId;           // разблокировать запись при чтении (записки)
    [SerializeField] string talkCounterId;       // счётчик разговоров для достижений ("guard")

    [SerializeField] bool talkedOnce;
    [SerializeField] bool setsTalkedKim; // true только у Ким — идёт в зачёт кабинета

    public void StartDialog()
    {
        bool calibrationPending =
            GameFlags.AnomalySeen &&
            !GameFlags.CalibrationAccepted &&
            !GameFlags.CalibrationRefused;

        DialogAsset dialog;

        if (anomalyDialog != null && calibrationPending)
            dialog = anomalyDialog;
        else if (worriedDialog != null && talkedOnce && Syndrome.ConsumeWorried())
            dialog = worriedDialog;
        else if (dejaVuDialog != null && talkedOnce && Syndrome.ConsumeDejaVu())
            dialog = dejaVuDialog;
        else if (refusedDialog != null && GameFlags.CalibrationRefused)
            dialog = refusedDialog;
        else if (acceptedDialog != null && GameFlags.CalibrationAccepted)
            dialog = acceptedDialog;
        else if (talkedOnce && repeatDialog != null)
            dialog = repeatDialog;
        else
            dialog = firstDialog;

        talkedOnce = true;
        if (setsTalkedKim && !GameFlags.TalkedKim)
        {
            GameFlags.TalkedKim = true;
            OfficeAccess.TryUnlock();
        }
        if (!string.IsNullOrEmpty(journalId)) JournalSystem.Unlock(journalId);
        if (!string.IsNullOrEmpty(talkCounterId)) Achievements.OnTalk(talkCounterId);
        DialogRunner.Instance.Open(dialog);
    }
}
