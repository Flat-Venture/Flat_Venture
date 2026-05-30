using UnityEngine;
using System.Collections.Generic;
using System;

public class SkillSelectionUI : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform cardParent;

    private List<GameObject> spawnedCards = new List<GameObject>();
    private Action onCardSelectedCallback;

    public void ShowCards(List<SkillCardData> cards, Action onCardSelected)
    {
        onCardSelectedCallback = onCardSelected;

        ClearCards();

        foreach(var cardData in cards)
        {
            GameObject go = Instantiate(cardPrefab, cardParent);
            spawnedCards.Add(go);

            SkillCardUI cardUI = go.GetComponent<SkillCardUI>();
            cardUI.SetUp(cardData, OnCardClicked, this);
        }
        
        gameObject.SetActive(true);
    }

    public void OnCardClicked(SkillCardData data)
    {
        //선택 결과 적용
        Player.Instance.skillController.AddSkillOrLevelUp(data.skillBaseData);

        //UI 닫기
        gameObject.SetActive(false);
        ClearCards();

        //콜백 호출
        onCardSelectedCallback?.Invoke();
    }

    private void ClearCards()
    {
        foreach(var card in spawnedCards)
        {
            Destroy(card);
        }
        spawnedCards.Clear();
    }


    public void PlayerSelectEffect(SkillCardUI selected)
    {
        foreach(var go in spawnedCards)
        {
            var card = go.GetComponent<SkillCardUI>();

            if(card == selected)
            {
                card.PlaySelected();
            }
            else
            {
                card.PlayUnSelected();
            }
        }
    }
}
