using UnityEngine;

/// <summary>
/// PlayerPrefs 기반 저장/불러오기.
/// 스테이지 클리어 여부, 최고 클리어 스테이지 번호를 저장.
/// </summary>
public static class SaveSystem
{
    private const string KEY_MAX_STAGE = "MaxStageCleared";
    private const string KEY_CLEAR_PREFIX = "Stage_Cleared_";

    // ── 클리어 ──────────────────────────────────────────────

    /// <summary>스테이지 클리어 저장.</summary>
    public static void SaveClear(int stageNumber)
    {
        PlayerPrefs.SetInt(KEY_CLEAR_PREFIX + stageNumber, 1);

        // 최고 클리어 스테이지 갱신
        int maxCleared = GetMaxStageCleared();
        if (stageNumber > maxCleared)
            PlayerPrefs.SetInt(KEY_MAX_STAGE, stageNumber);

        PlayerPrefs.Save();
    }

    public static bool IsStageCleared(int stageNumber) =>
        PlayerPrefs.GetInt(KEY_CLEAR_PREFIX + stageNumber, 0) > 0;

    public static int GetMaxStageCleared() =>
        PlayerPrefs.GetInt(KEY_MAX_STAGE, 0);

    /// <summary>스테이지가 잠금 해제됐는지 확인 (이전 스테이지 클리어 필요)</summary>
    public static bool IsStageUnlocked(int stageNumber)
    {
        if (stageNumber <= 1) return true;
        return GetMaxStageCleared() >= stageNumber - 1;
    }

    // ── 전체 초기화 ───────────────────────────────────────

    public static void ClearAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
