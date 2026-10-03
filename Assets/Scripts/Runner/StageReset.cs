using System;

// 사망/클리어 후 스테이지를 다시 시작할 때 부서진 장애물, 먹은 아이템을 되돌리는 신호.
// RunManager가 보내고, 되돌릴 오브젝트가 구독한다.
public static class StageReset
{
    public static event Action Requested;

    public static void Raise() => Requested?.Invoke();
}
