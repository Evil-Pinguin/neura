using UnityEngine;

// Строит пространство памяти Уилла: фрагмент дома без людей.
// Планировка (локальные координаты от origin, смотрим на +Z):
//   прихожая (z 0..4) -> зелёный коридор (z 4..16) -> детская (z 16..22).
// В детской: рисунок на стене (MemoryEcho) и светящийся выход (MemoryExit).
// Локацию можно запечь в сцену: меню NeuralCanvas -> Bake Memory Space into Scene.
// ВАЖНО: подписки на Interactable живут в Awake самих компонентов,
// а не здесь, — иначе в запечённой сцене они потеряются.
public static class MemorySpace
{
    public static readonly Vector3 DefaultOrigin = new Vector3(500f, 0f, 500f);

    public static void Build(Vector3 o, out Vector3 spawnPos, out float spawnYaw)
    {
        var root = new GameObject("MemorySpace");
        root.transform.position = o;

        var floorMat = Mat(new Color(0.13f, 0.10f, 0.08f)); // тёмное дерево
        var wallMat = Mat(new Color(0.32f, 0.30f, 0.27f));  // тёплый серый
        var greenMat = Mat(new Color(0.16f, 0.32f, 0.22f)); // школьный зелёный
        var darkMat = Mat(new Color(0.05f, 0.05f, 0.06f));  // мебель-силуэты
        var paperMat = Mat(new Color(0.85f, 0.84f, 0.78f)); // рисунок
        var glowMat = GlowMat(new Color(0.30f, 0.85f, 1.00f)); // выход

        // земля-страховка под комплексом
        Box(root, "Ground", new Vector3(0f, -0.5f, 11f), new Vector3(40f, 0.5f, 44f), floorMat);

        // невидимые стены по периметру — из пустоты не выйти
        InvisibleWall(root, "BoundW", new Vector3(-10f, 3f, 11f), new Vector3(1f, 8f, 34f));
        InvisibleWall(root, "BoundE", new Vector3(10f, 3f, 11f), new Vector3(1f, 8f, 34f));
        InvisibleWall(root, "BoundS", new Vector3(0f, 3f, -5f), new Vector3(21f, 8f, 1f));
        InvisibleWall(root, "BoundN", new Vector3(0f, 3f, 27f), new Vector3(21f, 8f, 1f));

        // ---------- прихожая: x -3..3, z 0..4 ----------
        Box(root, "FloorA", new Vector3(0f, -0.25f, 2f), new Vector3(6.6f, 0.5f, 4.6f), floorMat);
        Box(root, "WallA_S", new Vector3(0f, 1.5f, -0.3f), new Vector3(6.6f, 3f, 0.3f), wallMat);
        Box(root, "WallA_W", new Vector3(-3.3f, 1.5f, 2f), new Vector3(0.3f, 3f, 4.9f), wallMat);
        Box(root, "WallA_E", new Vector3(3.3f, 1.5f, 2f), new Vector3(0.3f, 3f, 4.9f), wallMat);
        Box(root, "WallA_N_L", new Vector3(-2.15f, 1.5f, 4.3f), new Vector3(2.3f, 3f, 0.3f), wallMat);
        Box(root, "WallA_N_R", new Vector3(2.15f, 1.5f, 4.3f), new Vector3(2.3f, 3f, 0.3f), wallMat);

        // стол-силуэт
        Box(root, "TableA", new Vector3(1.8f, 0.4f, 1.2f), new Vector3(1.4f, 0.8f, 0.9f), darkMat);

        // ---------- коридор: x -1..1, z 4..16 ----------
        Box(root, "FloorCorr", new Vector3(0f, -0.25f, 10f), new Vector3(2.6f, 0.5f, 12.6f), floorMat);
        Box(root, "WallCorr_W", new Vector3(-1.3f, 1.5f, 10f), new Vector3(0.3f, 3f, 12.6f), greenMat);
        Box(root, "WallCorr_E", new Vector3(1.3f, 1.5f, 10f), new Vector3(0.3f, 3f, 12.6f), greenMat);

        // ---------- детская: x -3..3, z 16..22 ----------
        Box(root, "FloorB", new Vector3(0f, -0.25f, 19f), new Vector3(6.6f, 0.5f, 6.6f), floorMat);
        Box(root, "WallB_S_L", new Vector3(-2.15f, 1.5f, 15.7f), new Vector3(2.3f, 3f, 0.3f), wallMat);
        Box(root, "WallB_S_R", new Vector3(2.15f, 1.5f, 15.7f), new Vector3(2.3f, 3f, 0.3f), wallMat);
        Box(root, "WallB_W", new Vector3(-3.3f, 1.5f, 19f), new Vector3(0.3f, 3f, 7.0f), wallMat);
        Box(root, "WallB_E", new Vector3(3.3f, 1.5f, 19f), new Vector3(0.3f, 3f, 7.0f), wallMat);
        Box(root, "WallB_N", new Vector3(0f, 1.5f, 22.3f), new Vector3(6.9f, 3f, 0.3f), wallMat);

        // кровать и шкаф — силуэты
        Box(root, "BedB", new Vector3(-1.7f, 0.3f, 20.5f), new Vector3(1.8f, 0.6f, 2.6f), darkMat);
        Box(root, "WardrobeB", new Vector3(2.3f, 1.0f, 21.5f), new Vector3(1.2f, 2.0f, 0.8f), darkMat);

        // ---------- рисунок на северной стене ----------
        var echo = Box(root, "EchoDrawing",
            new Vector3(0.5f, 1.6f, 22.08f), new Vector3(0.45f, 0.55f, 0.04f), paperMat);
        echo.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        var echoInter = echo.AddComponent<Interactable>();
        echoInter.prompt = "Рассмотреть рисунок [E]";
        echo.AddComponent<MemoryEcho>().echoId = "memory_no_people";

        // ---------- выход в восточной стене ----------
        Box(root, "ExitFrame_L", new Vector3(3.05f, 1.5f, 18.2f), new Vector3(0.24f, 2.6f, 0.24f), darkMat);
        Box(root, "ExitFrame_R", new Vector3(3.05f, 1.5f, 19.8f), new Vector3(0.24f, 2.6f, 0.24f), darkMat);
        Box(root, "ExitFrame_T", new Vector3(3.05f, 2.7f, 19f), new Vector3(0.24f, 0.24f, 1.8f), darkMat);
        var exit = Box(root, "MemoryExit",
            new Vector3(3.1f, 1.5f, 19f), new Vector3(0.1f, 2.2f, 1.4f), glowMat);
        var exitInter = exit.AddComponent<Interactable>();
        exitInter.prompt = "Выйти из воспоминания [E]";
        exit.AddComponent<MemoryExit>();

        // ---------- свет ----------
        Lamp(root, "LampA", new Vector3(0f, 2.4f, 2f), new Color(0.60f, 0.75f, 0.90f), 0.6f, 7f);
        Lamp(root, "LampCorr", new Vector3(0f, 2.4f, 10f), new Color(0.50f, 0.80f, 0.60f), 0.7f, 9f);
        Lamp(root, "LampB", new Vector3(0f, 2.4f, 19f), new Color(1.0f, 0.80f, 0.60f), 0.8f, 10f);
        Lamp(root, "LampExit", new Vector3(2.4f, 2.0f, 19f), new Color(0.30f, 0.85f, 1.0f), 1.2f, 7f);

        // ---------- маркер спавна (едет вместе с корнем, если двигать руками) ----------
        var marker = new GameObject("MemorySpawn");
        marker.transform.SetParent(root.transform, false);
        marker.transform.localPosition = new Vector3(0f, 1.1f, 1.2f);
        spawnPos = marker.transform.position;
        spawnYaw = marker.transform.eulerAngles.y;
    }

    static GameObject Box(GameObject root, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().material = mat;
        return go;
    }

    static void InvisibleWall(GameObject root, string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().enabled = false; // коллайдер остаётся
    }

    static void Lamp(GameObject root, string name, Vector3 pos, Color c, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    static Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Glossiness", 0.1f);
        return m;
    }

    static Material GlowMat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = Color.black;
        m.SetColor("_EmissionColor", c);
        m.EnableKeyword("_EMISSION");
        return m;
    }
}
