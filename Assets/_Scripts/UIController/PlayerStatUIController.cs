using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatUIController : MonoBehaviour
{
    [Header("스탯창 패널")]
    public GameObject playerStatUIPanel;                // 플레이어 스탯창 패널

    [Header("유저 정보")]
    public TextMeshProUGUI userLevel;                   // 유저 레벨
    public TextMeshProUGUI userNickName;                // 유저 닉네임

    [Header("플레이어 정보")]
    public Image classImage;                            // 현재 클래스 이미지
    public TextMeshProUGUI playerLevel;                 // 플레이어 레벨
    public TextMeshProUGUI className;                   // 클래스 이름

    [Header("시너지 정보")]
    public SynergyEffectUIController synergyEffectUIController;

    [Header("스탯 설명 텍스트")]
    public TextMeshProUGUI[] statText;                  // 스탯을 표시해줄 텍스트 배열

    [Header("페이지 관련 UI")]
    public TextMeshProUGUI page;                        // 현재 페이지 / 전체 페이지
    public Button previousPageButton;                   // 이전 페이지 버튼
    public Button nextPageButton;                       // 다음 페이지 버튼

    List<(string name, float stat)>playerStats;         // 스탯 리스트
    int rowCount;                                       // 한페이지에 나타나는 최대 줄 수
    int statCount;                                      // 총 스탯 개수
    int currentPage = 0;                                // 현재 페이지
    int maxPage = 0;                                    // 최대 페이지

    SaveLoadManager saveLoadManager;
    SoundManager soundManager;
    Player player;

    private void Awake()
    {
        saveLoadManager = SaveLoadManager.Instance;
        soundManager = SoundManager.Instance;
        player = Player.Instance;
    }

    // 플레이어 스탯창 초기화
    public void InitPlayerStatUIController()
    {
        if (player != null)
        {
            statText = GetComponentInChildren<GridLayoutGroup>().GetComponentsInChildren<TextMeshProUGUI>();

            playerStats = ItemDataHelper.GetPlayerStatInfo(player.finalStats);
            rowCount = 8;
            statCount = playerStats.Count;
            maxPage = statCount / rowCount;

            previousPageButton.onClick.AddListener(ClickPreviousPageButton);
            nextPageButton.onClick.AddListener(ClickNextPageButton);

            UpdatePlayerStatUI();
            UpdatePageText();
        }
    }

    // 플레이어 스탯창 활성화
    public void UpdatePlayerStatUI()
    {
        playerStats = ItemDataHelper.GetPlayerStatInfo(player.finalStats);

        // 유저 정보 업데이트
        userLevel.text = "Lv " + saveLoadManager.userData.level.ToString();
        userNickName.text = saveLoadManager.userData.nickName;

        // 플레이어 정보 -> 플레이어와 연계
        classImage.sprite = player.classStat.classImage;
        playerLevel.text = "Lv " + player.levelSystem.currentLevel.ToString();
        className.text = player.classStat.classType.ToString();

        synergyEffectUIController.ShowSynergyEffect();

        // 한 페이지에 보여줄 스탯 정리
        for ( int i = 0; i < rowCount; i++)
        { 
            if (i + 8 * currentPage < statCount)
            {
                statText[i * 2].text = playerStats[i + 8 * currentPage].name;
                statText[i * 2 + 1].text = playerStats[i + 8 * currentPage].stat.ToString();

                // 체력과 마나는 현재 체력 / 최대 체력으로 표시하기 위해 예외처리
                if (playerStats[i + 8 * currentPage].name == "최대 체력")
                {
                    statText[i * 2].text = "체력";
                    statText[i * 2 + 1].text = player.currentHp.ToString() + " / " 
                        + playerStats[i + 8 * currentPage].stat.ToString();
                }

                if (playerStats[i + 8 * currentPage].name == "최대 마나")
                {
                    statText[i * 2].text = "마나";
                    statText[i * 2 + 1].text = player.currentMp.ToString() + " / "
                        + playerStats[i + 8 * currentPage].stat.ToString();
                }
            }
            else
            {
                statText[i * 2].text = "";
                statText[i * 2 + 1].text = "";
            }
        }
    }
    
    // 스탯창 숨기기
    public void HidePlayerStatUI()
    {
        playerStatUIPanel.SetActive(false);
    }

    // 스탯창 이전버튼 클릭 했을 때
    public void ClickPreviousPageButton()
    {
        soundManager.PlaySFX("buttonClick");

        currentPage--;
        UpdatePageText();

        nextPageButton.interactable = true;
        if (currentPage <= 0)
        {
            previousPageButton.interactable = false;
        }
        UpdatePlayerStatUI();
    }

    // 스탯창 다음 버튼 클릭 했을 때
    public void ClickNextPageButton()
    {
        soundManager.PlaySFX("buttonClick");

        currentPage++;
        UpdatePageText();

        previousPageButton.interactable = true;
        if (currentPage >= maxPage)
        {
            nextPageButton.interactable = false;
        }
        UpdatePlayerStatUI();
    }

    // 현재 페이지 / 전체 페이지 업데이트
    public void UpdatePageText()
    {
        page.text = $"{currentPage + 1} / {maxPage + 1}";
    }
}
