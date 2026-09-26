using UnityEngine;

public enum JournalCategory
{
    Report = 0,  // отчёт / документ
    Anomaly = 1, // аномалия
    Symptom = 2, // симптом / справка
}

[CreateAssetMenu(fileName = "NewJournalEntry", menuName = "NeuralCanvas/JournalEntry")]
public class JournalEntry : ScriptableObject
{
    [Tooltip("Уникальный id. Файл должен лежать в Resources/Journal/<id>.asset, открывается через JournalSystem.Unlock(id)")]
    public string entryId = "new_entry";
    public string title = "Новая запись";
    public JournalCategory category = JournalCategory.Report;
    public string dateStamp = "—";
    public string operatorId = "—";
    [TextArea(4, 10)] public string body;   // видно сразу
    [TextArea(4, 10)] public string detail; // второй слой, кнопка «Раскрыть анализ»; пусто = без кнопки
    [Range(0f, 1f)] public float corruption; // 0 = чисто, выше = глитч-текст с мерцанием
    public int sortOrder;                    // порядок в списке
}
