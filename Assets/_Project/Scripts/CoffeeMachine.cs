using System.Collections;
using UnityEngine;

public class CoffeeMachine : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] GameObject stain;   // объект CoffeeStain
    [SerializeField] CameraShake shake;  // PlayerCamera

    [Header("Состояние сцены (отладка)")]
    [SerializeField] bool firstBrewHappened; // видно в инспекторе во время Play

    int brews;

    void Awake()
    {
        SetPrompt("Сварить кофе [E]");
    }

    void SetPrompt(string s)
    {
        var inter = GetComponent<Interactable>();
        if (inter != null) inter.prompt = s;
    }

    public void Brew() // имя НЕ менять — на него уже подписан On Interact
    {
        // Сломана: чиним предохранителем из инвентаря.
        if (SideFlags.CoffeeBroken && !SideFlags.CoffeeFixed)
        {
            var fuse = FindFuse();
            if (fuse != null)
            {
                InventorySystem.Instance.RemoveItem(fuse);
                SideFlags.CoffeeFixed = true;
                JournalSystem.Unlock("quest_coffee");
                JournalSystem.Notify("Кофемашина починена. Пахнет победой.");
                SetPrompt("Сварить кофе [E]");
            }
            else
            {
                JournalSystem.Notify("Машина хрипит и молчит. Нужен предохранитель.");
                Announcer.Say("Кофемашина сломалась. Предохранитель ищи в холле.", 4f);
            }
            return;
        }

        brews++;
        if (!firstBrewHappened)
        {
            firstBrewHappened = true;
            GameFlags.CoffeeDone = true;
            OfficeAccess.TryUnlock();
            StartCoroutine(SpillSequence());
        }
        else
        {
            Debug.Log("Кофе готов.");
        }

        // Вторая варка убивает машину — начинается побочка.
        if (brews >= 2 && !SideFlags.CoffeeBroken && !SideFlags.CoffeeFixed)
        {
            SideFlags.CoffeeBroken = true;
            Announcer.Say("Кофемашина захрипела и умерла. Ей нужен предохранитель.", 4.5f);
            SetPrompt("Починить [E]");
        }
    }

    ItemAsset FindFuse()
    {
        if (InventorySystem.Instance == null) return null;
        foreach (var it in InventorySystem.Instance.Items)
            if (it != null && it.name == "Item_Fuse") return it;
        return null;
    }

    IEnumerator SpillSequence()
    {
        shake.Shake(0.3f);                       // рука дрогнула
        yield return new WaitForSeconds(0.35f);  // ...и кофе пошёл мимо кружки
        shake.Shake(0.15f);
        stain.SetActive(true);
        JournalSystem.Unlock("coffee_spill");
        Debug.Log("Рука дрогнула. Кофе пролился.");
    }

    public void EraseStain() // вызовется с пятна
    {
        stain.SetActive(false);
        GameFlags.StainErased = true;
        JournalSystem.Unlock("stain_erased");
        Debug.Log("Пятно исчезло. Как будто его и не было.");
    }
}
