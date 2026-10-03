using System;
using System.Collections.Generic;
using UnityEngine;

// 소리 종류. 새 소리가 필요하면 여기에 이름을 더하고 SoundManager 프리팹의 Sounds 목록에 클립을 꽂는다.
public enum Sound
{
    Jump,
    DoubleJump,
    Brake,
    Item,
    Collision,
    Fail,
    Goal,
    BroomFly,      // 반복 (비행 중)
    ChaserSteps,   // 반복 (추격자가 가까울수록 크게)
}

// 효과음과 배경음악을 재생한다. Assets/Resources/SoundManager 프리팹이 게임 시작 때 자동으로 생기고 씬이 바뀌어도 유지된다.
// 클립·볼륨·피치는 프리팹의 Sounds 목록에서 정한다 (코드 수정 없이 에셋만 꽂는다). 클립이 비어 있으면 소리를 내지 않는다.
// 배경음악은 스테이지(StageSettings)가 정하고, 같은 곡이면 이어서 재생하고 다른 곡이면 서서히 바뀐다.
public class SoundManager : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public Sound id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("재생할 때마다 이 범위에서 피치를 무작위로 정한다 (둘 다 1이면 그대로). 2단 점프처럼 같은 클립을 높게 쓸 때도 사용")]
        public Vector2 pitch = Vector2.one;
        [Tooltip("재생 요청 후 이 시간(초) 뒤에 시작한다")]
        [Min(0f)] public float delay;
        [Tooltip("앞부분만 이 시간(초)까지 재생한다. 0이면 끝까지 (긴 클립을 짧게 쓸 때)")]
        [Min(0f)] public float maxLength;
        [Tooltip("maxLength로 자를 때 끝에서 서서히 줄이는 시간(초)")]
        [Min(0f)] public float fadeOut = 0.15f;
    }

    class Voice
    {
        public AudioSource source;
        public float busyUntil;
        public float stopAt;      // 0이면 자르지 않는다
        public float fade;
        public float baseVolume;
    }

    class Loop
    {
        public AudioSource source;
        public float level;
        public float target;
    }

    class Bgm
    {
        public AudioSource source;
        public float level;
        public float target;
    }

    [Header("효과음")]
    [SerializeField] Entry[] sounds;
    [Tooltip("동시에 낼 수 있는 효과음 수")]
    [SerializeField, Min(1)] int voiceCount = 8;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;
    [Tooltip("반복 소리가 켜지고 꺼질 때 걸리는 시간(초)")]
    [SerializeField, Min(0.01f)] float loopFadeTime = 0.25f;

    [Header("배경음악")]
    [SerializeField, Range(0f, 1f)] float bgmVolume = 1f;
    [Tooltip("곡이 바뀔 때 서서히 줄고 커지는 시간(초)")]
    [SerializeField, Min(0.01f)] float bgmFadeTime = 1f;

    readonly List<Voice> voices = new();
    readonly Dictionary<Sound, Entry> table = new();
    readonly Dictionary<Sound, Loop> loops = new();
    readonly List<Bgm> bgmTracks = new();
    AudioClip bgmClip;
    float bgmStageVolume = 1f;

    static SoundManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null) return;
        var prefab = Resources.Load<SoundManager>("SoundManager");
        if (prefab == null)
        {
            Debug.LogWarning("Assets/Resources/SoundManager 프리팹이 없어 소리가 나지 않습니다.");
            return;
        }
        var obj = Instantiate(prefab);
        obj.name = "SoundManager";
        DontDestroyOnLoad(obj.gameObject);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        foreach (var e in sounds)
            if (e != null && e.clip != null) table[e.id] = e;

        for (int i = 0; i < voiceCount; i++)
            voices.Add(new Voice { source = NewSource() });
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    AudioSource NewSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        return s;
    }

    // ── 정적 진입점: 매니저가 없으면 조용히 무시한다 ──

    public static void Play(Sound id)
    {
        if (instance != null) instance.PlayOneShot(id);
    }

    // 반복 소리의 목표 크기(0~1). 0보다 크면 켜지고 0이면 서서히 꺼진다. 매 프레임 불러도 된다.
    public static void SetLoop(Sound id, float target)
    {
        if (instance != null) instance.SetLoopTarget(id, target);
    }

    // 스테이지 배경음악. 같은 곡이면 이어서 재생하고 볼륨만 바꾼다. null이면 서서히 끈다.
    public static void PlayBgm(AudioClip clip, float stageVolume)
    {
        if (instance != null) instance.StartBgm(clip, stageVolume);
    }

    // 지금 재생 중인 배경음악을 처음부터 다시 튼다 (재시작할 때)
    public static void RestartBgm()
    {
        if (instance == null) return;
        foreach (var t in instance.bgmTracks)
        {
            if (t.target <= 0f) continue;   // 사라지는 중인 이전 곡은 건드리지 않는다
            t.source.time = 0f;
            if (!t.source.isPlaying) t.source.Play();
        }
    }

    // ── 효과음 ──

    void PlayOneShot(Sound id)
    {
        if (!table.TryGetValue(id, out var e)) return;

        var v = TakeVoice();
        float pitch = UnityEngine.Random.Range(e.pitch.x, e.pitch.y);
        float length = e.clip.length / Mathf.Max(0.1f, pitch);
        if (e.maxLength > 0f) length = Mathf.Min(length, e.maxLength);

        var s = v.source;
        s.Stop();
        s.clip = e.clip;
        s.loop = false;
        s.pitch = pitch;
        v.baseVolume = e.volume * sfxVolume;
        s.volume = v.baseVolume;
        if (e.delay > 0f) s.PlayDelayed(e.delay);
        else s.Play();

        float now = Time.unscaledTime;
        v.busyUntil = now + e.delay + length + 0.05f;
        v.stopAt = e.maxLength > 0f ? now + e.delay + length : 0f;
        v.fade = Mathf.Min(e.fadeOut, length);
    }

    Voice TakeVoice()
    {
        float now = Time.unscaledTime;
        Voice oldest = voices[0];
        foreach (var v in voices)
        {
            if (v.busyUntil <= now) return v;
            if (v.busyUntil < oldest.busyUntil) oldest = v;
        }
        return oldest;   // 모두 사용 중이면 가장 먼저 끝날 소리를 끊는다
    }

    // ── 반복 소리 ──

    void SetLoopTarget(Sound id, float target)
    {
        if (!loops.TryGetValue(id, out var loop))
        {
            if (!table.TryGetValue(id, out var e)) return;
            if (target <= 0f) return;
            var s = NewSource();
            s.clip = e.clip;
            s.loop = true;
            loop = new Loop { source = s };
            loops[id] = loop;
        }
        loop.target = Mathf.Clamp01(target);
    }

    // ── 배경음악 ──

    void StartBgm(AudioClip clip, float stageVolume)
    {
        bgmStageVolume = stageVolume;
        if (clip == bgmClip) return;
        bgmClip = clip;

        foreach (var t in bgmTracks) t.target = 0f;
        if (clip == null) return;

        var s = NewSource();
        s.clip = clip;
        s.loop = true;
        s.volume = 0f;
        s.Play();
        bgmTracks.Add(new Bgm { source = s, level = 0f, target = 1f });
    }

    void Update()
    {
        float now = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        // 앞부분만 쓰는 효과음: 끝에서 서서히 줄이고 자른다
        foreach (var v in voices)
        {
            if (v.stopAt <= 0f) continue;
            float remaining = v.stopAt - now;
            if (remaining <= 0f)
            {
                v.source.Stop();
                v.stopAt = 0f;
            }
            else if (v.fade > 0f && remaining < v.fade)
            {
                v.source.volume = v.baseVolume * (remaining / v.fade);
            }
        }

        foreach (var kv in loops)
        {
            var loop = kv.Value;
            loop.level = Mathf.MoveTowards(loop.level, loop.target, dt / loopFadeTime);
            if (loop.level > 0f && !loop.source.isPlaying) loop.source.Play();
            else if (loop.level <= 0f && loop.source.isPlaying) loop.source.Stop();
            loop.source.volume = loop.level * table[kv.Key].volume * sfxVolume;
        }

        for (int i = bgmTracks.Count - 1; i >= 0; i--)
        {
            var t = bgmTracks[i];
            t.level = Mathf.MoveTowards(t.level, t.target, dt / bgmFadeTime);
            t.source.volume = t.level * bgmStageVolume * bgmVolume;
            if (t.target <= 0f && t.level <= 0f)
            {
                Destroy(t.source);
                bgmTracks.RemoveAt(i);
            }
        }
    }
}
