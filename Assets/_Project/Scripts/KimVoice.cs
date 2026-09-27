using System.Collections.Generic;
using UnityEngine;

// Озвучка Ким: клипы лежат в Resources/KimVoice/<Dialog>_<index>,
// проигрываются на её репликах. Цепляется из DialogRunner.
public class KimVoice : MonoBehaviour
{
    public static KimVoice Instance { get; private set; }

    const string ResPath = "KimVoice/";
    const string KimSpeaker = "Ким";

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    AudioSource src;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<KimVoice>() != null) return;
        new GameObject("KimVoice (auto)").AddComponent<KimVoice>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        foreach (var c in Resources.LoadAll<AudioClip>(ResPath))
            clips[c.name] = c;
        Debug.Log("[KimVoice] клипов: " + clips.Count);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void PlayFor(string dialogName, int index, string speaker)
    {
        if (Instance == null || speaker != KimSpeaker) return;
        AudioClip c;
        if (Instance.clips.TryGetValue(dialogName + "_" + index, out c) && c != null)
            Instance.src.PlayOneShot(c);
    }

    public static void Stop()
    {
        if (Instance != null && Instance.src != null) Instance.src.Stop();
    }
}
