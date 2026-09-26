using UnityEngine;

public enum ItemRealm { Reality, Memory } // мир предмета: реал или воспоминание

[CreateAssetMenu(fileName = "NewItem", menuName = "NeuralCanvas/Item")]
public class ItemAsset : ScriptableObject
{
    public string displayName;
    [TextArea(2, 5)] public string description; // видно сразу
    [TextArea(2, 5)] public string detail;      // скрыто до осмотра; пусто = без кнопки осмотра
    public string glyph = "?";                  // символ в слоте кармана
    public ItemRealm realm = ItemRealm.Reality;
}