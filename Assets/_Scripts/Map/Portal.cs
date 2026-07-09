using System;
using UnityEngine;

/// <summary>
/// 플레이어가 닿으면 지정된 이벤트(지도로 돌아가기 등)를 실행하는 포탈 부품
/// </summary>
public class Portal : MonoBehaviour
{
    //포탈에 들어갔을 때 실행될 함수(RoomController가 연결해줌)
    public Action onPortalEntered;

    private bool isUsed = false;

    private void OnTriggerEnter(Collider other)
    {
        //이미 사용된 포탈이거나, 플레이어가 아니면 무시
        if (isUsed || !other.CompareTag("Player")) return;

        isUsed = true;
        Debug.Log("<color=magenta>[Portal]</color> 포탈 탑승! 지도로 돌아갑니다.");

        //포탈 이벤트 실행 (StageManager를 통해 지도가 열리게 됨)
        onPortalEntered?.Invoke();
    }
}