using System.Collections;
using UnityEngine;

// 마녀 이펙트: 슬라이드 먼지, 브레이크 불꽃, 피격(파티클·흰색 번쩍임·카메라 흔들림·짧은 정지), 장애물 파괴 파편.
// 마녀는 화면에 고정이고 바닥이 스크롤되므로, 파티클은 월드 공간에서 직접 Emit하고 스크롤 속도만큼 왼쪽 속도를 더해 바닥과 같이 흘러가게 한다.
// 파티클 모양(크기·수명·색·중력)은 Assets/Prefabs/Effects/의 FX_* 프리팹에서 조정하고, 여기서는 위치·속도·양만 정한다.
[RequireComponent(typeof(PlayerController))]
public class PlayerEffects : MonoBehaviour
{
    [Header("파티클 (Player 아래 FX_* 프리팹)")]
    [SerializeField] ParticleSystem slideDust;
    [SerializeField] ParticleSystem brakeSparks;
    [SerializeField] ParticleSystem hitBurst;
    [SerializeField] ParticleSystem debris;
    [SerializeField] ParticleSystem doubleJumpSwirl;

    [Header("슬라이드·브레이크 (초당 개수)")]
    [SerializeField, Min(0f)] float dustRate = 30f;
    [SerializeField, Min(0f)] float sparkRate = 45f;

    [Header("2단 점프 소용돌이 (발밑에서 도는 흰 입자)")]
    [SerializeField, Min(0.01f)] float swirlTime = 0.3f;
    [Tooltip("소용돌이가 도는 바퀴 수")]
    [SerializeField, Min(0.1f)] float swirlTurns = 1.5f;
    [SerializeField, Min(0.05f)] float swirlRadiusX = 0.5f;
    [Tooltip("세로 반지름. 작을수록 납작한 원판으로 보인다")]
    [SerializeField, Min(0.01f)] float swirlRadiusY = 0.15f;
    [SerializeField, Min(1f)] float swirlRate = 110f;

    [Header("피격")]
    [SerializeField] SpriteRenderer witchSprite;
    [Tooltip("피격 때 마녀를 한 색으로 칠하는 머티리얼 (Plum/SpriteFlash)")]
    [SerializeField] Material flashMaterial;
    [SerializeField, Min(0f)] float flashTime = 0.12f;
    [Tooltip("사망 순간 화면을 멈추는 시간 (초). 0이면 끔")]
    [SerializeField, Min(0f)] float hitStopTime = 0.07f;
    [SerializeField, Min(0f)] float shakeTime = 0.25f;
    [Tooltip("카메라 흔들림 세기 (units, 가로)")]
    [SerializeField, Min(0f)] float shakeAmount = 0.15f;
    [SerializeField, Min(0)] int hitBurstCount = 20;
    [SerializeField, Min(0)] int debrisCount = 10;

    PlayerController player;
    SpeedController speed;
    BoxCollider2D col;
    Camera cam;
    Material originalMaterial;
    float dustAcc;
    float sparkAcc;
    bool hitStopping;
    bool wasDoubleJumped;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        col = GetComponent<BoxCollider2D>();
        if (witchSprite != null) originalMaterial = witchSprite.sharedMaterial;
    }

    void Start()
    {
        speed = FindFirstObjectByType<SpeedController>();
        cam = Camera.main;
    }

    void OnEnable()
    {
        RunManager.Died += OnDied;
        Hazard.Broken += OnHazardBroken;
    }

    void OnDisable()
    {
        RunManager.Died -= OnDied;
        Hazard.Broken -= OnHazardBroken;
        if (hitStopping) Time.timeScale = 1f;
        hitStopping = false;
    }

    void Update()
    {
        var run = RunManager.Instance;
        bool running = run != null && run.Current == RunManager.State.Running;
        float scroll = speed != null ? speed.ScrollSpeed : 0f;

        var b = col.bounds;
        var feet = new Vector3(b.center.x - b.extents.x * 0.5f, b.min.y + 0.03f, 0f);

        // 2단 점프를 쓴 순간(체공 중 처음 true가 될 때) 소용돌이를 시작한다
        if (player.DoubleJumped && !wasDoubleJumped && doubleJumpSwirl != null) StartCoroutine(Swirl());
        wasDoubleJumped = player.DoubleJumped;

        bool slideNow = running && player.Grounded && player.Sliding;
        bool brakeNow = running && player.Grounded && speed != null && speed.Braking;

        // 먼지: 바닥과 함께 뒤로 흘러가며 살짝 떠오른다
        for (int n = Take(ref dustAcc, slideNow ? dustRate : 0f); n > 0; n--)
            Emit(slideDust, feet + Jitter(0.15f, 0.03f), new Vector2(-scroll * 0.85f + Random.Range(-0.6f, 0.2f), Random.Range(0.2f, 1.1f)));

        // 불꽃: 뒤쪽 위로 튀고 중력으로 떨어진다
        for (int n = Take(ref sparkAcc, brakeNow ? sparkRate : 0f); n > 0; n--)
            Emit(brakeSparks, feet + Jitter(0.1f, 0.03f), new Vector2(-scroll * 0.6f - Random.Range(0.5f, 2.5f), Random.Range(0.8f, 3f)));
    }

    // 이번 프레임에 내보낼 개수를 돌려준다 (소수 개수는 누적)
    static int Take(ref float acc, float rate)
    {
        if (rate <= 0f)
        {
            acc = 0f;
            return 0;
        }
        acc += rate * Time.deltaTime;
        int n = Mathf.FloorToInt(acc);
        acc -= n;
        return n;
    }

    static Vector3 Jitter(float x, float y) => new Vector3(Random.Range(-x, x), Random.Range(-y, y), 0f);

    static void Emit(ParticleSystem ps, Vector3 position, Vector2 velocity)
    {
        if (ps == null) return;
        var p = new ParticleSystem.EmitParams { position = position, velocity = velocity };
        ps.Emit(p, 1);
    }

    // 마녀 발밑 타원 위를 돌며 입자를 내보낸다. 마녀가 오르는 동안 계속 내보내서 나선형 궤적이 남는다.
    IEnumerator Swirl()
    {
        float elapsed = 0f;
        float acc = 0f;
        float angle = Mathf.PI;   // 왼쪽(뒤)에서 시작
        float angularSpeed = swirlTurns * Mathf.PI * 2f / swirlTime;

        while (elapsed < swirlTime)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            acc += swirlRate * dt;
            var b = col.bounds;
            var center = new Vector3(b.center.x, b.min.y + 0.1f, 0f);

            while (acc >= 1f)
            {
                acc -= 1f;
                // 한 프레임 안에서도 각도를 조금씩 나눠 끊기지 않게 한다
                float a = angle + Random.value * angularSpeed * dt;
                var offset = new Vector3(Mathf.Cos(a) * swirlRadiusX, Mathf.Sin(a) * swirlRadiusY, 0f);
                var tangent = new Vector2(-Mathf.Sin(a) * swirlRadiusX, Mathf.Cos(a) * swirlRadiusY).normalized;
                Emit(doubleJumpSwirl, center + offset, tangent * 0.8f);
            }
            angle += angularSpeed * dt;
            yield return null;
        }
    }

    void OnHazardBroken(Bounds bounds)
    {
        if (debris == null) return;
        float scroll = speed != null ? speed.ScrollSpeed : 0f;
        for (int i = 0; i < debrisCount; i++)
        {
            var pos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y), 0f);
            Emit(debris, pos, new Vector2(-scroll * 0.8f + Random.Range(-2f, 2f), Random.Range(0.5f, 4f)));
        }
    }

    void OnDied()
    {
        if (hitBurst != null)
        {
            var center = col.bounds.center;
            for (int i = 0; i < hitBurstCount; i++)
            {
                var dir = Random.insideUnitCircle.normalized;
                Emit(hitBurst, center + (Vector3)(dir * 0.1f), dir * Random.Range(2.5f, 6f));
            }
        }

        StartCoroutine(Flash());
        if (shakeAmount > 0f && cam != null) StartCoroutine(Shake());
        if (hitStopTime > 0f) StartCoroutine(HitStop());
    }

    IEnumerator Flash()
    {
        if (witchSprite == null || flashMaterial == null) yield break;
        witchSprite.sharedMaterial = flashMaterial;
        yield return new WaitForSecondsRealtime(flashTime);
        witchSprite.sharedMaterial = originalMaterial;
    }

    IEnumerator Shake()
    {
        var t = cam.transform;
        var basePos = t.localPosition;
        float elapsed = 0f;
        while (elapsed < shakeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float falloff = 1f - Mathf.Clamp01(elapsed / shakeTime);
            // 가로로만 흔든다 (세로로 흔들면 화면 아래 끝이 비어 보인다)
            t.localPosition = basePos + new Vector3(Random.Range(-1f, 1f) * shakeAmount * falloff, 0f, 0f);
            yield return null;
        }
        t.localPosition = basePos;
    }

    IEnumerator HitStop()
    {
        hitStopping = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopTime);
        Time.timeScale = 1f;
        hitStopping = false;
    }
}
