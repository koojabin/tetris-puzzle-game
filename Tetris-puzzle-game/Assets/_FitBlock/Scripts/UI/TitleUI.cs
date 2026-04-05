using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면 UI 컨트롤러.
/// </summary>
public class TitleUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private SettingsPanel settingsPanel;

    private void Start()
    {
        startButton?.onClick.AddListener(OnStartClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);

        settingsPanel?.Hide();

        AdManager.Instance?.ShowBanner();
    }

    private void OnStartClicked()
    {
        AdManager.Instance?.HideBanner();
        SceneLoader.LoadGame();
    }

    private void OnSettingsClicked() => settingsPanel?.Show();
}
