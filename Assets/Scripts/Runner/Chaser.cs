using UnityEngine;

// 플레이어 뒤에서 일정한 속도로 쫓아오는 추격자. Stage의 자식으로 두면 맵과 함께 움직인다.
// 위치는 스테이지 로컬 X 좌표로 관리하고, 플레이어와의 거리가 catchDistance 이하가 되면 잡힌 것으로 본다.
public class Chaser : MonoBehaviour
{
    [SerializeField] float speed = 4.5f;            // 스테이지별로 다르게 설정 (units/sec)
    [SerializeField] float startGap = 8f;           // 시작/재시작 시 플레이어와의 거리
    [SerializeField] float catchDistance = 1.25f;   // 중심 간 거리가 이 이하이면 사망
    [SerializeField] float warningDistance = 5f;    // 이 거리 안으로 들어오면 Proximity가 0보다 커진다 (발소리용)

    float localX;

    public float StartGap => startGap;
    public float LocalX => localX;
    public float Gap => RunManager.Instance.PlayerLocalX - localX;

    // 0(멀다) ~ 1(거의 잡힘). 추후 발소리 볼륨에 사용.
    public float Proximity => Mathf.Clamp01(1f - (Gap - catchDistance) / warningDistance);

    public void ResetTo(float x)
    {
        localX = x;
        Place();
    }

    void FixedUpdate()
    {
        var run = RunManager.Instance;
        if (run == null || run.Current != RunManager.State.Running) return;

        localX += speed * Time.fixedDeltaTime;
        Place();
        if (Gap <= catchDistance) run.Die();
    }

    void Place()
    {
        var p = transform.localPosition;
        p.x = localX;
        transform.localPosition = p;
    }
}
