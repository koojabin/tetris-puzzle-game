using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스테이지 선택 패널. Prefab 없이 버튼을 코드로 동적 생성.
/// GameUIManager에서 Show/Hide 호출.
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

    // 버튼 크기 설정
    private const float BTN_SIZE = 160f;
    private const float BTN_GAP = 20f;
    private const int COLS = 4;

    private void OnEnable()
    {
        Rebuild();
    }

    public void SetLoader(StageLoader loader) => stageLoader = loader;

    private void Rebuild()
    {
        if (stageLoader == null || buttonContainer == null) return;

        // 기존 버튼 제거
        for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            Destroy(buttonContainer.GetChild(i).gameObject);

        var stages = stageLoader.Stages;
        for (int i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            if (stage == null) continue;
            bool unlocked = SaveSystem.IsStageUnlocked(stage.stageNumber);
            bool cleared = SaveSystem.IsStageCleared(stage.stageNumber);

            int col = i % COLS;
            int row = i / COLS;
            float x = col * (BTN_SIZE + BTN_GAP);
            float y = -row * (BTN_SIZE + BTN_GAP);

            // 버튼 루트
            var btnGo = new GameObject($"StageBtn_{stage.stageNumber}");
            btnGo.transform.SetParent(buttonContainer, false);

            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(BTN_SIZE, BTN_SIZE);
            rt.anchoredPosition = new Vector2(x, y);

            var img = btnGo.AddComponent<Image>();
            if (unlocked && btnUnlockedSprite != null)
            {
                img.sprite = btnUnlockedSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else if (!unlocked && btnLockedSprite != null)
            {
                img.sprite = btnLockedSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = unlocked ? new Color(0.23f, 0.49f, 0.96f) : new Color(0.55f, 0.55f, 0.55f);
            }

            var btn = btnGo.AddComponent<Button>();
            if (!unlocked) btn.interactable = false;

            // 스테이지 번호 텍스트
            var numGo = new GameObject("Num");
            numGo.transform.SetParent(btnGo.transform, false);
            var numRt = numGo.AddComponent<RectTransform>();
            numRt.anchorMin = Vector2.zero;
            numRt.anchorMax = Vector2.one;
            numRt.offsetMin = new Vector2(0, 30f);
            numRt.offsetMax = Vector2.zero;
            var numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = stage.stageNumber.ToString();
            numTmp.fontSize = 42;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;

            // 클리어 체크 표시
            if (cleared)
            {
                var checkGo = new GameObject("Check");
                checkGo.transform.SetParent(btnGo.transform, false);
                var checkRt = checkGo.AddComponent<RectTransform>();
                checkRt.anchorMin = new Vector2(1f, 0f);
                checkRt.anchorMax = new Vector2(1f, 0f);
                checkRt.pivot = new Vector2(1f, 0f);
                checkRt.sizeDelta = new Vector2(40f, 40f);
                checkRt.anchoredPosition = new Vector2(-8f, 8f);
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
                    // 폴백: 텍스트
                    Destroy(checkImg);
                    var checkTmp = checkGo.AddComponent<TextMeshProUGUI>();
                    checkTmp.text = "\u2714";
                    checkTmp.fontSize = 28;
                    checkTmp.alignment = TextAlignmentOptions.Center;
                    checkTmp.color = new Color(0.2f, 0.8f, 0.2f);
                }
            }

            // 잠금 아이콘 (텍스트)
            if (!unlocked)
            {
                var lockGo = new GameObject("Lock");
                lockGo.transform.SetParent(btnGo.transform, false);
                var lockRt = lockGo.AddComponent<RectTransform>();
                lockRt.anchorMin = Vector2.zero;
                lockRt.anchorMax = Vector2.one;
                lockRt.offsetMin = lockRt.offsetMax = Vector2.zero;
                var lockTmp = lockGo.AddComponent<TextMeshProUGUI>();
                lockTmp.text = "Lock";
                lockTmp.fontSize = 24;
                lockTmp.alignment = TextAlignmentOptions.Center;
                lockTmp.color = new Color(1f, 1f, 1f, 0.8f);
            }

            if (unlocked)
            {
                var capturedStage = stage;
                btn.onClick.AddListener(() => OnStageClicked(capturedStage));
            }
        }

        // 콘텐츠 높이 갱신 (너비는 고정, 높이만 변경)
        int rows = Mathf.CeilToInt((float)stages.Count / COLS);
        float totalHeight = rows * (BTN_SIZE + BTN_GAP) + BTN_GAP;
        var containerRt = buttonContainer.GetComponent<RectTransform>();
        if (containerRt != null)
            containerRt.sizeDelta = new Vector2(containerRt.sizeDelta.x, totalHeight);
    }

    private void OnStageClicked(StageData stage)
    {
        StageManager.Instance.LoadStage(stage);
        GameUIManager.Instance?.RefreshHUD();
        GameUIManager.Instance?.ShowGamePanel();
    }
}
