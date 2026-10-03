using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 엔딩 연출. 맵 프리팹 루트에 두고(StageSettings 옆), StageRig의 마녀·배경·스크롤을 그대로 쓰되 게임 규칙(조작, 사망, 추격자, HUD)은 끈다.
// 마지막 스테이지를 깨면 어두워지지 않고 바로 이 씬으로 와서 계속 달린다 (클리어 때의 스크롤 속도를 이어받는다).
//
//  1) 약 3초 그대로 달린다 (오븐 배경이 흐른다)
//  2) 오른쪽 화면 밖에서 쿠키들이 달려온다
//  3) 배경이 멈추고, 마녀는 화면 중앙까지 달려가 쿠키가 다가오자 멈춰 선다 (브레이크 자세)
//  4) 왼쪽 화면 밖에서도 쿠키들이 달려와, 마녀 양옆에 적당한 거리를 두고 둘러싼다
//  5) 마녀가 쿠키로 바뀐다 (Witch.controller의 Change 상태 = witch_change 애니메이션)
//  6) 잠깐 머문 뒤 화면이 어두워지며 시작 화면으로 돌아간다
public class EndingDirector : MonoBehaviour
{
    // RunManager가 엔딩으로 넘어가기 직전에 클리어한 스테이지의 스크롤 속도를 넣어 준다 (이 씬에서 읽고 0으로 되돌린다)
    public static float CarriedSpeed;

    [Header("달리기")]
    [SerializeField] float defaultRunSpeed = 6f;     // 엔딩 씬을 직접 열었을 때의 속도
    [SerializeField] float runTime = 3f;             // 처음에 그대로 달리는 시간
    [SerializeField] float cookieShowTime = 0.5f;    // 오른쪽 쿠키가 보이기 시작하고 배경이 멈추기 시작하기까지
    [SerializeField] float settleTime = 1.2f;        // 배경이 멈추고 마녀가 중앙에 서기까지
    [SerializeField] float centerX = 0f;             // 마녀가 서는 화면 가운데 (카메라 x)

    [Header("쿠키")]
    [SerializeField] int cookiesPerSide = 3;
    [SerializeField] float cookieSpeed = 7f;         // 달려오는 속도 (units/sec)
    [SerializeField] float spawnDistance = 7f;       // 화면 밖에서 출발하는 가운데로부터의 거리 (화면 반폭 5)
    [SerializeField] float surroundDistance = 2.3f;  // 마녀에서 가장 가까운 쿠키까지의 거리
    [SerializeField] float cookieSpacing = 0.8f;     // 같은 쪽 쿠키끼리의 간격
    [SerializeField] float cookieStagger = 0.25f;    // 쿠키가 출발하는 시간 차
    [SerializeField] RuntimeAnimatorController cookieController;
    [SerializeField] Sprite cookieSprite;

    [Header("마무리")]
    [SerializeField] float beforeChange = 0.9f;      // 둘러싼 뒤 변신 전까지 정적
    [SerializeField] float afterChange = 2.0f;       // 변신 뒤 머무는 시간
    [SerializeField] float fadeTime = 1.2f;

    static readonly int RunState = Animator.StringToHash("Run");
    static readonly int BrakeState = Animator.StringToHash("Brake");
    static readonly int ChangeState = Animator.StringToHash("Change");
    const float RunSpeedReference = 5f;   // 달리기 모션 배속 기준 (PlayerAnimator와 비슷하게)

    Transform witch;
    Animator witchAnim;
    SpriteRenderer witchRenderer;
    SpeedController speed;
    Image fade;
    bool loopRun;   // 마녀 달리기 모션 되감기 (Run 클립이 반복 설정이 아니라 직접 되감아야 한다 — PlayerAnimator와 같다)

    void Update()
    {
        if (!loopRun || witchAnim == null) return;
        var info = witchAnim.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash == RunState && info.normalizedTime >= 1f) witchAnim.Play(RunState, 0, 0f);
    }

    IEnumerator Start()
    {
        BuildFade();
        yield return null;   // 다른 컴포넌트의 Start가 끝난 뒤에

        var player = FindFirstObjectByType<PlayerController>();
        speed = FindFirstObjectByType<SpeedController>();
        if (player == null || speed == null) yield break;

        // 게임 규칙은 끈다: 조작·사망·추격자·HUD·일시정지·소리
        Disable<RunManager>();
        Disable<PlayerController>();
        Disable<WitchFlight>();
        Disable<PlayerAnimator>();
        Disable<PlayerSounds>();
        Disable<PlayerEffects>();
        Disable<PauseMenu>();
        Disable<ProgressBarHUD>();
        Disable<SpeedDebugHUD>();
        var chaser = FindFirstObjectByType<Chaser>();
        if (chaser != null) chaser.gameObject.SetActive(false);

        witch = player.transform;
        var rb = player.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;   // 위치를 직접 옮긴다
        witchAnim = player.GetComponentInChildren<Animator>();
        witchRenderer = player.GetComponentInChildren<SpriteRenderer>();

        // 1) 그대로 달린다
        float v = CarriedSpeed > 0f ? CarriedSpeed : defaultRunSpeed;
        CarriedSpeed = 0f;
        SetScroll(v);
        witchAnim.Play(RunState, 0, 0f);
        loopRun = true;
        SetRunAnim(v);
        yield return new WaitForSeconds(runTime);

        // 2) 오른쪽에서 쿠키가 달려온다
        var right = SpawnCookies(+1);
        yield return new WaitForSeconds(cookieShowTime);

        // 3) 배경이 멈추고 마녀는 중앙까지 달려가다 멈춘다
        float startX = witch.position.x;
        float walk = (centerX - startX) / settleTime;   // 마녀가 화면에서 앞으로 나아가는 속도
        for (float t = 0f; t < settleTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / settleTime);
            SetScroll(v * (1f - k));
            var p = witch.position;
            p.x = Mathf.Lerp(startX, centerX, k);
            witch.position = p;
            SetRunAnim(v * (1f - k) + walk * k);
            yield return null;
        }
        SetScroll(0f);
        speed.Frozen = true;
        witch.position = new Vector3(centerX, witch.position.y, witch.position.z);
        loopRun = false;
        witchAnim.speed = 1f;
        witchAnim.Play(BrakeState, 0, 0f);   // 쿠키가 다가오니 급정지
        yield return WaitArrived(right);

        // 4) 왼쪽에서도 쿠키가 달려와 둘러싼다
        var left = SpawnCookies(-1);
        yield return WaitArrived(left);
        yield return new WaitForSeconds(beforeChange);

        // 5) 마녀가 쿠키로 바뀐다
        witchAnim.speed = 1f;
        witchAnim.Play(ChangeState, 0, 0f);
        yield return null;
        float clipLength = witchAnim.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength + afterChange);

        // 6) 어두워지며 시작 화면으로
        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            fade.color = new Color(0f, 0f, 0f, t / fadeTime);
            yield return null;
        }
        fade.color = Color.black;
        StageLoader.Load(StageCatalog.TitleScene);
    }

    void Disable<T>() where T : MonoBehaviour
    {
        var c = FindFirstObjectByType<T>();
        if (c != null) c.enabled = false;
    }

    // 스테이지 스크롤(배경·바닥) 속도를 정한다 (SpeedController는 조작이 없으니 Restore로 직접 넣는다)
    void SetScroll(float value)
    {
        speed.Restore(value, value);
    }

    void SetRunAnim(float value)
    {
        witchAnim.speed = Mathf.Clamp(value / RunSpeedReference, 0.5f, 2f);
    }

    // side +1 = 오른쪽, -1 = 왼쪽. 마녀 쪽으로 달려와 가까운 쿠키부터 자리를 잡는다 (바깥쪽 쿠키는 더 뒤에서 시작)
    List<EndingCookie> SpawnCookies(int side)
    {
        var list = new List<EndingCookie>();
        for (int i = 0; i < cookiesPerSide; i++)
        {
            // 스크롤하는 Stage(이 디렉터의 부모)에 붙이지 않는다: 화면 좌표 그대로 달려와야 한다
            var go = new GameObject($"EndingCookie_{(side > 0 ? "R" : "L")}{i}");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = cookieSprite;
            sr.sortingLayerID = witchRenderer.sortingLayerID;
            sr.sortingOrder = witchRenderer.sortingOrder;
            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = cookieController;
            anim.Play(RunState, 0, (i * 0.27f) % 1f);   // 달리는 발이 똑같지 않게 (쿠키 컨트롤러의 상태 이름도 Run)

            float x = centerX + side * (spawnDistance + i * cookieStagger * cookieSpeed);
            go.transform.position = new Vector3(x, witch.position.y, -0.01f * i);
            go.transform.localScale = new Vector3(side > 0 ? -1f : 1f, 1f, 1f);   // 오른쪽 쿠키는 왼쪽(마녀 쪽)을 본다

            var cookie = go.AddComponent<EndingCookie>();
            cookie.Init(centerX + side * (surroundDistance + i * cookieSpacing), cookieSpeed);
            list.Add(cookie);
        }
        return list;
    }

    static IEnumerator WaitArrived(List<EndingCookie> cookies)
    {
        while (true)
        {
            bool all = true;
            foreach (var c in cookies) all &= c.Arrived;
            if (all) yield break;
            yield return null;
        }
    }

    void BuildFade()
    {
        var canvas = MenuUI.CreateCanvas(transform, 300);
        var rt = MenuUI.Box(canvas.transform, "Fade", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
        fade = rt.GetComponent<Image>();
        fade.raycastTarget = false;
    }
}
