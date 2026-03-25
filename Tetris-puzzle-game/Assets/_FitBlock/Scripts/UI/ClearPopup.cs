using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스테이지 클리어 팝업.
/// 체크 표시 애니메이션, 다음 스테이지 / 다시하기 / 스테이지 선택 버튼 제공.
/// </summary>
public class ClearPopup : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stageText;

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

        if (stageText != null && StageManager.Instance?.CurrentStage != null)
            stageText.text = $"Stage {StageManager.Instance.CurrentStage.stageNumber}";

        if (titleText != null)
            titleText.text = "Clear!";

        // 다음 스테이지 버튼 활성 여부
        if (nextStageButton != null)
            nextStageButton.interactable = StageManager.Instance != null && StageManager.Instance.HasNextStage;

        if (checkImage != null)
        {
            checkImage.color = new Color(1f, 1f, 1f, 0.3f);
            checkImage.transform.localScale = Vector3.zero;
        }

        StartCoroutine(AnimateCheck());
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
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(checkScalePunch, 1f, elapsed / duration);
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
        StageManager.Instance?.ResetStage();
        GameUIManager.Instance?.RefreshHUD();
    }

    private void OnSelectStage()
    {
        gameObject.SetActive(false);
        GameUIManager.Instance?.ShowStageSelect();
    }
}
