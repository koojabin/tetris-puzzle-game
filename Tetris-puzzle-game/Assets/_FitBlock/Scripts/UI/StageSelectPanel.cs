using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스테이지 선택 패널 (페이지 방식).
/// 4행 × N열 그리드, 좌우 화살표로 페이지 이동.
/// </summary>
public class StageSelectPanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private StageLoader stageLoader;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private ScrollRect scrollRect;

    [Header("스프라이트")]
    [SerializeField] private Sprite btnUnlockedSprite;
    [SerializeField] private Sprite btnLockedSprite;
    [SerializeField] private Sprite checkMarkSprite;

    [Header("페이지 화살표")]
    [SerializeField] private Sprite arrowSprite;

    [Header("숫자 스프라이트 (0~9)")]
    [Tooltip("Hierarchical Challenge 폴더의 0.png~9.png를 순서대로 할당")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];

    // 버튼 크기 설정
    private const float BTN_SIZE = 160f;
    private const float BTN_GAP = 20f;
    private const int ROWS = 4;
    private const int COLS = 5;
    private const int PER_PAGE = ROWS * COLS;

    // 화살표 크기 (설정 버튼과 동일)
    private const float ARROW_SIZE = 80f;

    private int _currentPage = 0;
    private int _totalPages = 1;

    private GameObject _prevArrow;
    private GameObject _nextArrow;

    private void OnEnable()
    {
        _currentPage = 0;
        Rebuild();
    }

    public void SetLoader(StageLoader loader) => stageLoader = loader;

    private void Rebuild()
    {
        if (stageLoader == null || buttonContainer == null) return;

        var stages = stageLoader.Stages;
        _totalPages = Mathf.CeilToInt((float)stages.Count / PER_PAGE);
        if (_totalPages < 1) _totalPages = 1;

        // ScrollRect 비활성화 (페이지 방식이므로)
        if (scrollRect != null)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = false;
        }

        BuildPage();
    }

    private void BuildPage()
    {
        if (stageLoader == null || buttonContainer == null) return;

        // 기존 자식 제거
        for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            Destroy(buttonContainer.GetChild(i).gameObject);

        // 화살표도 제거 후 재생성
        if (_prevArrow != null) { Destroy(_prevArrow); _prevArrow = null; }
        if (_nextArrow != null) { Destroy(_nextArrow); _nextArrow = null; }

        var stages = stageLoader.Stages;
        int startIndex = _currentPage * PER_PAGE;
        int endIndex = Mathf.Min(startIndex + PER_PAGE, stages.Count);

        // 그리드 전체 크기 계산 (버튼 영역 중앙 배치용)
        float gridWidth = COLS * BTN_SIZE + (COLS - 1) * BTN_GAP;
        float gridHeight = ROWS * BTN_SIZE + (ROWS - 1) * BTN_GAP;
        float gridStartX = -gridWidth / 2f;
        float gridStartY = gridHeight / 2f;

        for (int i = startIndex; i < endIndex; i++)
        {
            var stage = stages[i];
            if (stage == null) continue;
            bool unlocked = SaveSystem.IsStageUnlocked(stage.stageNumber);
            bool cleared = SaveSystem.IsStageCleared(stage.stageNumber);

            int localIndex = i - startIndex;
            int col = localIndex % COLS;
            int row = localIndex / COLS;
            float x = gridStartX + col * (BTN_SIZE + BTN_GAP) + BTN_SIZE / 2f;
            float y = gridStartY - row * (BTN_SIZE + BTN_GAP) - BTN_SIZE / 2f;

            // 버튼 루트
            var btnGo = new GameObject($"StageBtn_{stage.stageNumber}");
            btnGo.transform.SetParent(buttonContainer, false);

            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(BTN_SIZE, BTN_SIZE);
            rt.anchoredPosition = new Vector2(x, y);

            var img = btnGo.AddComponent<Image>();
            if (unlocked && btnUnlockedSprite != null)
            {
                img.sprite = btnUnlockedSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.45f, 0.72f, 1f);
            }
            else if (!unlocked && btnLockedSprite != null)
            {
                img.sprite = btnLockedSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = unlocked ? new Color(0.45f, 0.72f, 1f) : new Color(0.55f, 0.55f, 0.55f);
            }

            var btn = btnGo.AddComponent<Button>();
            if (!unlocked) btn.interactable = false;

            // 스테이지 번호
            if (digitSprites != null && digitSprites.Length >= 10 && digitSprites[0] != null)
            {
                CreateDigitDisplay(btnGo.transform, stage.stageNumber);
            }
            else
            {
                var numGo = new GameObject("Num");
                numGo.transform.SetParent(btnGo.transform, false);
                var numRt = numGo.AddComponent<RectTransform>();
                numRt.anchorMin = new Vector2(0f, 0.35f);
                numRt.anchorMax = Vector2.one;
                numRt.offsetMin = Vector2.zero;
                numRt.offsetMax = Vector2.zero;
                var numTmp = numGo.AddComponent<TextMeshProUGUI>();
                numTmp.text = stage.stageNumber.ToString();
                numTmp.fontSize = 42;
                numTmp.alignment = TextAlignmentOptions.Center;
                numTmp.color = Color.white;
            }

            // 클리어 체크 표시
            if (cleared)
            {
                var checkGo = new GameObject("Check");
                checkGo.transform.SetParent(btnGo.transform, false);
                var checkRt = checkGo.AddComponent<RectTransform>();
                checkRt.anchorMin = new Vector2(0.5f, 0f);
                checkRt.anchorMax = new Vector2(0.5f, 0f);
                checkRt.pivot = new Vector2(0.5f, 0f);
                checkRt.sizeDelta = new Vector2(36f, 36f);
                checkRt.anchoredPosition = new Vector2(0f, 37f);
                var checkImg = checkGo.AddComponent<Image>();
                if (checkMarkSprite != null)
                {
                    checkImg.sprite = checkMarkSprite;
                    checkImg.preserveAspect = true;
                    checkImg.color = Color.white;
                    checkImg.raycastTarget = false;
                }
                else
                {
                    Destroy(checkImg);
                    var checkTmp = checkGo.AddComponent<TextMeshProUGUI>();
                    checkTmp.text = "\u2714";
                    checkTmp.fontSize = 28;
                    checkTmp.alignment = TextAlignmentOptions.Center;
                    checkTmp.color = new Color(0.2f, 0.8f, 0.2f);
                }
            }

            if (unlocked)
            {
                var capturedStage = stage;
                btn.onClick.AddListener(() => OnStageClicked(capturedStage));
            }
        }

        // 컨테이너 크기 설정
        var containerRt = buttonContainer.GetComponent<RectTransform>();
        if (containerRt != null)
            containerRt.sizeDelta = new Vector2(gridWidth, gridHeight);

        // 페이지 화살표 생성
        CreatePageArrows(gridWidth);
    }

    private void CreatePageArrows(float gridWidth)
    {
        // 이전 페이지 (첫 페이지가 아닐 때만) — 하단 왼쪽 (설정버튼과 대칭 x좌표)
        if (_currentPage > 0)
        {
            _prevArrow = CreateArrowButton("PrevPage", false);
            _prevArrow.GetComponent<Button>().onClick.AddListener(PrevPage);
        }

        // 다음 페이지 (마지막 페이지가 아닐 때만) — 하단 오른쪽 (설정버튼과 같은 x좌표)
        if (_currentPage < _totalPages - 1)
        {
            _nextArrow = CreateArrowButton("NextPage", true);
            _nextArrow.GetComponent<Button>().onClick.AddListener(NextPage);
        }
    }

    private GameObject CreateArrowButton(string name, bool flipHorizontal)
    {
        var arrowGo = new GameObject(name);
        // 패널 루트(transform)에 붙여서 Mask 영향 안 받게
        arrowGo.transform.SetParent(transform, false);

        var rt = arrowGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(ARROW_SIZE, ARROW_SIZE);

        if (flipHorizontal)
        {
            // 다음 버튼: 우하단 — 설정버튼과 같은 x좌표 (anchor 1,0 / pivot 1,0 / x=-30)
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-30f, 30f);
        }
        else
        {
            // 이전 버튼: 좌하단 — 설정버튼 대칭 (anchor 0,0 / pivot 0,0 / x=30)
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(30f, 30f);
        }

        // 버튼 클릭 영역용 투명 Image
        var btnImg = arrowGo.AddComponent<Image>();
        btnImg.color = Color.clear;

        // 화살표 이미지를 자식으로 분리
        var imgGo = new GameObject("ArrowIcon");
        imgGo.transform.SetParent(arrowGo.transform, false);
        var imgRt = imgGo.AddComponent<RectTransform>();
        imgRt.anchorMin = Vector2.zero;
        imgRt.anchorMax = Vector2.one;
        imgRt.offsetMin = Vector2.zero;
        imgRt.offsetMax = Vector2.zero;

        var img = imgGo.AddComponent<Image>();
        if (arrowSprite != null)
        {
            img.sprite = arrowSprite;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.6f, 0.7f, 0.9f);
        }
        img.raycastTarget = false;

        // 다음 페이지 화살표는 자식 이미지만 좌우 반전
        if (flipHorizontal)
            imgGo.transform.localScale = new Vector3(-1f, 1f, 1f);

        arrowGo.AddComponent<Button>();
        return arrowGo;
    }

    private void PrevPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            BuildPage();
        }
    }

    private void NextPage()
    {
        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            BuildPage();
        }
    }

    private void CreateDigitDisplay(Transform parent, int number)
    {
        string digits = number.ToString();
        float digitW = digits.Length > 1 ? 32f : 40f;
        float digitH = digits.Length > 1 ? 42f : 50f;
        float gap = -2f;
        float totalW = digits.Length * digitW + (digits.Length - 1) * gap;

        var container = new GameObject("DigitContainer");
        container.transform.SetParent(parent, false);
        var containerRt = container.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.pivot = new Vector2(0.5f, 0.5f);
        containerRt.anchoredPosition = new Vector2(0f, 18f);
        containerRt.sizeDelta = new Vector2(totalW, digitH);

        float startX = -totalW / 2f + digitW / 2f;
        for (int d = 0; d < digits.Length; d++)
        {
            int digitVal = digits[d] - '0';
            if (digitVal < 0 || digitVal > 9 || digitSprites[digitVal] == null) continue;

            var digitGo = new GameObject($"Digit_{d}");
            digitGo.transform.SetParent(container.transform, false);
            var drt = digitGo.AddComponent<RectTransform>();
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(digitW, digitH);
            drt.anchoredPosition = new Vector2(startX + d * (digitW + gap), 0f);

            var digitImg = digitGo.AddComponent<Image>();
            digitImg.sprite = digitSprites[digitVal];
            digitImg.preserveAspect = true;
            digitImg.raycastTarget = false;
        }
    }

    private void OnStageClicked(StageData stage)
    {
        StageManager.Instance.LoadStage(stage);
        GameUIManager.Instance?.RefreshHUD();
        GameUIManager.Instance?.ShowGamePanel();
    }
}
