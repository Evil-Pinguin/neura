using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public string prompt = "Взять";   // текст подсказки при взгляде
    public UnityEvent onInteract = new UnityEvent(); // new нужен рантайм-объектам (AddComponent)

    public void Interact()
    {
        onInteract?.Invoke(); // вызвать, если назначено
    }
}