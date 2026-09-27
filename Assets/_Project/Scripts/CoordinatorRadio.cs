using UnityEngine;

// Селектор координатора: объявления по событиям сюжета + дежурное бормотание.
// Работает без настройки сцены (bootstrap).
public class CoordinatorRadio : MonoBehaviour
{
    public static CoordinatorRadio Instance { get; private set; }

    static readonly Color RadioColor = new Color(1f, 0.85f, 0.45f);

    static readonly string[] Pool = {
        "Мойте руки перед погружением.",
        "Помните: déjà vu — это нормально. Это нормально.",
        "Координатор видит вашу продуктивность.",
        "Перерыв на кофе — пятнадцать минут. Кофе закончился.",
        "Не разговаривайте с голосами. Они не ваши коллеги.",
        "Сотрудник М., вас ждут в архиве. Шутка. Работайте.",
        "Пятна на столах протирайте сами. Уборщица в отпуске. С 2019 года.",
    };

    bool talked, anom, dive, acc, refu;
    float poolTimer = 90f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<CoordinatorRadio>() != null) return;
        new GameObject("CoordinatorRadio (auto)").AddComponent<CoordinatorRadio>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        poolTimer = Random.Range(75f, 110f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (GameFlags.TalkedKim && !talked)
        {
            talked = true;
            Say("Оператор М. закреплён за направлением. Поздравляем. Работайте.");
        }
        if (GameFlags.AnomalySeen && !anom)
        {
            anom = true;
            Say("Внимание. Скачок частоты в секторе Уилла. Сохраняйте спокойствие. Это учение. Наверное.");
        }
        if (GameFlags.FirstDiveDone && !dive)
        {
            dive = true;
            Say("Оператор вернулся из погружения. Кровь из носа — вариант нормы. Расходитесь.");
        }
        if (GameFlags.CalibrationAccepted && !acc)
        {
            acc = true;
            Say("Благодарим сознательных сотрудников. Калибровочный кабинет работает до восьми.");
        }
        if (GameFlags.CalibrationRefused && !refu)
        {
            refu = true;
            Say("Напоминаем: отказ от калибровки фиксируется в личном деле. Хорошего дня.");
        }

        poolTimer -= Time.deltaTime;
        if (poolTimer <= 0f)
        {
            poolTimer = Random.Range(110f, 170f);
            Say(Pool[Random.Range(0, Pool.Length)]);
        }
    }

    static void Say(string s)
    {
        Announcer.Say("СЕЛЕКТОР: " + s, 5f, RadioColor);
    }
}
