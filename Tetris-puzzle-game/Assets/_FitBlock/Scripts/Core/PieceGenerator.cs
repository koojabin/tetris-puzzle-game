using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 허용 조각 풀에서 랜덤 조합을 생성하고 풀이 가능 여부를 검증.
/// 스테이지 로드 시 호출되어 매번 다른 조각 조합을 제공.
/// </summary>
public static class PieceGenerator
{
    private const int MAX_ATTEMPTS = 200;

    /// <summary>
    /// 허용 풀에서 pieceCount개를 랜덤으로 뽑아 풀이 가능한 조합을 반환.
    /// 실패 시 null 반환.
    /// </summary>
    public static List<PieceData> Generate(StageData stage)
    {
        if (stage == null || stage.allowedPieces == null || stage.allowedPieces.Count == 0)
            return null;

        // null 제거된 허용 풀
        var pool = new List<PieceData>();
        foreach (var p in stage.allowedPieces)
            if (p != null) pool.Add(p);

        if (pool.Count == 0) return null;

        int targetCells = stage.GetActiveCellCount();
        int pieceCount = stage.pieceCount;

        // 각 조각의 셀 수 미리 캐싱
        var cellCounts = new Dictionary<PieceData, int>();
        foreach (var p in pool)
            if (!cellCounts.ContainsKey(p))
                cellCounts[p] = p.GetNormalizedCells().Count;

        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            var candidate = PickRandom(pool, cellCounts, pieceCount, targetCells);
            if (candidate == null) continue;

            // 풀이 가능한지 검증 (답 1개만 찾으면 됨)
            int solutions = PuzzleSolver.Solve(stage, candidate, 1);
            if (solutions > 0)
            {
                Debug.Log($"[FitBlock] 조각 생성 성공 (시도 {attempt + 1}회)");
                return candidate;
            }
        }

        Debug.LogWarning($"[FitBlock] {MAX_ATTEMPTS}회 시도 후 유효한 조합을 찾지 못했습니다.");
        return null;
    }

    /// <summary>
    /// 풀에서 pieceCount개를 랜덤으로 뽑되, 총 셀 수가 targetCells와 일치하는 조합만 반환.
    /// </summary>
    private static List<PieceData> PickRandom(List<PieceData> pool, Dictionary<PieceData, int> cellCounts,
        int pieceCount, int targetCells)
    {
        var result = new List<PieceData>();
        int currentCells = 0;

        for (int i = 0; i < pieceCount; i++)
        {
            int remaining = pieceCount - i;
            int neededCells = targetCells - currentCells;

            // 남은 슬롯에 넣을 수 있는 조각만 필터링
            var valid = new List<PieceData>();
            foreach (var p in pool)
            {
                int pc = cellCounts[p];
                // 이 조각을 넣었을 때 남은 슬롯에 최소 1셀짜리라도 채울 수 있는지 확인
                int cellsAfter = currentCells + pc;
                int slotsLeft = remaining - 1;
                // 남은 셀이 남은 슬롯 수 이상이고, 풀의 최대 셀로도 채울 수 있어야 함
                int minPossible = cellsAfter + slotsLeft * GetMinCells(pool, cellCounts);
                int maxPossible = cellsAfter + slotsLeft * GetMaxCells(pool, cellCounts);
                if (minPossible <= targetCells && maxPossible >= targetCells)
                    valid.Add(p);
            }

            if (valid.Count == 0) return null;

            var picked = valid[Random.Range(0, valid.Count)];
            result.Add(picked);
            currentCells += cellCounts[picked];
        }

        return currentCells == targetCells ? result : null;
    }

    private static int GetMinCells(List<PieceData> pool, Dictionary<PieceData, int> cellCounts)
    {
        int min = int.MaxValue;
        foreach (var p in pool)
        {
            int c = cellCounts[p];
            if (c < min) min = c;
        }
        return min;
    }

    private static int GetMaxCells(List<PieceData> pool, Dictionary<PieceData, int> cellCounts)
    {
        int max = 0;
        foreach (var p in pool)
        {
            int c = cellCounts[p];
            if (c > max) max = c;
        }
        return max;
    }
}
