using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 튜토리얼 스테이지 연출. 맵 프리팹 루트에 두고(StageSettings 옆), 맵에 TutorialTrigger를 놓아 대화 지점을 정한다.
//
// 시작 연출: 스테이지가 멈춘 채 마녀는 평소 자리에 서 있고, 왼쪽 화면 밖에서 쿠키 군단이 달려온다.
//   쿠키가 어느 정도 다가오면 스테이지가 움직이기 시작하고, 이후로는 쿠키가 느려서 화면 밖으로 떨어진다
//   (이 맵의 StageSettings 추격 속도를 마녀 시작 속도보다 낮게 둔다. 브레이크를 너무 오래 걸면 따라잡혀 죽는다).
// 대화: 마녀가 트리거를 지나면 달리던 시간이 살짝 멈추고(timeScale을 서서히 0으로) 대화창이 뜬다.
//   설명이 끝나면 다시 서서히 움직인다. 사망 후 재시작해도 이미 본 대화는 다시 뜨지 않는다.
public class TutorialDirector : MonoBehaviour
{
    [Header("시작 연출")]
    [SerializeField] float introDelay = 0.6f;       // 쿠키가 달려오기 전에 마녀만 서 있는 시간
    [SerializeField] float cookieStartGap = 4.5f;   // 쿠키 선두가 출발하는 마녀와의 거리 (화면 왼쪽 끝이 3)
    [SerializeField] float cookieSpeed = 2.8f;      // 달려오는 속도 (units/sec)
    [SerializeField] float cookieStopGap = 1.6f;    // 이 거리까지 다가오면 스테이지가 움직이기 시작한다
    [SerializeField] float introHold = 0.5f;        // 다가온 쿠키가 잠깐 선 채로 달리는 시간

    [Header("대화")]
    [SerializeField] float slowTime = 0.35f;        // 달리던 것이 멈추기까지
    [SerializeField] float resumeTime = 0.3f;       // 다시 움직이기까지
    [SerializeField] Font font;                     // 대화창 글꼴 (Assets/Fonts)

    readonly HashSet<TutorialTrigger> seen = new HashSet<TutorialTrigger>();   // 씬이 다시 로드될 때만 초기화된다
    TutorialTrigger[] triggers;
    DialogBox dialog;
    RunManager run;
    PlayerController player;
    bool busy;

    IEnumerator Start()
    {
        MenuUI.SetFont(font);
        dialog = new GameObject("DialogBox").AddComponent<DialogBox>();

        yield return null;   // RunManager.Start(추격자 초기 위치 지정)가 끝난 뒤에 시작한다
        run = RunManager.Instance;
        player = FindFirstObjectByType<PlayerController>();
        var speed = FindFirstObjectByType<SpeedController>();
        var chaser = FindFirstObjectByType<Chaser>();
        triggers = FindObjectsByType<TutorialTrigger>(FindObjectsSortMode.None);
        if (run == null || speed == null || chaser == null) yield break;

        // 시작 연출 (처음 한 번만, 사망 후 재시작은 연출 없이 바로 달린다)
        run.Holding = true;
        speed.Frozen = true;
        chaser.ResetTo(run.PlayerLocalX - cookieStartGap - 3f);   // 연출 전에는 화면 밖에서 기다린다
        yield return new WaitForSeconds(introDelay);
        chaser.BeginIntro(cookieStartGap, cookieSpeed, cookieStopGap);
        while (chaser.IntroRunning) yield return null;
        yield return new WaitForSeconds(introHold);
        speed.Frozen = false;
        run.Holding = false;
    }

    void Update()
    {
        if (busy || run == null || run.Current != RunManager.State.Running || run.Holding) return;

        float playerX = player.transform.position.x;
        TutorialTrigger next = null;
        foreach (var t in triggers)
        {
            if (t == null || seen.Contains(t) || t.transform.position.x > playerX) continue;
            if (next == null || t.transform.position.x > next.transform.position.x) next = t;   // 한꺼번에 지나면 앞선 것부터
        }
        if (next != null) StartCoroutine(Play(next));
    }

    IEnumerator Play(TutorialTrigger trigger)
    {
        busy = true;
        seen.Add(trigger);
        run.Holding = true;   // 입력 잠금 (대화를 넘기는 Space가 점프가 되지 않게)

        // 달리던 시간이 서서히 멈춘다
        for (float t = 0f; t < slowTime; t += Time.unscaledDeltaTime)
        {
            Time.timeScale = Mathf.SmoothStep(1f, 0f, t / slowTime);
            yield return null;
        }
        Time.timeScale = 0f;
        AudioListener.pause = true;   // 달리기·발소리 루프가 대화 중에 계속 들리지 않게

        yield return dialog.Run(trigger.message);

        AudioListener.pause = false;
        for (float t = 0f; t < resumeTime; t += Time.unscaledDeltaTime)
        {
            Time.timeScale = Mathf.SmoothStep(0f, 1f, t / resumeTime);
            if (t > 0f) run.Holding = false;   // 대화를 닫은 입력이 한 프레임 지난 뒤부터 조작할 수 있다
            yield return null;
        }
        Time.timeScale = 1f;
        run.Holding = false;
        busy = false;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
