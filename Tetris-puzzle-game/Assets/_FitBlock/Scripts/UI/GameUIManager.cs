using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 화면 UI 전체 관리.
/// HUD (스테이지 번호, 클리어 체크), 게임 버튼, 스테이지 선택 패널 전환 담당.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI stageNumberText;
    [SerializeField] private Image clearCheckIcon;

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

        clearPopup?.gameObject.SetActive(false);

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
    }

    public void ShowStageSelect()
    {
        stageSelectPanel?.gameObject.SetActive(true);
        gamePanel?.SetActive(false);
        clearPopup?.gameObject.SetActive(false);
    }

    // ── HUD ──────────────────────────────────────────────

    public void RefreshHUD()
    {
        if (StageManager.Instance?.CurrentStage == null) return;

        int stageNum = StageManager.Instance.CurrentStage.stageNumber;
        if (stageNumberText != null)
            stageNumberText.text = $"STAGE {stageNum}";

        bool cleared = SaveSystem.IsStageCleared(stageNum);
        RefreshClearCheck(cleared);
    }

    private void RefreshClearCheck(bool cleared)
    {
        if (clearCheckIcon != null)
            clearCheckIcon.color = cleared ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.7f, 0.7f, 0.7f, 0.3f);
    }

    // ── 버튼 콜백 ─────────────────────────────────────────

    private void OnRotateClicked() => StageManager.Instance?.RotateLastPiece();

    private void OnFlipClicked() => StageManager.Instance?.FlipLastPiece();

    private void OnUndoClicked() => StageManager.Instance?.Undo();

    private void OnResetClicked()
    {
        if (clearPopup != null && clearPopup.gameObject.activeSelf) return;
        StageManager.Instance?.ResetStage();
        RefreshHUD();
    }

    private void OnMenuClicked() => ShowStageSelect();

    public void GoToTitle() => SceneLoader.LoadTitle();

    // ── 클리어 이벤트 ─────────────────────────────────────

    private void OnStageClear()
    {
        RefreshClearCheck(true);
        clearPopup?.Show();
    }
}
