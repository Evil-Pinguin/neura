using UnityEngine;

public class DialogStarter : MonoBehaviour
{
    [SerializeField] DialogAsset firstDialog;
    [SerializeField] DialogAsset repeatDialog;
    [SerializeField] DialogAsset anomalyDialog; // приоритетный: аномалия видена, решения ещё нет

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
        DialogRunner.Instance.Open(dialog);
    }
}
