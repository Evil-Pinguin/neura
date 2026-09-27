using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogRunner : MonoBehaviour
{
    public static DialogRunner Instance { get; private set; }

    [Header("Ссылки")]
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI speakerLabel;
    [SerializeField] TextMeshProUGUI textLabel;
    [SerializeField] Button[] choiceButtons;
    [SerializeField] PlayerController playerController;
    [SerializeField] PlayerInteraction playerInteraction;
    [SerializeField] CameraShake shake;      // НОВОЕ
    [SerializeField] Image flashOverlay;     // НОВОЕ

    [Header("Настройки")]
    [SerializeField] float typeSpeed = 0.02f;
    [SerializeField] float choiceDelay = 0.25f;

    DialogAsset asset;
    int index;
    bool typing;
    bool waitingChoice;
    bool active;
    bool lineEnds;
    float openCooldown;
    Coroutine typeRoutine;

    public bool Active => active;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        HideChoices();
    }

    public void Open(DialogAsset dialog)
    {
        if (active) return;
        asset = dialog;
        index = 0;
        active = true;
        openCooldown = 0.2f;

        panel.SetActive(true);
        playerController.enabled = false;
        playerInteraction.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowLine();
    }

    public void Close()
    {
        active = false;
        panel.SetActive(false);
        HideChoices();
        DialogPortrait.Instance?.Hide();
        StandeeEmotion.Instance?.OnDialogClosed();
        playerController.enabled = true;
        playerInteraction.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!active) return;
        if (openCooldown > 0f) { openCooldown -= Time.deltaTime; return; }

        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (waitingChoice) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (typing) FinishTyping();
            else if (lineEnds) Close();   // НОВОЕ
            else Next();
        }
    }

    void ShowLine()
    {
        if (index < 0 || index >= asset.lines.Length) { Close(); return; }

        var line = asset.lines[index];
        lineEnds = line.endAfterLine;
        speakerLabel.text = line.speaker;
        textLabel.text = "";
        DialogPortrait.Instance?.ShowForLine(line.speaker, line.emotion);
        StandeeEmotion.Instance?.OnDialogLine(line.speaker, line.emotion);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeLine(line.text));
    }

    IEnumerator TypeLine(string text)
    {
        typing = true;
        foreach (char c in text)
        {
            textLabel.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        typing = false;
        OnLineShown();
    }

    void FinishTyping()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        textLabel.text = asset.lines[index].text;
        typing = false;
        OnLineShown();
    }

    void OnLineShown()
    {
        var line = asset.lines[index];
        if (line.choices != null && line.choices.Length > 0)
        {
            waitingChoice = true;
            StartCoroutine(ShowChoices(line));
        }
    }

    IEnumerator ShowChoices(DialogLine line)
    {
        yield return new WaitForSeconds(choiceDelay);
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < line.choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = line.choices[i].label;
            }
        }
    }

    void HideChoices()
    {
        waitingChoice = false;
        foreach (var b in choiceButtons) b.gameObject.SetActive(false);
    }

    void Next()
    {
        index++;
        if (index >= asset.lines.Length) Close();
        else ShowLine();
    }

    public void SelectChoice(int i)
    {
        if (!waitingChoice) return;
        var choice = asset.lines[index].choices[i];
        HideChoices();
        ApplyEffect(choice.effect);   // НОВОЕ
        index = choice.nextIndex;
        if (index < 0 || index >= asset.lines.Length) Close();
        else ShowLine();
    }

    // НОВОЕ: игровые последствия выбора
    void ApplyEffect(ChoiceEffect effect)
    {
        switch (effect)
        {
            case ChoiceEffect.AcceptCalibration:
                GameFlags.CalibrationAccepted = true;
                JournalSystem.Unlock("calibration_yes");
                StartCoroutine(CalibrationCalm());
                Debug.Log("ВЫБОР: калибровка принята. Симптомы подавлены на 90 секунд.");
                break;

            case ChoiceEffect.RefuseCalibration:
                GameFlags.CalibrationRefused = true;
                JournalSystem.Unlock("calibration_no");
                shake.Nausea(0.7f); // тело отвечает сразу
                Debug.Log("ВЫБОР: отказ. Файл Уилла остаётся открытым. Аномалия усиливается.");
                break;
        }
    }

    // комфорт: мягкая вспышка + 90 секунд покоя
    IEnumerator CalibrationCalm()
    {
        shake.SetSedation(90f);

        if (flashOverlay != null)
        {
            const float dur = 0.9f;
            float t = 0f;
            Color c = flashOverlay.color;
            while (t < dur)
            {
                t += Time.deltaTime;
                c.a = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI) * 0.3f;
                flashOverlay.color = c;
                yield return null;
            }
            c.a = 0f;
            flashOverlay.color = c;
        }
    }
}