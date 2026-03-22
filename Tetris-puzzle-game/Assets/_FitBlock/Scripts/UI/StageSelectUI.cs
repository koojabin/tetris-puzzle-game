using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스테이지 선택 화면. StageLoader의 스테이지 목록을 버튼으로 표시.
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private StageLoader stageLoader;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject stageButtonPrefab;

    [Header("게임 화면 연결")]
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject stageSelectPanel;

    private void Start()
    {
        BuildStageButtons();
    }

    private void BuildStageButtons()
    {
        if (stageLoader == null || buttonContainer == null) return;

        // 기존 버튼 제거
        for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            Destroy(buttonContainer.GetChild(i).gameObject);

        foreach (var stage in stageLoader.Stages)
        {
            bool unlocked = SaveSystem.IsStageUnlocked(stage.stageNumber);
            bool cleared = SaveSystem.IsStageCleared(stage.stageNumber);

            var btnGo = Instantiate(stageButtonPrefab, buttonContainer);
            var btn = btnGo.GetComponent<Button>();

            // 스테이지 번호
            var numText = btnGo.transform.Find("NumberText")?.GetComponent<TextMeshProUGUI>();
            if (numText != null) numText.text = stage.stageNumber.ToString();

            // 클리어 체크 표시
            var checkIcon = btnGo.transform.Find("CheckIcon")?.gameObject;
            if (checkIcon != null) checkIcon.SetActive(cleared);

            // 잠금 상태
            var lockIcon = btnGo.transform.Find("LockIcon")?.gameObject;
            if (lockIcon != null) lockIcon.SetActive(!unlocked);

            btn.interactable = unlocked;

            if (unlocked)
            {
                var capturedStage = stage;
                btn.onClick.AddListener(() => OnStageSelected(capturedStage));
            }
        }
    }

    private void OnStageSelected(StageData stage)
    {
        StageManager.Instance.LoadStage(stage);
        GameUIManager.Instance?.RefreshHUD();

        // 스테이지 선택 패널 닫고 게임 화면 표시
        stageSelectPanel?.SetActive(false);
        gameUI?.SetActive(true);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        BuildStageButtons(); // 클리어 상태 갱신
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
