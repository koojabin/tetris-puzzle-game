using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 프로젝트 내 모든 StageData를 로드하고 번호순으로 관리.
/// Resources 폴더 없이 직접 참조 방식 사용.
/// </summary>
[CreateAssetMenu(fileName = "StageLoader", menuName = "FitBlock/Stage Loader")]
public class StageLoader : ScriptableObject
{
    [SerializeField] private List<StageData> stages = new List<StageData>();

    public IReadOnlyList<StageData> Stages => stages;

    public StageData GetStage(int stageNumber)
    {
        return stages.Where(s => s != null).FirstOrDefault(s => s.stageNumber == stageNumber);
    }

    public StageData GetNextStage(int currentStageNumber)
    {
        return stages
            .Where(s => s != null && s.stageNumber > currentStageNumber)
            .OrderBy(s => s.stageNumber)
            .FirstOrDefault();
    }

    public bool HasNextStage(int currentStageNumber)
    {
        return GetNextStage(currentStageNumber) != null;
    }

    public int TotalStageCount => stages.Count;
}
