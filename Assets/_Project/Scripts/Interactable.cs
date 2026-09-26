using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public string prompt = "Взять";   // текст подсказки при взгляде
    public UnityEvent onInteract;     // что происходит при нажатии E

    public void Interact()
    {
        onInteract?.Invoke(); // вызвать, если назначено
    }
}