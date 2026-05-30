using System.Collections.Generic;
using UnityEngine;

public class BuffReceiver : MonoBehaviour
{
    struct BuffInfo
    {
        public float value;     // 배율 값: > 1이면 증가, < 1이면 감소
        public float endTime;
    }

    // 소스(시전자) 기준으로 버프를 관리하여 같은 소스의 버프는 중첩되지 않음
    Dictionary<int, BuffInfo> atkBuffs = new Dictionary<int, BuffInfo>(16);
    Dictionary<int, BuffInfo> defBuffs = new Dictionary<int, BuffInfo>(16);

    // 만료된 버프 키 제거용 임시 리스트
    static readonly List<int> s_RemoveKeys = new List<int>(32);

    /// <summary>공격 배율 (공격력 버프 중첩 결과)</summary>
    public float AttackMultiplier => ComputeMultiplier(atkBuffs, defaultValue: 1f);

    /// <summary>
    /// 피격 배율 (방어 버프 중첩 결과)
    /// 1보다 작을수록 받는 피해 감소
    /// </summary>
    public float DamageTakenMultiplier => ComputeMultiplier(defBuffs, defaultValue: 1f);

    public void ApplyAttackBuff(MonsterBase source, float multiplier, float duration)
        => ApplyBuff(atkBuffs, source, multiplier, duration);

    /// <param name="multiplier">
    /// 예) 0.8이면 20% 피해 감소 (받는 피해 * 0.8)
    /// </param>
    public void ApplyDefenseBuff(MonsterBase source, float multiplier, float duration)
        => ApplyBuff(defBuffs, source, multiplier, duration);

    void ApplyBuff(Dictionary<int, BuffInfo> dict, MonsterBase source, float value, float duration)
    {
        if (source == null) return;

        int key = source.GetInstanceID();
        float end = Time.time + duration;

        if (dict.TryGetValue(key, out BuffInfo info))
        {
            // 동일한 소스에서 다시 적용될 경우
            // 공격 버프: 더 큰 값 유지
            // 방어 버프(피해 감소): 더 작은 값 유지
            if (dict == defBuffs) info.value = Mathf.Min(info.value, value);
            else
            {
                info.value = Mathf.Max(info.value, value);
            }

            // 지속 시간은 더 긴 쪽으로 갱신
            info.endTime = Mathf.Max(info.endTime, end);
            dict[key] = info;
        }
        else
        {
            dict[key] = new BuffInfo { value = value, endTime = end };
        }
    }

    float ComputeMultiplier(Dictionary<int, BuffInfo> dict, float defaultValue)
    {
        float mul = defaultValue;
        if (dict.Count == 0) return mul;

        s_RemoveKeys.Clear();

        foreach (var kv in dict)
        {
            if (Time.time >= kv.Value.endTime) s_RemoveKeys.Add(kv.Key);    // 만료된 버프 제거 대상
            else mul *= kv.Value.value;                                     // 유효한 버프는 모두 곱연산
        }

        for (int i = 0; i < s_RemoveKeys.Count; i++) dict.Remove(s_RemoveKeys[i]);

        return mul;
    }
}
