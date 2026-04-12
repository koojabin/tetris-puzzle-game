using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 게임 화면 UI 전체 관리.
/// HUD (스테이지 번호, 클리어 체크), 게임 버튼, 스테이지 선택 패널 전환 담당.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private Image stageLevelImage;
    [SerializeField] private RectTransform stageDigitContainer;
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];

    [Header("버튼")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button rotateButton;
    [SerializeField] private Button flipButton;
    [SerializeField] private Button menuButton;

    [Header("패널")]
    [SerializeField] private ClearPopup clearPopup;
    [SerializeField] private GameObject gamePanel;        // HUD + 보드 영역
    [SerializeField] private StageSelectPanel stageSelectPanel;
    [SerializeField] private SettingsPanel settingsPanel;

    [Header("설정 버튼")]
    [SerializeField] private Button settingsButtonHUD;
    [SerializeField] private Button settingsButtonStageSelect;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // FlipButton 미할당 시 이름으로 자동 검색
        if (flipButton == null)
        {
            var found = transform.GetComponentsInChildren<Button>(true);
            foreach (var btn in found)
                if (btn.gameObject.name == "FlipButton")
                { flipButton = btn; break; }
        }

        undoButton?.onClick.AddListener(OnUndoClicked);
        resetButton?.onClick.AddListener(OnResetClicked);
        rotateButton?.onClick.AddListener(OnRotateClicked);
        flipButton?.onClick.AddListener(OnFlipClicked);
        menuButton?.onClick.AddListener(OnMenuClicked);
        settingsButtonHUD?.onClick.AddListener(OnSettingsClicked);
        settingsButtonStageSelect?.onClick.AddListener(OnSettingsClicked);

        clearPopup?.gameObject.SetActive(false);
        settingsPanel?.gameObject.SetActive(false);

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageClear.AddListener(OnStageClear);
            RefreshHUD();
        }

        // 처음엔 스테이지 선택 화면 표시
        if (stageSelectPanel != null)
            ShowStageSelect();
        else
            ShowGamePanel();
    }

    // ── 패널 전환 ─────────────────────────────────────────

    public void ShowGamePanel()
    {
        gamePanel?.SetActive(true);
        stageSelectPanel?.gameObject.SetActive(false);
        HideBannerAd();
    }

    public void ShowStageSelect()
    {
        stageSelectPanel?.gameObject.SetActive(true);
        gamePanel?.SetActive(false);
        clearPopup?.gameObject.SetActive(false);
        ShowBannerAd();
    }

    // ── HUD ──────────────────────────────────────────────

    public void RefreshHUD()
    {
        if (StageManager.Instance?.CurrentStage == null) return;

        int stageNum = StageManager.Instance.CurrentStage.stageNumber;
        UpdateStageDigits(stageNum);
    }

    private void UpdateStageDigits(int number)
    {
        if (stageDigitContainer == null || digitSprites == null || digitSprites.Length < 10) return;

        // 기존 숫자 제거
        for (int i = stageDigitContainer.childCount - 1; i >= 0; i--)
            Destroy(stageDigitContainer.GetChild(i).gameObject);

        string digits = number.ToString();
        float digitW = 36f;
        float digitH = 52f;
        float gap = 4f;
        float totalW = digits.Length * digitW + (digits.Length - 1) * gap;
        float startX = -totalW / 2f + digitW / 2f;

        for (int d = 0; d < digits.Length; d++)
        {
            int val = digits[d] - '0';
            if (val < 0 || val > 9 || digitSprites[val] == null) continue;

            var go = new GameObject($"Digit_{d}");
            go.transform.SetParent(stageDigitContainer, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(digitW, digitH);
            rt.anchoredPosition = new Vector2(startX + d * (digitW + gap), 0f);

            var img = go.AddComponent<Image>();
            img.sprite = digitSprites[val];
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
    }

    // ── 버튼 콜백 ─────────────────────────────────────────

    private void OnRotateClicked() => StageManager.Instance?.RotateLastPiece();

    private void OnFlipClicked() => StageManager.Instance?.FlipLastPiece();

    private void OnUndoClicked() => StageManager.Instance?.Undo();

    private void OnResetClicked()
    {
        if (clearPopup != null && clearPopup.gameObject.activeSelf) return;
        DoReset();
    }

    private void OnMenuClicked() => ShowStageSelect();

    private void OnSettingsClicked() => settingsPanel?.Show();

    public void GoToTitle() => SceneLoader.LoadTitle();

    // ── 리셋 (재시도 광고) ──────────────────────────────────

    private void DoReset()
    {
        int stageNum = StageManager.Instance?.CurrentStage?.stageNumber ?? 0;
        if (AdManager.Instance != null && stageNum > 0)
        {
            AdManager.Instance.ShowRetryInterstitial(stageNum, () =>
            {
                StageManager.Instance?.ResetStage();
                RefreshHUD();
            });
        }
        else
        {
            StageManager.Instance?.ResetStage();
            RefreshHUD();
        }
    }

    // ── 배너 광고 관리 ────────────────────────────────────────

    private void ShowBannerAd() => AdManager.Instance?.ShowBanner();
    private void HideBannerAd() => AdManager.Instance?.HideBanner();

    // ── 클리어 이벤트 ─────────────────────────────────────

    private void OnStageClear()
    {
        int stageNum = StageManager.Instance?.CurrentStage?.stageNumber ?? 0;
        if (AdManager.Instance != null && stageNum > 0)
        {
            AdManager.Instance.ShowClearInterstitial(stageNum, () =>
            {
                clearPopup?.Show();
            });
        }
        else
        {
            clearPopup?.Show();
        }
    }
}
