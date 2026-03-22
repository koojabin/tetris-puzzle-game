using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Piece", menuName = "FitBlock/Piece Data")]
public class PieceData : ScriptableObject
{
    public string pieceName;
    public Color pieceColor = Color.blue;

    // 최대 5x5 격자로 조각 모양 정의 (펜토미노까지 지원)
    public int gridSize = 4;
    public bool[] cells; // gridSize * gridSize 크기의 1D 배열

    public void Init(int size)
    {
        gridSize = size;
        cells = new bool[size * size];
    }

    public bool GetCell(int x, int y)
    {
        if (cells == null || x < 0 || x >= gridSize || y < 0 || y >= gridSize) return false;
        return cells[y * gridSize + x];
    }

    public void SetCell(int x, int y, bool value)
    {
        if (cells == null || x < 0 || x >= gridSize || y < 0 || y >= gridSize) return;
        cells[y * gridSize + x] = value;
    }

    // 채워진 셀의 좌표 목록 반환 (정규화: 최소 x,y를 0,0으로)
    public List<Vector2Int> GetNormalizedCells()
    {
        var result = new List<Vector2Int>();
        if (cells == null) return result;

        int minX = gridSize, minY = gridSize;
        for (int y = 0; y < gridSize; y++)
            for (int x = 0; x < gridSize; x++)
                if (GetCell(x, y))
                {
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                }

        for (int y = 0; y < gridSize; y++)
            for (int x = 0; x < gridSize; x++)
                if (GetCell(x, y))
                    result.Add(new Vector2Int(x - minX, y - minY));

        return result;
    }

    // 90도 시계방향 회전한 셀 목록 반환
    public List<Vector2Int> GetRotatedCells(int rotationCount)
    {
        var cells = GetNormalizedCells();
        for (int i = 0; i < rotationCount % 4; i++)
            cells = RotateOnce(cells);
        return NormalizeCells(cells);
    }

    private List<Vector2Int> RotateOnce(List<Vector2Int> input)
    {
        var result = new List<Vector2Int>();
        foreach (var c in input)
            result.Add(new Vector2Int(-c.y, c.x));
        return result;
    }

    private List<Vector2Int> NormalizeCells(List<Vector2Int> input)
    {
        if (input.Count == 0) return input;
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in input)
        {
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
        }
        var result = new List<Vector2Int>();
        foreach (var c in input)
            result.Add(new Vector2Int(c.x - minX, c.y - minY));
        return result;
    }

    // 좌우 반전한 셀 목록 반환
    public List<Vector2Int> GetFlippedCells()
    {
        var cells = GetNormalizedCells();
        int maxX = 0;
        foreach (var c in cells) if (c.x > maxX) maxX = c.x;
        var result = new List<Vector2Int>();
        foreach (var c in cells)
            result.Add(new Vector2Int(maxX - c.x, c.y));
        return result;
    }
}
