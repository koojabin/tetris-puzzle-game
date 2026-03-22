using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 백트래킹 기반 퍼즐 솔버.
/// 스테이지에 정답이 존재하는지, 몇 가지 정답이 있는지 검증.
/// </summary>
public static class PuzzleSolver
{
    private const int MAX_SOLUTIONS = 10; // 최대 탐색 정답 수 (성능 제한)

    /// <summary>stage.pieces를 사용하여 풀이 (에디터 호환)</summary>
    public static int Solve(StageData stage, int maxSolutions = MAX_SOLUTIONS)
    {
        return Solve(stage, stage.pieces, maxSolutions);
    }

    /// <summary>보드는 stage에서, 조각은 별도 리스트로 풀이 검증</summary>
    public static int Solve(StageData stage, List<PieceData> pieceList, int maxSolutions = MAX_SOLUTIONS)
    {
        if (stage == null || pieceList == null || pieceList.Count == 0) return 0;

        // 셀 수 검증
        int boardCells = stage.GetActiveCellCount();
        int pieceCells = 0;
        foreach (var p in pieceList)
            if (p != null) pieceCells += p.GetNormalizedCells().Count;
        if (boardCells != pieceCells) return 0;

        // 보드 상태 복사
        int[,] board = new int[stage.boardWidth, stage.boardHeight];
        for (int y = 0; y < stage.boardHeight; y++)
            for (int x = 0; x < stage.boardWidth; x++)
                board[x, y] = stage.GetBoardCell(x, y) ? 0 : -1;

        var remaining = new List<int>();
        for (int i = 0; i < pieceList.Count; i++)
            if (pieceList[i] != null)
                remaining.Add(i);

        int solutionCount = 0;
        Backtrack(board, stage, pieceList, remaining, ref solutionCount, maxSolutions);
        return solutionCount;
    }

    private static void Backtrack(int[,] board, StageData stage, List<PieceData> pieceList,
        List<int> remaining, ref int count, int maxSolutions)
    {
        if (count >= maxSolutions) return;

        if (remaining.Count == 0)
        {
            count++;
            return;
        }

        int targetX = -1, targetY = -1;
        for (int y = 0; y < stage.boardHeight && targetX == -1; y++)
            for (int x = 0; x < stage.boardWidth && targetX == -1; x++)
                if (board[x, y] == 0)
                { targetX = x; targetY = y; }

        if (targetX == -1) return;

        for (int idx = 0; idx < remaining.Count; idx++)
        {
            if (count >= maxSolutions) return;

            int pieceIdx = remaining[idx];
            var piece = pieceList[pieceIdx];
            if (piece == null) continue;

            var usedOrientations = new HashSet<string>();

            for (int rot = 0; rot < 4; rot++)
            {
                var cells = piece.GetRotatedCells(rot);
                TryPlacement(board, stage, pieceList, remaining, ref count, cells, pieceIdx, idx, targetX, targetY, usedOrientations, maxSolutions);

                if (stage.allowFlip)
                {
                    var flippedCells = piece.GetFlippedCells();
                    var rotated = RotateCells(flippedCells, rot);
                    TryPlacement(board, stage, pieceList, remaining, ref count, rotated, pieceIdx, idx, targetX, targetY, usedOrientations, maxSolutions);
                }

                if (count >= maxSolutions) return;
            }
        }
    }

    private static void TryPlacement(int[,] board, StageData stage, List<PieceData> pieceList,
        List<int> remaining, ref int count,
        List<Vector2Int> cells, int pieceIdx, int listIdx, int targetX, int targetY,
        HashSet<string> usedOrientations, int maxSolutions)
    {
        if (count >= maxSolutions) return;

        foreach (var anchor in cells)
        {
            int offsetX = targetX - anchor.x;
            int offsetY = targetY - anchor.y;

            string key = OrientationKey(cells, offsetX, offsetY);
            if (usedOrientations.Contains(key)) continue;
            usedOrientations.Add(key);

            if (!CanPlace(board, stage, cells, offsetX, offsetY)) continue;

            Place(board, cells, offsetX, offsetY, pieceIdx + 1);
            remaining.RemoveAt(listIdx);

            Backtrack(board, stage, pieceList, remaining, ref count, maxSolutions);

            remaining.Insert(listIdx, pieceIdx);
            Unplace(board, cells, offsetX, offsetY);
        }
    }

    private static bool CanPlace(int[,] board, StageData stage, List<Vector2Int> cells, int ox, int oy)
    {
        foreach (var c in cells)
        {
            int x = c.x + ox, y = c.y + oy;
            if (x < 0 || x >= stage.boardWidth || y < 0 || y >= stage.boardHeight) return false;
            if (board[x, y] != 0) return false;
        }
        return true;
    }

    private static void Place(int[,] board, List<Vector2Int> cells, int ox, int oy, int value)
    {
        foreach (var c in cells)
            board[c.x + ox, c.y + oy] = value;
    }

    private static void Unplace(int[,] board, List<Vector2Int> cells, int ox, int oy)
    {
        foreach (var c in cells)
            board[c.x + ox, c.y + oy] = 0;
    }

    private static List<Vector2Int> RotateCells(List<Vector2Int> input, int times)
    {
        var cells = new List<Vector2Int>(input);
        for (int i = 0; i < times % 4; i++)
        {
            var rotated = new List<Vector2Int>();
            foreach (var c in cells)
                rotated.Add(new Vector2Int(-c.y, c.x));
            cells = NormalizeCells(rotated);
        }
        return cells;
    }

    private static List<Vector2Int> NormalizeCells(List<Vector2Int> input)
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

    private static string OrientationKey(List<Vector2Int> cells, int ox, int oy)
    {
        var parts = new List<string>();
        foreach (var c in cells)
            parts.Add($"{c.x + ox},{c.y + oy}");
        parts.Sort();
        return string.Join("|", parts);
    }
}
