using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 게임 씬의 핵심 컨트롤러.
/// 스테이지 로드, 조각 배치/제거, 클리어 판정을 담당.
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("씬 참조")]
    [SerializeField] private BoardRenderer _board;
    [SerializeField] private PieceTray _tray;

    [Header("스테이지")]
    [SerializeField] private StageData _stageData;
    [SerializeField] private StageLoader _stageLoader;

    // 외부 접근용
    public BoardRenderer Board => _board;
    public StageData CurrentStage => _stageData;
    public StageLoader Loader => _stageLoader;
    public bool HasNextStage => _stageLoader != null && _stageLoader.HasNextStage(_stageData.stageNumber);

    // 이벤트
    public UnityEvent OnStageClear;

    private GridSystem _grid;

    // 되돌리기용 배치 이력 스택
    private Stack<PieceView> _undoStack = new Stack<PieceView>();

    // 회전 버튼용 마지막 터치 조각
    public PieceView LastTouchedPiece { get; private set; }

    public void SetLastTouchedPiece(PieceView piece) => LastTouchedPiece = piece;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (_stageData != null) LoadStage(_stageData);
    }

    // ── 스테이지 로드 ─────────────────────────────────────

    // 현재 생성된 조각 리스트 (랜덤)
    private List<PieceData> _generatedPieces;

    public void LoadStage(StageData stage)
    {
        _stageData = stage;
        _undoStack.Clear();

        // 허용 풀에서 랜덤 조각 생성
        if (stage.allowedPieces != null && stage.allowedPieces.Count > 0)
        {
            _generatedPieces = PieceGenerator.Generate(stage);
            if (_generatedPieces == null)
            {
                Debug.LogError($"[FitBlock] Stage {stage.stageNumber}: 유효한 조각 조합을 찾지 못했습니다!");
                return;
            }
        }
        else
        {
            // 하위 호환: 기존 pieces 리스트 사용
            _generatedPieces = new List<PieceData>(stage.pieces);
        }

        // pieces에 생성된 리스트 설정 (PuzzleSolver 등 호환)
        stage.pieces = new List<PieceData>(_generatedPieces);

        _grid = new GridSystem(stage);
        _board.Init(_grid);
        _tray.Init(stage);

        Debug.Log($"[FitBlock] Stage {stage.stageNumber} 로드 완료 (조각 {_generatedPieces.Count}개)");
    }

    // ── 드래그 중 하이라이트 갱신 ─────────────────────────

    public void UpdateDragHighlight(PieceView piece)
    {
        Vector2Int gridPos = GetSnapPosition(piece);
        var cells = GetOffsetCells(piece.GetCurrentCells(), gridPos);
        bool canPlace = _grid.CanPlace(piece.GetCurrentCells(), gridPos);
        _board.ShowHighlight(cells, canPlace);
    }

    // ── 조각 배치 ─────────────────────────────────────────

    /// <summary>조각을 현재 위치 기준 그리드에 배치 시도. 성공하면 true.</summary>
    public bool TryPlacePiece(PieceView piece)
    {
        Vector2Int gridPos = GetSnapPosition(piece);

        if (!_grid.CanPlace(piece.GetCurrentCells(), gridPos))
            return false;

        _grid.Place(piece.GetCurrentCells(), gridPos, piece.PieceId);
        Vector3 worldPos = _board.GridToWorld(gridPos);
        piece.PlaceOnGrid(gridPos, worldPos);

        _undoStack.Push(piece);

        CheckClear();
        return true;
    }

    /// <summary>배치된 조각을 다시 들기</summary>
    public void PickUpPlacedPiece(PieceView piece)
    {
        _grid.Remove(piece.PieceId);
    }

    // ── 되돌리기 ──────────────────────────────────────────

    public void Undo()
    {
        while (_undoStack.Count > 0)
        {
            var piece = _undoStack.Pop();
            if (piece != null && piece.IsPlaced)
            {
                _grid.Remove(piece.PieceId);
                piece.ReturnToTray();
                return;
            }
        }
    }

    // ── 회전 ──────────────────────────────────────────────

    /// <summary>마지막 터치한 조각을 90도 회전. 배치된 조각은 들어서 회전.</summary>
    public void RotateLastPiece()
    {
        if (LastTouchedPiece == null) return;
        LastTouchedPiece.Rotate();
    }

    // ── 반전 ──────────────────────────────────────────────

    /// <summary>마지막 터치한 조각을 좌우 반전. 배치된 조각은 들어서 반전.</summary>
    public void FlipLastPiece()
    {
        if (LastTouchedPiece == null) return;
        LastTouchedPiece.Flip();
    }

    // ── 이전 위치로 복귀 ──────────────────────────────────

    /// <summary>드래그 실패 시 이전 그리드 위치로 복귀 시도. 성공하면 true.</summary>
    public bool TryRestorePiece(PieceView piece, Vector2Int previousGridPos)
    {
        if (!_grid.CanPlace(piece.GetCurrentCells(), previousGridPos))
            return false;

        _grid.Place(piece.GetCurrentCells(), previousGridPos, piece.PieceId);
        piece.PlaceOnGrid(previousGridPos, _board.GridToWorld(previousGridPos));
        return true;
    }

    // ── 다시하기 ──────────────────────────────────────────

    public void ResetStage()
    {
        LoadStage(_stageData);
    }

    // ── 다음 스테이지 ─────────────────────────────────────

    public void LoadNextStage()
    {
        if (_stageLoader == null) return;
        var next = _stageLoader.GetNextStage(_stageData.stageNumber);
        if (next != null) LoadStage(next);
    }

    // ── 힌트 ──────────────────────────────────────────────

    public void UseHint()
    {
        Debug.Log("[FitBlock] 힌트 사용");
        // TODO: 힌트 로직 (정답 위치 1개 미리보기)
    }

    public void UseAdHelp()
    {
        Debug.Log("[FitBlock] 광고 도움 사용");
    }

    // ── 클리어 판정 ───────────────────────────────────────

    private void CheckClear()
    {
        if (!_grid.IsComplete()) return;

        SaveSystem.SaveClear(_stageData.stageNumber);

        Debug.Log($"[FitBlock] Stage {_stageData.stageNumber} 클리어!");
        OnStageClear?.Invoke();
    }

    // ── 유틸 ──────────────────────────────────────────────

    /// <summary>조각의 현재 월드 위치 기준으로 스냅될 그리드 좌표 계산</summary>
    private Vector2Int GetSnapPosition(PieceView piece)
    {
        // 조각의 (0,0) 셀 기준 월드 위치
        Vector3 pieceOrigin = piece.transform.position;
        Vector2Int raw = _board.WorldToGrid(pieceOrigin);

        // 셀 목록의 최소 x,y가 0,0이므로 오프셋 = raw
        return raw;
    }

    private List<Vector2Int> GetOffsetCells(List<Vector2Int> cells, Vector2Int offset)
    {
        var result = new List<Vector2Int>();
        foreach (var c in cells)
            result.Add(new Vector2Int(c.x + offset.x, c.y + offset.y));
        return result;
    }
}
