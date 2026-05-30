using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "AllItemsData", menuName = "Scriptable Objects/AllItemsData")]
public class AllItemsData : ScriptableObject
{
    public List<ItemData> allItems;

    [ContextMenu("ID 일괄 재배정")]
    public void ReassignAllIDs()
    {
        // 비어있는 리스트 삭제
        allItems.RemoveAll(item => item == null);

        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] != null)
            {
                allItems[i].iId = i + 1;

                // 추가적으로 아이템 시너지 중복 삭제
                allItems[i].SanitizeData();

#if UNITY_EDITOR
                // 유니티에 수정된 정보를 가르쳐줌
                EditorUtility.SetDirty(allItems[i]);
#endif
            }
        }
    }

}
