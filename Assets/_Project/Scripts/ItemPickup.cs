using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] ItemAsset item;
    [SerializeField] bool picked; // видно в инспекторе во время Play

    public void PickUp()
    {
        if (picked) return;
        picked = true;
        InventorySystem.Instance.AddItem(item);
        if (item == null) return;
        JournalSystem.TryUnlock("pickup_" + item.name);
        JournalSystem.Notify("Взято: " + item.displayName);
        SyndromeVoice.OnPickup(item.displayName);
        QuestLog.OnItemPicked(item);
        gameObject.SetActive(false); // предмет исчез из мира
    }
}