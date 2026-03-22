using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정 패널. BGM / SFX 토글, 진동 토글.
/// PlayerPrefs로 저장.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    private const string KEY_BGM = "Settings_BGM";
    private const string KEY_SFX = "Settings_SFX";
    private const string KEY_VIB = "Settings_Vibration";

    [Header("토글")]
    [SerializeField] private Toggle bgmToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle vibrationToggle;

    [Header("버튼")]
    [SerializeField] private Button closeButton;

    // 외부에서 현재 설정값 읽기
    public static bool BGMEnabled      => PlayerPrefs.GetInt(KEY_BGM, 1) == 1;
    public static bool SFXEnabled      => PlayerPrefs.GetInt(KEY_SFX, 1) == 1;
    public static bool VibrationEnabled => PlayerPrefs.GetInt(KEY_VIB, 1) == 1;

    private void Awake()
    {
        closeButton?.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        // 저장된 값으로 토글 상태 초기화
        if (bgmToggle != null)       bgmToggle.isOn       = BGMEnabled;
        if (sfxToggle != null)       sfxToggle.isOn       = SFXEnabled;
        if (vibrationToggle != null) vibrationToggle.isOn = VibrationEnabled;

        bgmToggle?.onValueChanged.AddListener(OnBGMChanged);
        sfxToggle?.onValueChanged.AddListener(OnSFXChanged);
        vibrationToggle?.onValueChanged.AddListener(OnVibrationChanged);
    }

    private void OnDisable()
    {
        bgmToggle?.onValueChanged.RemoveListener(OnBGMChanged);
        sfxToggle?.onValueChanged.RemoveListener(OnSFXChanged);
        vibrationToggle?.onValueChanged.RemoveListener(OnVibrationChanged);
    }

    private void OnBGMChanged(bool value)
    {
        PlayerPrefs.SetInt(KEY_BGM, value ? 1 : 0);
        PlayerPrefs.Save();
        // TODO: AudioManager.Instance?.SetBGM(value);
    }

    private void OnSFXChanged(bool value)
    {
        PlayerPrefs.SetInt(KEY_SFX, value ? 1 : 0);
        PlayerPrefs.Save();
        // TODO: AudioManager.Instance?.SetSFX(value);
    }

    private void OnVibrationChanged(bool value)
    {
        PlayerPrefs.SetInt(KEY_VIB, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
