using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ToneOscillator : MonoBehaviour
{
    public float freqA = 4f;  // частота пациента
    public float freqB = 8f;  // наша частота
    public float detune = 0f; // 0 = чисто, >0 = звук "плывёт" (аномалия)
    [Range(0f, 1f)] public float volume = 0.2f;
    public bool audioOn = false;

    const float Pitch = 80f;
    const int SampleRate = 48000; // фиксируем частоту руками
    double t;

    void Awake()
    {
        var src = GetComponent<AudioSource>();
        // тихий клип нулевой длины — заставляет источник "играть",
        // а реальный звук мы генерируем в OnAudioFilterRead
        var clip = AudioClip.Create("silence", 1, 1, SampleRate, false);
        clip.SetData(new float[1], 0);
        src.clip = clip;
        src.loop = true;
        src.Play();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        // ВАЖНО: никаких обращений к AudioSettings здесь и в Awake —
        // только наша константа SampleRate
        double sr = SampleRate;
        float vol = audioOn ? volume : 0f;
        float det = 1f + Mathf.Sin((float)t * 30f) * detune;

        for (int i = 0; i < data.Length; i += channels)
        {
            float a = Mathf.Sin((float)(2.0 * System.Math.PI * freqA * Pitch * det * t));
            float b = Mathf.Sin((float)(2.0 * System.Math.PI * freqB * Pitch * det * t));
            float s = (a + b) * 0.5f * vol;
            for (int c = 0; c < channels; c++) data[i + c] = s;
            t += 1.0 / sr;
        }
    }
}