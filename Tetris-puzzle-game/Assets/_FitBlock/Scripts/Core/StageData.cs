using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage", menuName = "FitBlock/Stage Data")]
public class StageData : ScriptableObject
{
    public int stageNumber;
    public int boardWidth = 4;
    public int boardHeight = 4;

    // 보드 활성 셀 (true = 채워야 하는 칸)
    public bool[] boardCells; // boardWidth * boardHeight

    // 허용 조각 풀 (이 스테이지에서 나올 수 있는 후보군)
    public List<PieceData> allowedPieces = new List<PieceData>();

    // 사용할 조각 개수
    public int pieceCount = 3;

    // 하위 호환용 (기존 에디터 코드에서 참조)
    [HideInInspector]
    public List<PieceData> pieces = new List<PieceData>();

    // 좌우 반전 허용 여부 (항상 true)
    public bool allowFlip = true;

    // 에디터 전용 검증 정보
    public bool isValidated = false;
    public int solutionCount = 0;

    public void InitBoard(int width, int height)
    {
        boardWidth = width;
        boardHeight = height;
        boardCells = new bool[width * height];
    }

    public bool GetBoardCell(int x, int y)
    {
        if (boardCells == null || x < 0 || x >= boardWidth || y < 0 || y >= boardHeight) return false;
        return boardCells[y * boardWidth + x];
    }

    public void SetBoardCell(int x, int y, bool value)
    {
        if (boardCells == null || x < 0 || x >= boardWidth || y < 0 || y >= boardHeight) return;
        boardCells[y * boardWidth + x] = value;
    }

    // 보드에서 활성화된 셀 수 반환
    public int GetActiveCellCount()
    {
        int count = 0;
        if (boardCells == null) return 0;
        foreach (var c in boardCells)
            if (c) count++;
        return count;
    }

    // 조각들의 총 셀 수 반환 (pieces 리스트 기준)
    public int GetTotalPieceCells()
    {
        int count = 0;
        foreach (var p in pieces)
            if (p != null)
                count += p.GetNormalizedCells().Count;
        return count;
    }

    // 보드 셀 수와 조각 셀 수가 일치하는지 확인
    public bool IsCellCountValid()
    {
        return GetActiveCellCount() == GetTotalPieceCells();
    }
}
