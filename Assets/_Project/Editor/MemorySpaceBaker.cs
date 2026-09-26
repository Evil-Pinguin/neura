#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Запекает пространство памяти в сцену как обычные объекты:
// меню NeuralCanvas -> Bake Memory Space into Scene.
// После запекания сохрани сцену (Ctrl+S) — локацию можно двигать и править руками.
// Если MemorySpace уже есть в сцене, MemoryDive использует её и не строит заново.
public static class MemorySpaceBaker
{
    [MenuItem("NeuralCanvas/Bake Memory Space into Scene")]
    public static void Bake()
    {
        if (GameObject.Find("MemorySpace") != null)
        {
            Debug.LogWarning("[Bake] MemorySpace уже есть в сцене. Удали её перед повторной запечкой.");
            return;
        }
        MemorySpace.Build(MemorySpace.DefaultOrigin, out _, out _);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = GameObject.Find("MemorySpace");
        Debug.Log("[Bake] MemorySpace запечена в сцену. Сохрани сцену (Ctrl+S).");
    }

    [MenuItem("NeuralCanvas/Remove Baked Memory Space")]
    public static void RemoveBaked()
    {
        var root = GameObject.Find("MemorySpace");
        if (root == null)
        {
            Debug.LogWarning("[Bake] в сцене нет MemorySpace.");
            return;
        }
        Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Bake] MemorySpace удалена из сцены. Сохрани сцену (Ctrl+S).");
    }
}
#endif
