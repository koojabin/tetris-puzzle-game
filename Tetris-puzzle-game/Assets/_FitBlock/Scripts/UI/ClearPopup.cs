using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 클리어 팝업.
/// 스테이지 번호를 숫자 스프라이트로 표시, 체크 표시 애니메이션,
/// 다음 스테이지 / 다시하기 / 스테이지 선택 버튼 제공.
/// </summary>
public class ClearPopup : MonoBehaviour
{
    [Header("스테이지 번호 (숫자 스프라이트)")]
    [SerializeField] private RectTransform stageDigitContainer;
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];

    [Header("체크 표시")]
    [SerializeField] private Image checkImage;

    [Header("버튼")]
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button selectStageButton;

    [Header("애니메이션")]
    [SerializeField] private float checkDelay = 0.3f;
    [SerializeField] private float checkScalePunch = 1.4f;

    private void Awake()
    {
        nextStageButton?.onClick.AddListener(OnNextStage);
        replayButton?.onClick.AddListener(OnReplay);
        selectStageButton?.onClick.AddListener(OnSelectStage);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (StageManager.Instance?.CurrentStage != null)
            UpdateStageDigits(StageManager.Instance.CurrentStage.stageNumber);

        if (nextStageButton != null)
            nextStageButton.interactable = StageManager.Instance != null && StageManager.Instance.HasNextStage;

        if (checkImage != null)
        {
            checkImage.color = new Color(1f, 1f, 1f, 0.3f);
            checkImage.transform.localScale = Vector3.zero;
        }

        StartCoroutine(AnimateCheck());
    }

    private void UpdateStageDigits(int number)
    {
        if (stageDigitContainer == null || digitSprites == null || digitSprites.Length < 10) return;

        // 기존 숫자 제거
        for (int i = stageDigitContainer.childCount - 1; i >= 0; i--)
            Object.Destroy(stageDigitContainer.GetChild(i).gameObject);

        string digits = number.ToString();
        float digitW = 50f;
        float gap = 5f;
        float totalW = digits.Length * digitW + (digits.Length - 1) * gap;
        float startX = -totalW / 2f + digitW / 2f;

        for (int d = 0; d < digits.Length; d++)
        {
            int val = digits[d] - '0';
            if (val < 0 || val > 9 || digitSprites[val] == null) continue;

            var go = new GameObject($"Digit_{d}");
            go.transform.SetParent(stageDigitContainer, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(digitW, digitW);
            rt.anchoredPosition = new Vector2(startX + d * (digitW + gap), 0f);

            var img = go.AddComponent<Image>();
            img.sprite = digitSprites[val];
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
    }

    private IEnumerator AnimateCheck()
    {
        yield return new WaitForSeconds(checkDelay);

        if (checkImage == null) yield break;

        checkImage.color = Color.white;
        StartCoroutine(PunchScale(checkImage.transform));
    }

    private IEnumerator PunchScale(Transform t)
    {
        float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float scale;
            if (progress < 0.5f)
                scale = Mathf.Lerp(0f, checkScalePunch, progress * 2f);
            else
                scale = Mathf.Lerp(checkScalePunch, 1f, (progress - 0.5f) * 2f);
            t.localScale = Vector3.one * scale;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private void OnNextStage()
    {
        gameObject.SetActive(false);
        StageManager.Instance.LoadNextStage();
        GameUIManager.Instance?.RefreshHUD();
        GameUIManager.Instance?.ShowGamePanel();
    }

    private void OnReplay()
    {
        gameObject.SetActive(false);
        int stageNum = StageManager.Instance?.CurrentStage?.stageNumber ?? 0;
        if (AdManager.Instance != null && stageNum > 0)
        {
            AdManager.Instance.ShowRetryInterstitial(stageNum, () =>
            {
                StageManager.Instance?.ResetStage();
                GameUIManager.Instance?.RefreshHUD();
            });
        }
        else
        {
            StageManager.Instance?.ResetStage();
            GameUIManager.Instance?.RefreshHUD();
        }
    }

    private void OnSelectStage()
    {
        gameObject.SetActive(false);
        GameUIManager.Instance?.ShowStageSelect();
    }
}
