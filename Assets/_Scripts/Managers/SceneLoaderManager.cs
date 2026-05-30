using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 씬 이름을 상수로 선언하고 사용
// 씬 추가 할거면 여기에 상수명과 씬 이름 추가해주세요
public static class SceneNames
{
    public const string Title = "00_Title";
    public const string Town = "01_Town";
    public const string DungeonMap = "02_DungeonMap";
    public const string Dungeon = "03_Dungeon";
    public const string Restroom = "04_RestRoom";
}

public class SceneLoaderManager : MonoBehaviour
{
    public static SceneLoaderManager Instance;

    // 씬 로드 시 전달할 데이터 (필요 시 이용)
    // 아마 매니저를 통해서 데이터를 대부분 얻어와서 안쓸 듯
    private object dataToPass = null;

    [Header("Loading UI")]
    public GameObject logoLoading;              // 맨 처음 시작할 때 보여질 로고 로딩화면
    public Image blackBackground;               // 로딩시간이 짧은 씬 전환에는 검은줄을 이용한 간단한 로딩화면
    public GameObject progressGroup;            // 로딩시간이 긴 씬 전환에는 프로그레스 바를 이용한 로딩화면

    [Header("Logo Images")]
    public List<CanvasGroup> logos;             // 로고 이미지 리스트
    public Image logoBackgroundImage;           // 로고 배경 검은화면
    public CanvasGroup logoBackground;

    [Header("progress UI")]
    public Slider progressBar;                  // 프로그레스 바
    public TextMeshProUGUI tipText;             // 팁 텍스트

    public LoadingTipData loadingTipData;       // 팁 모음

    float fadeDuration = 0.5f;                  // 페이드 인/아웃 지속시간
    float stayDuration = 1.2f;                  // 이미지 유지 지속시간
    float transitionDuration = 0.8f;            // 로딩 애니메이션 지속시간
    float maxScale = 3f;                        // 로딩 원 최대 크기
    Material mat;                               // 그래프 쉐이더 매터리얼

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        mat = blackBackground.material;
        foreach (CanvasGroup cg in logos)
        {
            cg.alpha = 0f;
        }

        HideAllUI();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void HideAllUI()
    {
        logoLoading.SetActive(false);
        blackBackground.gameObject.SetActive(false);
        progressGroup.SetActive(false);
    }

    // 씬 전환 (data 입력 시 데이터도 전달 가능)
    public void LoadScene(string sceneName, object data = null)
    {
        dataToPass = data;

        Enums.LoadingType type = Enums.LoadingType.None;

        if (sceneName == SceneNames.Title)
        {
            type = Enums.LoadingType.Logo;
        }
        else if( sceneName == SceneNames.Dungeon || sceneName == SceneNames.Restroom)
        {
            type = Enums.LoadingType.ProgressBar;
        }
        else if(sceneName == SceneNames.Town || sceneName == SceneNames.DungeonMap)
        {
            type = Enums.LoadingType.CircleInBlack;
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
        StartCoroutine(LoadSceneCoroutine(sceneName, type));
    }

    IEnumerator LoadSceneCoroutine(string sceneName, Enums.LoadingType type)
    {
        HideAllUI();

        // 타입에 따라 연출 활성화
        switch (type)
        {
            case Enums.LoadingType.None:
                break;

            case Enums.LoadingType.Logo:
                logoLoading.SetActive(true);
                
                break;

            case Enums.LoadingType.CircleInBlack:
                blackBackground.gameObject.SetActive(true);
                yield return StartCoroutine(AnimationIrisScale(maxScale, 0, transitionDuration));
                break;

            case Enums.LoadingType.ProgressBar:
                progressGroup.SetActive(true);
                break;
        }

        if(type == Enums.LoadingType.Logo)
        {
            // 로고 페이드인 아웃
            foreach (CanvasGroup cg in logos)
            {
                // 페이드 인
                yield return StartCoroutine(FadeLogo(cg, 0, 1, fadeDuration));


                yield return StartCoroutine(SoundManager.Instance.PreloadBGMs());

                yield return new WaitForSeconds(stayDuration);

                // 페이드 아웃
                yield return StartCoroutine(FadeLogo(cg, 1, 0, fadeDuration));
            }
        }

        // 비동기 로딩 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 로딩이 된 정도
        // 0.0 ~ 0.9: 로딩단계 / 0.9 ~ 1.0: 최종 활성화단계
        if (type != Enums.LoadingType.ProgressBar)
        {
            while (op.progress < 0.9f)
            {
                yield return null;
            }
        }

        else
        {
            float fakeProgress = 0f;
            float targetProgress = 0f;
            int randomIndex = Random.Range(0, loadingTipData.tips.Count);

            if (type == Enums.LoadingType.ProgressBar)
            {
                tipText.text = loadingTipData.tips[randomIndex];
            }

            // 실제 로딩과 별개로 가짜 로딩 생성
            while (fakeProgress < 1.0f)
            {
                //0.0 ~0.9: 로딩단계 / 0.9 ~1.0: 최종 활성화단계
                targetProgress = Mathf.Clamp01(op.progress / 0.9f);

                // fakeProgress가 실제 targetProgress를 따라가게 하되, 최대 속도를 제한
                // 0.5f 숫자를 조절해서 로딩 속도를 제어 (낮을수록 느림)
                fakeProgress = Mathf.MoveTowards(fakeProgress, targetProgress, Time.deltaTime * 0.5f);

                // 프로그레스 바 로딩 화면이면 프로그레스 바 채워줌
                if (type == Enums.LoadingType.ProgressBar)
                {
                    progressBar.value = fakeProgress;
                }

                // 로딩이 실제로는 다 끝났고(0.9), 가짜 바도 100%에 도달했다면 루프 탈출
                if (fakeProgress >= 1.0f && op.progress >= 0.9f)
                {
                    break;
                }

                yield return null;
            }
        }

        // 씬 활성화
        op.allowSceneActivation = true;

        // 로고 연출 마무리
        if (type == Enums.LoadingType.Logo)
        {
            yield return StartCoroutine(ColorChange(logoBackgroundImage, Color.black, Color.white, fadeDuration));

            yield return new WaitForSeconds(stayDuration);

            yield return StartCoroutine(FadeLogo(logoBackground, 1, 0, fadeDuration));
        }

        // circleInBackground 연출 마무리
        if (type == Enums.LoadingType.CircleInBlack)
        {
            yield return new WaitForSeconds(0.4f);

            yield return StartCoroutine(AnimationIrisScale(0, maxScale, transitionDuration));
        }

        if (type == Enums.LoadingType.ProgressBar)
        {
             yield return new WaitForSeconds(0.4f);
        }

        HideAllUI();
    }

    // 원이 커지고 작아지는 애니메이션
    IEnumerator AnimationIrisScale(float start, float end, float duration)
    {
        float elapsed = 0f;

        Vector3 startScale = new Vector2(start, start);
        Vector3 endScale = new Vector2(end, end);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mat.SetFloat("_CircleSize", Mathf.Lerp(start, end, elapsed / duration));
            yield return null;
        }
        mat.SetFloat("_CircleSize", end);
        yield return new WaitForSeconds(duration);
    }

    // 투명도 조절하는 코루틴
    IEnumerator FadeLogo(CanvasGroup logo, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            logo.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        logo.alpha = end;
    }

    IEnumerator ColorChange(Image image, Color start, Color end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            image.color = Color.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        image.color = end;
    }

    // 전달한 데이터 얻어오기
    public object GetData()
    {
        object data = dataToPass;
        // 데이터 를 사용했으면 초기화 (다음 씬으로 데이터 넘어가는 것 방지)
        dataToPass = null;
        return data;
    }

    // 현재 씬 이름 리턴
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    // 씬 리로드는 사용할 일이 없을 것 같음
    //public void ReoladCurrentScene()
    //{
    //    string currentSceneName = SceneManager.GetActiveScene().name;
    //    LoadScene(currentSceneName);
    //}

    /// <summary>
    /// 플레이어 HUD 특정 씬에서만 적용
    /// </summary>
    /// <param name="oldScene"></param>
    /// <param name="newScene"></param>
    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        UIManager.Instance?.UpdateHUD(newScene.name);
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
}
