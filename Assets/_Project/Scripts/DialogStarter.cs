using UnityEngine;

public class DialogStarter : MonoBehaviour
{
    [SerializeField] DialogAsset firstDialog;
    [SerializeField] DialogAsset repeatDialog;
    [SerializeField] DialogAsset anomalyDialog; // приоритетный: аномалия видена, решения ещё нет

    [SerializeField] bool talkedOnce;

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
        DialogRunner.Instance.Open(dialog);
    }
}