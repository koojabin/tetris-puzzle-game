using System;
using UnityEngine;
using GoogleMobileAds.Api;

/// <summary>
/// AdMob 광고 관리 싱글턴.
/// 배너(타이틀/스테이지 선택)와 전면 광고(클리어/재시도) 담당.
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    // ── 테스트 광고 ID (출시 전 실제 ID로 교체) ──────────────
#if UNITY_EDITOR
    // 에디터에서는 테스트 광고 ID 사용
    private const string BANNER_AD_UNIT_ID = "ca-app-pub-3940256099942544/6300978111";
    private const string INTERSTITIAL_AD_UNIT_ID = "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_ANDROID
    private const string BANNER_AD_UNIT_ID = "ca-app-pub-6713466500552066/8618431664";
    private const string INTERSTITIAL_AD_UNIT_ID = "ca-app-pub-6713466500552066/2519927178";
#else
    private const string BANNER_AD_UNIT_ID = "unused";
    private const string INTERSTITIAL_AD_UNIT_ID = "unused";
#endif

    // ── 정책 상수 ────────────────────────────────────────────
    private const int AD_FREE_STAGE_LIMIT = 10;   // 1~10 스테이지 광고 없음
    private const int CLEAR_AD_INTERVAL = 3;      // 3회 클리어마다 전면 광고
    private const int RETRY_AD_INTERVAL = 3;      // 3회 재시도마다 전면 광고
    private const string PREF_AD_REMOVED = "AdRemoved";

    // ── 상태 ─────────────────────────────────────────────────
    private BannerView _bannerView;
    private InterstitialAd _interstitialAd;
    private int _clearCount;
    private int _retryCount;
    private bool _adRemoved;
    private bool _sdkReady;
    private bool _bannerVisible;

    public bool IsAdRemoved => _adRemoved;

    // ── 초기화 ───────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _adRemoved = PlayerPrefs.GetInt(PREF_AD_REMOVED, 0) == 1;
    }

    private void Start()
    {
        if (_adRemoved) return;

        MobileAds.Initialize(status =>
        {
            _sdkReady = true;
            Debug.Log("[AdManager] SDK 초기화 완료");
            LoadInterstitial();
        });
    }

    // ── 배너 광고 ────────────────────────────────────────────

    public void ShowBanner()
    {
        if (_adRemoved || !_sdkReady) return;
        if (_bannerVisible) return;

        if (_bannerView == null)
            CreateBanner();

        _bannerView.Show();
        _bannerVisible = true;
    }

    public void HideBanner()
    {
        if (_bannerView == null || !_bannerVisible) return;
        _bannerView.Hide();
        _bannerVisible = false;
    }

    private void CreateBanner()
    {
        _bannerView?.Destroy();
        _bannerView = new BannerView(BANNER_AD_UNIT_ID, AdSize.Banner, AdPosition.Bottom);
        _bannerView.LoadAd(new AdRequest());
    }

    // ── 전면 광고 ────────────────────────────────────────────

    /// <summary>
    /// 스테이지 클리어 시 호출. 조건 충족 시 전면 광고를 보여주고, 완료 후 onComplete 콜백.
    /// 광고 미표시 시 즉시 onComplete 호출.
    /// </summary>
    public void ShowClearInterstitial(int stageNumber, Action onComplete)
    {
        _clearCount++;

        if (ShouldShowInterstitial(stageNumber, _clearCount, CLEAR_AD_INTERVAL))
        {
            ShowInterstitial(onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 재시도(리셋) 시 호출. 조건 충족 시 전면 광고를 보여주고, 완료 후 onComplete 콜백.
    /// </summary>
    public void ShowRetryInterstitial(int stageNumber, Action onComplete)
    {
        _retryCount++;

        if (ShouldShowInterstitial(stageNumber, _retryCount, RETRY_AD_INTERVAL))
        {
            ShowInterstitial(onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private bool ShouldShowInterstitial(int stageNumber, int count, int interval)
    {
        if (_adRemoved) return false;
        if (stageNumber <= AD_FREE_STAGE_LIMIT) return false;
        if (count % interval != 0) return false;
        return true;
    }

    private void ShowInterstitial(Action onComplete)
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                LoadInterstitial();
                onComplete?.Invoke();
            };
            _interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogWarning($"[AdManager] 전면 광고 표시 실패: {error.GetMessage()}");
                LoadInterstitial();
                onComplete?.Invoke();
            };
            _interstitialAd.Show();
        }
        else
        {
            LoadInterstitial();
            onComplete?.Invoke();
        }
    }

    private void LoadInterstitial()
    {
        _interstitialAd?.Destroy();
        _interstitialAd = null;

        InterstitialAd.Load(INTERSTITIAL_AD_UNIT_ID, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogWarning($"[AdManager] 전면 광고 로드 실패: {error.GetMessage()}");
                return;
            }
            _interstitialAd = ad;
        });
    }

    // ── 광고 제거 (IAP) ──────────────────────────────────────

    public void RemoveAds()
    {
        _adRemoved = true;
        PlayerPrefs.SetInt(PREF_AD_REMOVED, 1);
        PlayerPrefs.Save();

        HideBanner();
        _bannerView?.Destroy();
        _bannerView = null;
        _interstitialAd?.Destroy();
        _interstitialAd = null;

        Debug.Log("[AdManager] 광고 제거 완료");
    }

    // ── 정리 ─────────────────────────────────────────────────

    private void OnDestroy()
    {
        _bannerView?.Destroy();
        _interstitialAd?.Destroy();
    }
}
