using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 조각 시각화 + 드래그앤드롭 입력 처리.
/// - 트레이 조각: 놓으면 그 자리에 유지 (그리드 배치 실패 시)
/// - 배치된 조각: 드래그로 집어서 이동 가능, 실패 시 원래 그리드 위치로 복귀
/// - 회전: 마지막 터치 조각 기억 → 회전 버튼으로 처리
/// </summary>
public class PieceView : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public PieceData Data { get; private set; }
    public int PieceId { get; private set; }
    public bool IsPlaced { get; private set; }
    public bool IsInTray { get; private set; } = true;
    public Vector2Int GridPosition { get; private set; }

    /// <summary>런타임에 배정된 보석 스프라이트 (null이면 Data.pieceSprite 또는 색상 사용)</summary>
    public Sprite AssignedSprite { get; private set; }

    /// <summary>현재 드래그 중인 조각이 있는지 (트레이 스크롤과 충돌 방지)</summary>
    public static bool AnyPieceDragging { get; private set; }

    private int _rotation = 0;
    private bool _isFlipped = false;
    private List<Vector2Int> _currentCells;

    private Vector3 _trayPosition;
    private bool _isDragging = false;

    // 드래그 시작 전 상태 기억 (실패 시 복귀용)
    private bool _wasPreviouslyPlaced = false;
    private Vector2Int _previousGridPos;
    private Vector3 _previousWorldPos;


    private List<SpriteRenderer> _cellRenderers = new List<SpriteRenderer>();
    private Sprite _squareSprite;
    private SpriteMaskInteraction _maskInteraction = SpriteMaskInteraction.None;

    private const float DRAG_LIFT = 1.2f;

    public void Init(PieceData data, int pieceId, Vector3 trayPosition, Sprite assignedSprite = null)
    {
        Data = data;
        PieceId = pieceId;
        AssignedSprite = assignedSprite;
        _trayPosition = trayPosition;
        transform.position = trayPosition;
        _rotation = 0;
        _isFlipped = false;
        _squareSprite = CreateSquareSprite();
        RefreshVisual();
    }

    public List<Vector2Int> GetCurrentCells() => _currentCells;

    /// <summary>트레이 위치 갱신 (스크롤 시 호출)</summary>
    public void UpdateTrayPosition(Vector3 newPos)
    {
        _trayPosition = newPos;
        if (!IsPlaced && !_isDragging && IsInTray)
            transform.position = newPos;
    }

    /// <summary>마스크 상호작용 설정 (새 렌더러 생성 시에도 유지)</summary>
    public void SetMaskInteraction(SpriteMaskInteraction interaction)
    {
        _maskInteraction = interaction;
        foreach (var sr in _cellRenderers)
            if (sr) sr.maskInteraction = interaction;
    }

    // ── 입력 처리 ─────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[FitBlock] OnPointerDown: {Data.pieceName} (id={PieceId}) assigned={(AssignedSprite != null ? AssignedSprite.name : "NULL")}");
        // 드래그 시작 전 상태 저장
        _wasPreviouslyPlaced = IsPlaced;
        _previousWorldPos = transform.position;
        if (IsPlaced) _previousGridPos = GridPosition;

        // 배치된 조각이면 그리드에서 제거하고 들기
        if (IsPlaced)
        {
            StageManager.Instance.PickUpPlacedPiece(this);
            IsPlaced = false;
        }

        _isDragging = true;
        AnyPieceDragging = true;
        IsInTray = false;
        SetSortingOrder(10);

        // 트레이 축소 → 원래 크기로 복원
        transform.localScale = Vector3.one;

        // 마지막으로 터치한 조각 등록 (회전/반전 버튼용)
        StageManager.Instance.SetLastTouchedPiece(this);

        // 트레이에 "조각 집었음" 알림 (개수 감소 + 다음 조각 표시)
        if (PieceTray.Instance != null)
        {
            PieceTray.Instance.OnPiecePickedUp(this);
            PieceTray.Instance.OnPieceLeftTray(this); // 마스크 해제 (드래그 중 어디서든 보임)
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        transform.position = ScreenToWorld(eventData.position) + Vector3.up * DRAG_LIFT;
        StageManager.Instance.UpdateDragHighlight(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;
        AnyPieceDragging = false;

        bool placed = StageManager.Instance.TryPlacePiece(this);

        if (!placed)
        {
            // 트레이 영역 안에 놓았으면 → 그룹에 합류
            if (PieceTray.Instance != null && PieceTray.Instance.IsInsideTrayArea(transform.position))
            {
                PieceTray.Instance.ReturnPieceToGroup(this);
            }
            else if (_wasPreviouslyPlaced)
            {
                // 원래 그리드 위치로 복귀 시도
                bool restored = StageManager.Instance.TryRestorePiece(this, _previousGridPos);
                if (!restored)
                {
                    transform.position = _previousWorldPos;
                }
            }
            // 트레이에서 온 조각은 놓은 자리에 그대로
        }

        // 트레이에 상태 변화 알림
        if (PieceTray.Instance != null)
            PieceTray.Instance.OnPieceStateChanged(this);

        SetSortingOrder(5);
        StageManager.Instance.Board.ClearHighlight();
    }

    // ── 회전 / 반전 ───────────────────────────────────────

    public void Rotate()
    {
        // 배치된 조각이면 먼저 그리드에서 제거
        if (IsPlaced)
        {
            StageManager.Instance.PickUpPlacedPiece(this);
            IsPlaced = false;
        }

        _rotation = (_rotation + 1) % 4;
        RefreshVisual();
    }

    public void Flip()
    {
        // 배치된 조각이면 먼저 그리드에서 제거
        if (IsPlaced)
        {
            StageManager.Instance.PickUpPlacedPiece(this);
            IsPlaced = false;
        }

        _isFlipped = !_isFlipped;
        RefreshVisual();
    }

    public void ResetTransform()
    {
        _rotation = 0;
        _isFlipped = false;
        transform.position = _trayPosition;
        RefreshVisual();
    }

    // ── 배치 / 복귀 ───────────────────────────────────────

    public void PlaceOnGrid(Vector2Int gridPos, Vector3 worldPos)
    {
        IsPlaced = true;
        GridPosition = gridPos;
        transform.position = worldPos;
        SetSortingOrder(5);
    }

    public void ReturnToTray()
    {
        IsPlaced = false;
        IsInTray = true;
        transform.position = _trayPosition;

        // 트레이 축소 스케일 적용
        if (PieceTray.Instance != null)
            transform.localScale = Vector3.one * PieceTray.Instance.TrayPieceScale;
        SetSortingOrder(5);

        // 트레이에 상태 변화 알림
        if (PieceTray.Instance != null)
            PieceTray.Instance.OnPieceStateChanged(this);
    }

    // ── 시각화 ────────────────────────────────────────────

    private void RefreshVisual()
    {
        _currentCells = _isFlipped
            ? Data.GetFlippedCells()
            : Data.GetNormalizedCells();

        if (_rotation > 0)
        {
            var rotated = new List<Vector2Int>();
            foreach (var c in _currentCells)
            {
                var v = c;
                for (int i = 0; i < _rotation; i++)
                    v = new Vector2Int(-v.y, v.x);
                rotated.Add(v);
            }
            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var c in rotated) { if (c.x < minX) minX = c.x; if (c.y < minY) minY = c.y; }
            _currentCells = new List<Vector2Int>();
            foreach (var c in rotated) _currentCells.Add(new Vector2Int(c.x - minX, c.y - minY));
        }

        RebuildCellObjects();
    }

    private void RebuildCellObjects()
    {
        Debug.Log($"[FitBlock] RebuildCellObjects: {Data.pieceName} (id={PieceId}) assigned={(AssignedSprite != null ? AssignedSprite.name : "NULL")}");

        foreach (var sr in _cellRenderers)
            if (sr) Destroy(sr.gameObject);
        _cellRenderers.Clear();

        float cs = StageManager.Instance != null ? StageManager.Instance.Board.CellSize : 1f;
        float gap = StageManager.Instance != null ? StageManager.Instance.Board.CellGap : 0.05f;

        int maxX = 0, maxY = 0;
        foreach (var c in _currentCells)
        {
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;

            var cellObj = new GameObject($"PieceCell_{c.x}_{c.y}");
            cellObj.transform.SetParent(transform, false);
            cellObj.transform.localPosition = new Vector3(c.x * cs, c.y * cs, 0f);

            var sr = cellObj.AddComponent<SpriteRenderer>();
            if (AssignedSprite != null)
            {
                sr.sprite = AssignedSprite;
                sr.color = Color.white;
                sr.drawMode = SpriteDrawMode.Simple;
                float spriteSize = cs - gap;
                float scale = spriteSize / (sr.sprite.rect.width / sr.sprite.pixelsPerUnit);
                cellObj.transform.localScale = Vector3.one * scale;
            }
            else
            {
                sr.sprite = _squareSprite;
                sr.color = Data.pieceColor;
                sr.size = Vector2.one * (cs - gap);
            }
            sr.sortingOrder = 5;
            sr.maskInteraction = _maskInteraction;
            _cellRenderers.Add(sr);
        }

        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2((maxX + 1) * cs, (maxY + 1) * cs);
            col.offset = new Vector2(maxX * cs * 0.5f, maxY * cs * 0.5f);
        }
    }

    private void SetSortingOrder(int order)
    {
        foreach (var sr in _cellRenderers)
            if (sr) sr.sortingOrder = order;
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 pos = new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(pos);
    }

    private Sprite CreateSquareSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
