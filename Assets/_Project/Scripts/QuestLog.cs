using System;
using UnityEngine;

// Состояние побочных квестов.
public static class SideFlags
{
    public static bool MugFound;
    public static bool MugReturned;
    public static bool CoffeeBroken;
    public static bool CoffeeFixed;
    public static int FlowerWaterings;
    public static bool FlowerBloomed;
    public static float FlowerWater = 70f;
}

// Флаги по имени — для условных диалогов (DialogStarter.flagDialog).
public static class FlagUtil
{
    public static bool Get(string name)
    {
        switch (name)
        {
            case "MugFound": return SideFlags.MugFound;
            case "MugReturned": return SideFlags.MugReturned;
            case "CoffeeFixed": return SideFlags.CoffeeFixed;
            case "FlowerBloomed": return SideFlags.FlowerBloomed;
            case "TalkedKim": return GameFlags.TalkedKim;
            case "AnomalySeen": return GameFlags.AnomalySeen;
            case "FirstDiveDone": return GameFlags.FirstDiveDone;
            default: return false;
        }
    }

    public static void Set(string name)
    {
        switch (name)
        {
            case "MugFound": SideFlags.MugFound = true; break;
            case "MugReturned": SideFlags.MugReturned = true; break;
            case "CoffeeFixed": SideFlags.CoffeeFixed = true; break;
            case "FlowerBloomed": SideFlags.FlowerBloomed = true; break;
        }
    }
}

// Квест-лог: задачи с галочками, условия — из флагов.
public static class QuestLog
{
    public struct Quest
    {
        public string title;
        public string hint;
        public Func<bool> done;
        public Func<string> extra;
    }

    public static readonly Quest[] All = {
        new Quest { title = "Поговорить с Ким", hint = "Ким стоит у доски в лаборатории. Подойди и нажми E.", done = () => GameFlags.TalkedKim },
        new Quest { title = "Сварить кофе", hint = "Кофемашина в лаборатории. Осторожно: руки дрожат.", done = () => GameFlags.CoffeeDone },
        new Quest { title = "Провести анализ частот", hint = "Частотная консоль. Стабилизируй пик пациента Уилла.", done = () => GameFlags.FreqDone },
        new Quest { title = "Закрепить кабинет", hint = "Выполни три задачи выше — кабинет твой.", done = () => GameFlags.OfficeUnlocked },
        new Quest { title = "Зафиксировать аномалию Уилла", hint = "Что-то не так с частотой 7.13. Копни глубже.", done = () => GameFlags.AnomalySeen },
        new Quest { title = "Погрузиться в память Уилла", hint = "Станция реставрации. Доступна после фиксации аномалии.", done = () => GameFlags.FirstDiveDone },
        new Quest { title = "Решить вопрос с калибровкой", hint = "Ким подала запрос. Согласиться или отказаться — решать тебе.", done = () => GameFlags.CalibrationAccepted || GameFlags.CalibrationRefused },
        new Quest { title = "(Побочка) Найти кружку Ким", hint = "Ким потеряла кружку. Она где-то в лаборатории.", done = () => SideFlags.MugReturned,
            extra = () => SideFlags.MugFound && !SideFlags.MugReturned ? "Найдена! Отнеси Ким." : "" },
        new Quest { title = "(Побочка) Починить кофемашину", hint = "Машина хрипит. Предохранитель ищи в холле, на стойке.", done = () => SideFlags.CoffeeFixed,
            extra = () => SideFlags.CoffeeBroken && !SideFlags.CoffeeFixed ? "СЛОМАНА." : "Пока работает." },
        new Quest { title = "(Побочка) Не дать цветку зачахнуть", hint = "Цветок в холле. Поливай его (3 раза — и он расцветёт).", done = () => SideFlags.FlowerBloomed,
            extra = () => SideFlags.FlowerBloomed ? "Расцвёл!" : "Влажность: " + Mathf.RoundToInt(SideFlags.FlowerWater) + "%" },
    };

    public static int DoneCount()
    {
        int n = 0;
        foreach (var q in All)
            if (q.done != null && q.done()) n++;
        return n;
    }

    // Хук из ItemPickup.
    public static void OnItemPicked(ItemAsset item)
    {
        if (item != null && item.name == "Item_Mug" && !SideFlags.MugFound)
        {
            SideFlags.MugFound = true;
            JournalSystem.Notify("Кружка Ким! Отнеси её владелице.");
        }
    }
}
