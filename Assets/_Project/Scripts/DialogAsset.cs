using UnityEngine;

[System.Serializable]
public class DialogLine
{
    public string speaker;
    [TextArea(3, 6)] public string text;
    public DialogChoice[] choices;
    public bool endAfterLine; // разговор закончится после этой реплики
    public string emotion; // ключ эмоции Кима: smile/laugh/sad/serious/smirk/surprised/wink (пусто = без смены)
}

[System.Serializable]
public class DialogChoice
{
    public string label;
    public int nextIndex = -1;
    public ChoiceEffect effect = ChoiceEffect.None; // ← НОВОЕ
}

public enum ChoiceEffect
{
    None,
    AcceptCalibration, // согласие на калибровку
    RefuseCalibration  // отказ от калибровки
}

[CreateAssetMenu(fileName = "NewDialog", menuName = "NeuralCanvas/Dialog")]
public class DialogAsset : ScriptableObject
{
    public DialogLine[] lines;
}