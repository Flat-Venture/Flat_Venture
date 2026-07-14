using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 닿으면 지정된 이벤트(지도로 돌아가기 등)를 실행하는 포탈 부품
/// </summary>
public class Portal : MonoBehaviour
{
    //포탈에 들어갔을 때 실행될 함수(RoomController가 연결해줌)
    public Action onPortalEntered;

    private bool isUsed = false;
    private readonly HashSet<Collider> blockedUntilExit = new HashSet<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        //이미 사용된 포탈이거나, 플레이어가 아니면 무시
        if (isUsed || !other.CompareTag("Player")) return;

        if (ShouldBlockUntilExit(other))
        {
            return;
        }

        EnterPortal();
    }

    private void OnTriggerStay(Collider other)
    {
        if (isUsed || !other.CompareTag("Player")) return;

        if (ShouldBlockUntilExit(other))
        {
            return;
        }

        EnterPortal();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        blockedUntilExit.Remove(other);
    }

    private bool ShouldBlockUntilExit(Collider playerCollider)
    {
        if (FlatVenture.Reward.DungeonRewardSelectionBehaviour.IsOpen)
        {
            blockedUntilExit.Add(playerCollider);
            return true;
        }

        return blockedUntilExit.Contains(playerCollider);
    }

    private void EnterPortal()
    {
        isUsed = true;
        Debug.Log("<color=magenta>[Portal]</color> 포탈 탑승! 지도로 돌아갑니다.");

        //포탈 이벤트 실행 (StageManager를 통해 지도가 열리게 됨)
        onPortalEntered?.Invoke();
    }
}
