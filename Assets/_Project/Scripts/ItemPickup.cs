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
        gameObject.SetActive(false); // предмет исчез из мира
    }
}