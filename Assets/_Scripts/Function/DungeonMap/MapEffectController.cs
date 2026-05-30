using NUnit.Framework.Constraints;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MapEffectController : MonoBehaviour
{
    public static MapEffectController Instance;

    //지도의 연출용 캐릭터 스프라이트 관리.
    [Header("Character")]
    public Transform characterRoot;
    public Animator characterAnimator;

    //배경화면 스크롤용 변수(플래피 버드 방식)
    [Header("Background Scroll")]
    public RectTransform[] backgrounds; 
    public float backgroundSpeed = 300f;
    public float backgroundWidth = 1920f;

    //지도 연출 관리.
    [Header("Map Transition")]
    public RectTransform mapRoot;
    public float mapMoveDistance = 900f;
    public float mapDownDuration = 0.4f;   // 내려가는 속도
    public float mapUpDuration = 0.8f;     // 올라오는 속도

    
    [Header("References")]
    public DungeonMaker dungeonMaker;
    
    //지도 전환 연출 중 배경 스크롤 중지 제어 변수.
    private bool isBackgroundScrolling = true;
    //스테이지 클리어 후 테마 변경 작업 보호 변수.
    private bool isThemeSyncDone = false;

    //지도 기본 위치
    private Vector2 mapBasePos = new Vector2(250f, 0f);

    //전환 전/후 테마 캐시 
    private bool hasStageTransition = false;
    private Enums.DungeonTheme currentTransitionTheme;
    private Enums.DungeonTheme nextTransitionTheme;

    void Awake()
    {
        Instance = this;

        //테마 전환이 필요한 순간인지 체크.
        if (DungeonManager.Instance != null && DungeonManager.Instance.needStageTransitionEffect)
        {
            DungeonManager.Instance.needStageTransitionEffect = false;

            //테마 설정 초기화.
            currentTransitionTheme = DungeonManager.Instance.currentTheme;
            nextTransitionTheme = DungeonManager.Instance.nextTheme;
            hasStageTransition = true;

            //테마 전환 실행.
            DungeonManager.Instance.EnterNextStage();
        }
    }
    void Start()
    {
        //지도 위치 초기화.
        mapRoot.anchoredPosition = mapBasePos;
        //환경 배경 위치 초기화.
        ResetBackgroundPositions();

        //스테이지 전환 체크.
        if (hasStageTransition)
        {
            // 모든 백그라운드에 현재 테마 적용
            InitAllBackgroundToTheme(currentTransitionTheme);
            // 다음 백그라운드 테마에 적용(다음 테마 있으면 다음 테마로 변경.)
            SetThemeToBackBackground(nextTransitionTheme);
            StartCoroutine(StageTransitionSequence());
        }
        else
        {
            // 맵 위치와 테마 현재 테마로 전체 초기화.
            mapRoot.anchoredPosition = mapBasePos;

            InitAllBackgroundToTheme(DungeonManager.Instance.GetCurrentTheme());
        }
    }

    void Update()
    {
        if (isBackgroundScrolling)
            ScrollBackground();
    }

    // 스테이지 전환 연출--------------------------------------------------------------------------------------------------------------
    // 스테이지 전환 연출
    private IEnumerator StageTransitionSequence()
    {
        //스테이지 전환 체크 플래그
        isThemeSyncDone = false;

        //지도 내려가는 효과(스테이지 전환시)
        yield return PlayMapDown();

        //배경 환경 위치 초기화
        ResetBackgroundPositions();

        //지도 올라가는 효과(스테이지 전환시)
        yield return PlayMapUp();
    }

    //배경화면 연출 --------------------------------------------------------------------------------------------------------------
    private void ScrollBackground()
    {
        for (int ScrollBackgroundLoop = 0; ScrollBackgroundLoop < backgrounds.Length; ScrollBackgroundLoop++)
        {
            RectTransform background = backgrounds[ScrollBackgroundLoop];
            background.anchoredPosition += Vector2.left * backgroundSpeed * Time.deltaTime;

            // 첫 배경이 왼쪽으로 전부 빠지면
            if (background.anchoredPosition.x <= -backgroundWidth)
            {
                //우측으로 이동.
                background.anchoredPosition += Vector2.right * backgroundWidth * backgrounds.Length;

                // 스테이지 전환 중이고, 아직 동기화 안했으면 여기서 1회 동기화
                if (hasStageTransition && !isThemeSyncDone)
                {
                    SyncAllBackgroundsToTheme(nextTransitionTheme);
                    isThemeSyncDone = true;
                }
            }
        }
    }
    //배경환경 위치 초기화.
    private void ResetBackgroundPositions()
    {
        backgrounds[0].anchoredPosition = Vector2.zero;
        backgrounds[1].anchoredPosition = new Vector2(backgroundWidth, 0f);
    }
    //현재 테마로 배경화면 전부 초기화.
    private void InitAllBackgroundToTheme(Enums.DungeonTheme theme)
    {
        Sprite sprite = dungeonMaker.GetThemeSprite(theme);
        if (sprite == null) return;

        foreach (var background in backgrounds)
        {
            Image img = background.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
        }
    }

    //스테이지 전환 시 뒤 배경(오른쪽)만 다음 테마로.
    private void SetThemeToBackBackground(Enums.DungeonTheme theme)
    {
        Sprite sprite = dungeonMaker.GetThemeSprite(theme);
        if (sprite == null) return;

        RectTransform rightBackground = GetRightSideBackground();
        Image img = rightBackground.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
    }

    //스테이지 전환 시 바뀐 테마가 나온 후 그 다음 0번테마도 다시 초기화.
    private void SyncAllBackgroundsToTheme(Enums.DungeonTheme theme)
    {
        Sprite sprite = dungeonMaker.GetThemeSprite(theme);
        if (sprite == null) return;

        foreach (var background in backgrounds)
        {
            Image img = background.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
        }
    }

    //인덱스 순서가 아닌 x축 기반으로 하여 배경화면 선택.
    private RectTransform GetRightSideBackground()
    {
        return backgrounds[0].anchoredPosition.x > backgrounds[1].anchoredPosition.x
            ? backgrounds[0]
            : backgrounds[1];
    }

    private RectTransform GetLeftSideBackground()
    {
        return backgrounds[0].anchoredPosition.x < backgrounds[1].anchoredPosition.x
            ? backgrounds[0]
            : backgrounds[1];
    }
   
    //플레이어 캐릭터 연출 --------------------------------------------------------------------------------------------------------------
    //노드 클릭시 플레이어 이동 연출
    public void PlayEnterEffect()
    {
        StartCoroutine(EnterEffectSequence());
    }

    private IEnumerator EnterEffectSequence()
    {
        // 캐릭터 이동 연출
        yield return StartCoroutine(PlayCharacterExitToRight());
        // 클릭한 노드 로직에 따라 씬 전환
        LoadSceneByCurrentNode();
    }

    private void LoadSceneByCurrentNode()
    {
        var type = DungeonManager.Instance.GetCurrentRoomType();

        if (type == Enums.RoomType.Normal ||
            type == Enums.RoomType.Elite ||
            type == Enums.RoomType.Boss)
        {
            SceneLoaderManager.Instance.LoadScene(SceneNames.Dungeon);
        }
        else
        {
            SceneLoaderManager.Instance.LoadScene(SceneNames.Restroom);
        }
    }
   
    //캐릭터 이동 연출 로직.
    public IEnumerator PlayCharacterExitToRight()
    {
        RectTransform charRt = characterRoot as RectTransform;
        if (charRt == null) yield break;

        float speed = 4500f;                 // 체감 속도
        float outX = charRt.anchoredPosition.x + 1600f;

        Coroutine walSoundCoroutine = null;

        while (true)
        {
            charRt.anchoredPosition =
                Vector2.MoveTowards(charRt.anchoredPosition,
                    new Vector2(outX, charRt.anchoredPosition.y),
                    speed * Time.deltaTime
                );

            if (walSoundCoroutine == null)
            {
                walSoundCoroutine = StartCoroutine(PlayerWalkSound());
            }

            // 도착 프레임에서는 yield 안 함
            if (charRt.anchoredPosition.x >= outX)
                break;

            yield return null; // 이동 중에만
        }

    }

    IEnumerator PlayerWalkSound()
    {
        SoundManager.Instance.PlaySFX("playerWalk");

        yield return new WaitForSeconds(SoundManager.Instance.GetSoundData("playerWalk").clip.length);
    }

    //지도 이동 연출 로직--------------------------------------------------------------------------------------------------------------
    //던전 입장시 연출 로직.
    public void MapUpEffect()
    {
        StartCoroutine(MapUpSequence());
    }
    private IEnumerator MapUpSequence()
    {
        //맵 올라오기 끝날 때까지 대기
        yield return StartCoroutine(PlayMapUp());

        //정한 시간동안 대기 후 연출
        yield return new WaitForSeconds(0.1f);

        //DungeonMaker에게 노드 및 선 공개 요청
        dungeonMaker.StartRevealMap();
    }

    // Map 위로 이동 연출
    public IEnumerator PlayMapUp()
    {
        // 맵 올라오는 소리
        dungeonMaker.SetClickBlock(true);
        SoundManager.Instance.PlaySFX("mapOpen");

        Vector2 start = new Vector2(250f, -2000f);
        Vector2 end = mapBasePos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / mapUpDuration;
            mapRoot.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        isBackgroundScrolling = true;
    }

    // Map 아래로 이동 연출
    public IEnumerator PlayMapDown()
    {
        isBackgroundScrolling = false;
        // 맵 내려가는 소리
        SoundManager.Instance.PlaySFX("mapClose");

        Vector2 start = new Vector2(250f, 0f);
        Vector2 end = mapBasePos + Vector2.down * mapMoveDistance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / mapDownDuration;
            mapRoot.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
    }

}
