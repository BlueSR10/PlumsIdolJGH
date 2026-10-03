using System;
using System.Collections.Generic;
using UnityEngine;

// 소리 종류. 새 소리가 필요하면 맨 아래에 이름을 더하고 SoundManager 프리팹의 Sounds 목록에 클립을 꽂는다 (순서를 바꾸면 저장된 목록이 어긋난다).
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
    Slide,         // 누르는 동안 이어지는 소리 (앞부분 한 번 → 중간 반복 → 뒷부분 한 번)
    Wind,          // 투사체(날아오는 장애물) 공통
    WitchRun,      // 반복 (지상에서 달리는 동안)
}

// 효과음과 배경음악을 재생한다. Assets/Resources/SoundManager 프리팹이 게임 시작 때 자동으로 생기고 씬이 바뀌어도 유지된다.
// 클립·볼륨·피치·지연·자르기는 프리팹의 Sounds 목록에서 정한다 (코드 수정 없이 에셋만 꽂는다). 클립이 비어 있으면 소리를 내지 않는다.
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

        [Header("누르는 동안 이어지는 소리 (Slide 등, 둘 다 0이면 쓰지 않음)")]
        [Tooltip("여기까지(초)는 앞부분으로 한 번만 재생한다")]
        [Min(0f)] public float loopStart;
        [Tooltip("loopStart ~ 여기까지(초)를 반복한다. 떼면 여기부터 끝까지(뒷부분)를 한 번 재생한다")]
        [Min(0f)] public float loopEnd;
        [Tooltip("반복 구간의 이음새를 부드럽게 겹치는 시간(초). loopStart 앞에 이만큼 여유가 있어야 한다")]
        [Min(0f)] public float crossfade = 0.1f;
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
        public float pitch = 1f;
    }

    class Bgm
    {
        public AudioSource source;
        public float level;
        public float target;
    }

    // 소리 크기를 목표까지 일정한 속도로 바꾸는 소스
    class Fader
    {
        public AudioSource source;
        public float level;
        public float target;
        public float rate = 10f;
    }

    // 앞부분 → 중간 반복 → 뒷부분으로 나눈 소리 (SetHold)
    class Sustain
    {
        public Entry entry;
        public AudioClip intro, body, outro;
        public Fader introFade, bodyFade, outroFade;
        public bool built, simple;   // simple: 구간 지정이 없거나 만들 수 없을 때 클립 전체를 그냥 반복
        public bool held;
        public double bodyStartDsp;
    }

    [Header("효과음")]
    [SerializeField] Entry[] sounds;
    [Tooltip("동시에 낼 수 있는 효과음 수")]
    [SerializeField, Min(1)] int voiceCount = 8;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;
    [Tooltip("반복 소리가 켜지고 꺼질 때 걸리는 시간(초)")]
    [SerializeField, Min(0.01f)] float loopFadeTime = 0.25f;

    [Header("투사체 (Wind) - 날아오는 장애물 공통")]
    [Tooltip("도플러 효과에 쓰는 소리의 속도 (units/sec). 작을수록 다가올 때와 멀어질 때의 음 높이 차이가 커진다. 실제 소리 속도(343)는 너무 약해서 게임용으로 낮춘다")]
    [SerializeField, Min(1f)] float dopplerSpeed = 30f;
    [Tooltip("이 거리 안에서는 소리가 줄지 않는다 (카메라가 화면에서 약 10 떨어져 있다)")]
    [SerializeField, Min(0.1f)] float projectileMinDistance = 10f;
    [Tooltip("이 거리를 넘으면 들리지 않는다. 멀리서 오는 새는 작게, 지나갈 때 가장 크게 들린다")]
    [SerializeField, Min(0.1f)] float projectileMaxDistance = 24f;

    [Header("배경음악")]
    [SerializeField, Range(0f, 1f)] float bgmVolume = 1f;
    [Tooltip("곡이 바뀔 때 서서히 줄고 커지는 시간(초)")]
    [SerializeField, Min(0.01f)] float bgmFadeTime = 1f;
    [Tooltip("Game Over에서 배경음악을 끌 때 걸리는 시간(초). 딱 끊기면 소리가 튀어서 아주 짧게 줄인다")]
    [SerializeField, Min(0.01f)] float bgmPauseFadeTime = 0.1f;

    readonly List<Voice> voices = new();
    readonly Dictionary<Sound, Entry> table = new();
    readonly Dictionary<Sound, Loop> loops = new();
    readonly Dictionary<Sound, Sustain> sustains = new();
    readonly List<Bgm> bgmTracks = new();
    AudioClip bgmClip;
    float bgmStageVolume = 1f;
    bool bgmPaused;            // Game Over 동안 배경음악을 끈다
    float bgmPauseLevel = 1f;  // 1 = 들림, 0 = 꺼짐

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

    // 반복 소리의 목표 크기(0~1)와 피치. 0보다 크면 켜지고 0이면 서서히 꺼진다. 매 프레임 불러도 된다.
    public static void SetLoop(Sound id, float target, float pitch = 1f)
    {
        if (instance != null) instance.SetLoopTarget(id, target, pitch);
    }

    // 누르는 동안 이어지는 소리. held가 true가 되는 순간 앞부분을 재생하고 이어서 중간을 반복하며,
    // false가 되면 뒷부분을 재생하고 끝낸다. 매 프레임 불러도 된다 (값이 바뀔 때만 동작).
    public static void SetHold(Sound id, bool held)
    {
        if (instance != null) instance.SetHoldState(id, held, true);
    }

    // 누르는 동안 이어지는 소리를 뒷부분 없이 바로 끈다 (사망 등).
    public static void StopHold(Sound id)
    {
        if (instance != null) instance.SetHoldState(id, false, false);
    }

    // 스테이지 배경음악. 같은 곡이면 이어서 재생하고 볼륨만 바꾼다. null이면 서서히 끈다.
    public static void PlayBgm(AudioClip clip, float stageVolume)
    {
        if (instance != null) instance.StartBgm(clip, stageVolume);
    }

    // 투사체(ProjectileSound)가 쓸 Wind 소스를 설정한다. 소리가 없으면 false.
    public static bool TryConfigureProjectile(AudioSource src, out float baseVolume, out float speedOfSound)
    {
        baseVolume = 0f;
        speedOfSound = 30f;
        if (instance == null || !instance.table.TryGetValue(Sound.Wind, out var e)) return false;

        src.clip = e.clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 1f;                       // 위치에 따라 좌우로 들리고 가까울수록 크게
        src.dopplerLevel = 0f;                       // 도플러는 ProjectileSound가 직접 계산한다
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = instance.projectileMinDistance;
        src.maxDistance = instance.projectileMaxDistance;
        src.spread = 0f;
        baseVolume = e.volume * instance.sfxVolume;
        src.volume = 0f;
        speedOfSound = instance.dopplerSpeed;
        return true;
    }

    // 배경음악을 끈다 (Game Over). RestartBgm이나 새 곡 재생으로 다시 켜진다.
    public static void PauseBgm()
    {
        if (instance != null) instance.bgmPaused = true;
    }

    // 지금 재생 중인 배경음악을 처음부터 다시 튼다 (재시작할 때). 꺼져 있었다면 다시 켠다.
    public static void RestartBgm()
    {
        if (instance == null) return;
        instance.bgmPaused = false;
        instance.bgmPauseLevel = 1f;
        foreach (var t in instance.bgmTracks)
        {
            if (t.target <= 0f) continue;   // 사라지는 중인 이전 곡은 건드리지 않는다
            t.source.time = 0f;
            t.source.Play();
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

    void SetLoopTarget(Sound id, float target, float pitch)
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
        loop.pitch = pitch;
    }

    // ── 누르는 동안 이어지는 소리 (앞부분 → 중간 반복 → 뒷부분) ──

    void SetHoldState(Sound id, bool held, bool withOutro)
    {
        if (!sustains.TryGetValue(id, out var s))
        {
            if (!held || !table.TryGetValue(id, out var e)) return;
            s = new Sustain { entry = e };
            sustains[id] = s;
        }
        if (held == s.held) return;

        if (held) PressSustain(s);
        else ReleaseSustain(s, withOutro);
    }

    // 클립을 앞부분/반복 구간/뒷부분 세 개로 나눈다. 반복 구간은 끝을 앞과 겹쳐서 이음새가 안 들리게 만든다.
    void BuildSustain(Sustain s)
    {
        s.built = true;
        var e = s.entry;
        var clip = e.clip;
        int ch = clip.channels, freq = clip.frequency, total = clip.samples;

        s.simple = !(e.loopStart > 0f && e.loopEnd > e.loopStart);
        float[] data = null;
        if (!s.simple)
        {
            data = new float[total * ch];
            if (!clip.GetData(data, 0))
            {
                Debug.LogWarning($"'{clip.name}' 데이터를 읽을 수 없어 구간 반복 대신 전체를 반복합니다. 임포트의 Load Type을 Decompress On Load로 두세요.");
                s.simple = true;
            }
        }

        if (s.simple)
        {
            s.body = clip;
        }
        else
        {
            int a = Mathf.Clamp(Mathf.RoundToInt(e.loopStart * freq), 1, total - 2);
            int b = Mathf.Clamp(Mathf.RoundToInt(e.loopEnd * freq), a + 2, total);
            int xf = Mathf.Min(Mathf.RoundToInt(e.crossfade * freq), a, (b - a) / 2);
            int len = b - a;

            s.intro = Slice(clip.name + "_intro", data, 0, a, ch, freq);
            if (total - b > 0) s.outro = Slice(clip.name + "_outro", data, b, total - b, ch, freq);

            // 반복 구간: [a, b)를 복사하고, 끝 xf 샘플은 '구간 끝 → 구간 시작 직전'으로 서서히 넘어가게 섞는다
            var loop = new float[len * ch];
            Array.Copy(data, a * ch, loop, 0, len * ch);
            for (int k = 0; k < xf; k++)
            {
                float w = (k + 1f) / xf;
                for (int c = 0; c < ch; c++)
                {
                    float tail = data[(b - xf + k) * ch + c];
                    float head = data[(a - xf + k) * ch + c];
                    loop[(len - xf + k) * ch + c] = tail * (1f - w) + head * w;
                }
            }
            s.body = AudioClip.Create(clip.name + "_loop", len, ch, freq, false);
            s.body.SetData(loop, 0);
        }

        s.introFade = new Fader { source = NewSource() };
        s.bodyFade = new Fader { source = NewSource() };
        s.outroFade = new Fader { source = NewSource() };
        s.bodyFade.source.loop = true;
    }

    static AudioClip Slice(string name, float[] data, int start, int count, int ch, int freq)
    {
        var part = new float[count * ch];
        Array.Copy(data, start * ch, part, 0, count * ch);
        var c = AudioClip.Create(name, count, ch, freq, false);
        c.SetData(part, 0);
        return c;
    }

    void PressSustain(Sustain s)
    {
        if (!s.built) BuildSustain(s);
        s.held = true;

        s.introFade.source.Stop();
        s.bodyFade.source.Stop();
        s.outroFade.source.Stop();
        Reset(s.introFade, 1f);
        Reset(s.bodyFade, 1f);
        Reset(s.outroFade, 0f);

        double now = AudioSettings.dspTime;
        if (s.intro != null)
        {
            s.introFade.source.clip = s.intro;
            s.introFade.source.Play();
            s.bodyStartDsp = now + (double)s.intro.samples / s.intro.frequency;   // 앞부분이 끝나는 순간에 이어서 시작
            s.bodyFade.source.clip = s.body;
            s.bodyFade.source.PlayScheduled(s.bodyStartDsp);
        }
        else
        {
            s.bodyStartDsp = now;
            s.bodyFade.source.clip = s.body;
            s.bodyFade.source.Play();
        }
        ApplyFaders(s, 0f);
    }

    void ReleaseSustain(Sustain s, bool withOutro)
    {
        s.held = false;
        bool bodyStarted = AudioSettings.dspTime >= s.bodyStartDsp;

        if (!bodyStarted)
        {
            // 앞부분 도중에 뗐다: 반복이 시작되기 전이므로 예약을 취소하고 앞부분을 짧게 줄여 끈다
            s.bodyFade.source.Stop();
            Fade(s.introFade, 0f, 0.12f);
            return;
        }

        Fade(s.bodyFade, 0f, 0.08f);
        Fade(s.introFade, 0f, 0.05f);
        if (withOutro && s.outro != null)
        {
            s.outroFade.source.clip = s.outro;
            Reset(s.outroFade, 0f);
            s.outroFade.source.Play();
            Fade(s.outroFade, 1f, 0.08f);   // 반복이 줄어드는 사이 뒷부분이 커지며 이어진다
        }
    }

    static void Reset(Fader f, float level)
    {
        f.level = level;
        f.target = level;
    }

    static void Fade(Fader f, float target, float time)
    {
        f.target = target;
        f.rate = 1f / Mathf.Max(0.01f, time);
    }

    void ApplyFaders(Sustain s, float dt)
    {
        float baseVol = s.entry.volume * sfxVolume;
        foreach (var f in new[] { s.introFade, s.bodyFade, s.outroFade })
        {
            if (f == null) continue;
            f.level = Mathf.MoveTowards(f.level, f.target, f.rate * dt);
            f.source.volume = f.level * baseVol;
            if (f.level <= 0f && f.target <= 0f && f.source.isPlaying && !s.held) f.source.Stop();
        }
    }

    // ── 배경음악 ──

    void StartBgm(AudioClip clip, float stageVolume)
    {
        bgmStageVolume = stageVolume;
        if (clip == bgmClip && !bgmPaused) return;
        bgmPaused = false;
        bgmPauseLevel = 1f;
        if (clip == bgmClip)
        {
            foreach (var t in bgmTracks)
                if (t.target > 0f) t.source.UnPause();
            return;
        }
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
            loop.source.pitch = loop.pitch;
        }

        foreach (var s in sustains.Values)
            if (s.built) ApplyFaders(s, dt);

        bgmPauseLevel = Mathf.MoveTowards(bgmPauseLevel, bgmPaused ? 0f : 1f, dt / bgmPauseFadeTime);

        for (int i = bgmTracks.Count - 1; i >= 0; i--)
        {
            var t = bgmTracks[i];
            t.level = Mathf.MoveTowards(t.level, t.target, dt / bgmFadeTime);
            t.source.volume = t.level * bgmStageVolume * bgmVolume * bgmPauseLevel;
            if (bgmPaused && bgmPauseLevel <= 0f && t.source.isPlaying) t.source.Pause();
            if (t.target <= 0f && t.level <= 0f)
            {
                Destroy(t.source);
                bgmTracks.RemoveAt(i);
            }
        }
    }
}
