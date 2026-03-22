using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보드 격자 데이터를 관리하는 순수 로직 클래스.
/// 조각 배치 가능 여부 판단, 배치/제거, 클리어 판정을 담당.
/// </summary>
public class GridSystem
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    // 0 = 빈 활성 셀, -1 = 비활성 셀(보드 외부), 1+ = 배치된 조각 ID
    private int[,] _cells;

    public GridSystem(StageData stage)
    {
        Width = stage.boardWidth;
        Height = stage.boardHeight;
        _cells = new int[Width, Height];

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _cells[x, y] = stage.GetBoardCell(x, y) ? 0 : -1;
    }

    public int GetCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return -1;
        return _cells[x, y];
    }

    public bool IsActiveCell(int x, int y) => GetCell(x, y) != -1;
    public bool IsEmptyCell(int x, int y) => GetCell(x, y) == 0;

    /// <summary>조각을 지정 오프셋에 배치 가능한지 확인</summary>
    public bool CanPlace(List<Vector2Int> pieceCells, Vector2Int offset)
    {
        foreach (var c in pieceCells)
        {
            int x = c.x + offset.x;
            int y = c.y + offset.y;
            if (!IsEmptyCell(x, y)) return false;
        }
        return true;
    }

    /// <summary>조각 배치 (pieceId는 1 이상)</summary>
    public void Place(List<Vector2Int> pieceCells, Vector2Int offset, int pieceId)
    {
        foreach (var c in pieceCells)
            _cells[c.x + offset.x, c.y + offset.y] = pieceId;
    }

    /// <summary>특정 pieceId의 조각 제거</summary>
    public void Remove(int pieceId)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (_cells[x, y] == pieceId)
                    _cells[x, y] = 0;
    }

    /// <summary>활성 셀이 모두 채워졌으면 클리어</summary>
    public bool IsComplete()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (_cells[x, y] == 0) return false;
        return true;
    }

    /// <summary>특정 pieceId가 차지하는 셀 목록 반환</summary>
    public List<Vector2Int> GetPieceCells(int pieceId)
    {
        var result = new List<Vector2Int>();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (_cells[x, y] == pieceId)
                    result.Add(new Vector2Int(x, y));
        return result;
    }
}
