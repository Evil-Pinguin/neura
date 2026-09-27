using UnityEngine;

// Поворачивает стенд лицом к камере (только вокруг Y — «билборд»).
public class Billboard : MonoBehaviour
{
    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>(); // страховка: вдруг нет тега MainCamera
        if (cam == null) return;

        Vector3 d = cam.transform.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(d);
    }
}
