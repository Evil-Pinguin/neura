using System.Collections;
using UnityEngine;

public class CoffeeMachine : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] GameObject stain;   // объект CoffeeStain
    [SerializeField] CameraShake shake;  // PlayerCamera

    [Header("Состояние сцены (отладка)")]
    [SerializeField] bool firstBrewHappened; // видно в инспекторе во время Play

    public void Brew() // имя НЕ менять — на него уже подписан On Interact
    {
        if (firstBrewHappened)
        {
            Debug.Log("Кофе готов.");
            return;
        }

        firstBrewHappened = true;
        GameFlags.CoffeeDone = true;
        OfficeAccess.TryUnlock();
        StartCoroutine(SpillSequence());
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