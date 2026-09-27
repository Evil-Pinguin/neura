using UnityEngine;

// Цветок-тамагочи в холле: сохнет со временем, полив (3 раза) — цветение.
// Висит на PlantTop, подписывается на Interactable в Awake.
public class FlowerQuest : MonoBehaviour
{
    const float DecayPerSec = 100f / 480f; // полное высыхание ~8 минут
    const float WaterAmount = 45f;
    const int WateringsToBloom = 3;

    bool warned;
    Material mat;

    void Awake()
    {
        var rend = GetComponent<MeshRenderer>();
        if (rend != null) mat = rend.material; // инстанс — общий материал не трогаем
        var inter = GetComponent<Interactable>();
        if (inter != null) inter.onInteract.AddListener(Water);
        Tint();
    }

    void Update()
    {
        if (SideFlags.FlowerBloomed) { enabled = false; return; }
        SideFlags.FlowerWater = Mathf.Max(0f, SideFlags.FlowerWater - DecayPerSec * Time.deltaTime);
        if (SideFlags.FlowerWater <= 25f && !warned)
        {
            warned = true;
            Announcer.Say("Цветок в холле вянет. Полей его.", 4f, new Color(1f, 0.8f, 0.4f));
        }
        Tint();
    }

    public void Water()
    {
        if (SideFlags.FlowerBloomed)
        {
            JournalSystem.Notify("Цветок расцвёл. Красивый.");
            return;
        }
        SideFlags.FlowerWater = Mathf.Min(100f, SideFlags.FlowerWater + WaterAmount);
        if (SideFlags.FlowerWater > 25f) warned = false;
        SideFlags.FlowerWaterings++;
        if (SideFlags.FlowerWaterings >= WateringsToBloom)
        {
            SideFlags.FlowerBloomed = true;
            JournalSystem.Unlock("quest_flower");
            JournalSystem.Notify("Цветок расцвёл! Пахнет... нормально. Просто цветком.");
        }
        else
        {
            JournalSystem.Notify("Полито (" + SideFlags.FlowerWaterings + "/" + WateringsToBloom + ").");
        }
        Tint();
    }

    void Tint()
    {
        if (mat == null) return;
        if (SideFlags.FlowerBloomed) mat.color = new Color(1f, 0.55f, 0.75f);
        else if (SideFlags.FlowerWater > 50f) mat.color = new Color(0.3f, 0.8f, 0.3f);
        else if (SideFlags.FlowerWater > 25f) mat.color = new Color(0.75f, 0.75f, 0.3f);
        else mat.color = new Color(0.5f, 0.35f, 0.2f);
    }
}
