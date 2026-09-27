using UnityEngine;

// Записки появляются на столах по мере прогресса.
// Имена объектов в сцене фиксированы; работает без настройки сцены (bootstrap).
public class NoteVisibility : MonoBehaviour
{
    public static NoteVisibility Instance { get; private set; }

    struct Gate
    {
        public string goName;
        public System.Func<bool> cond;
        public bool shown;
    }

    Gate[] gates;
    GameObject[] objs;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<NoteVisibility>() != null) return;
        new GameObject("NoteVisibility (auto)").AddComponent<NoteVisibility>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gates = new Gate[]
        {
            new Gate { goName = "Note_Coord1", cond = () => GameFlags.TalkedKim },
            new Gate { goName = "Note_Kim", cond = () => GameFlags.FirstDiveDone },
            new Gate { goName = "Note_Coord2", cond = () => GameFlags.CalibrationRefused },
        };
        objs = new GameObject[gates.Length];
        for (int i = 0; i < gates.Length; i++)
        {
            objs[i] = GameObject.Find(gates[i].goName);
            if (objs[i] != null && !gates[i].cond())
                objs[i].SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        bool all = true;
        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i].shown) continue;
            all = false;
            if (!gates[i].cond()) continue;
            if (objs[i] != null) objs[i].SetActive(true);
            gates[i].shown = true;
            JournalSystem.Notify("На столе появилась записка.");
        }
        if (all) enabled = false;
    }
}
