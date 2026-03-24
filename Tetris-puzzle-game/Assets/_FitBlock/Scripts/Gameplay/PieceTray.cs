using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// 화면 오른쪽 세로 조각 트레이.
/// - SpriteMask로 트레이 영역 밖 조각 클리핑
/// - 마우스 드래그 / 터치 드래그로 세로 스크롤
/// - 같은 종류 조각 그룹화 + 개수 배지
/// - 트레이 밖에 둔 조각을 다시 트레이에 놓으면 그룹 합류
/// </summary>
public class PieceTray : MonoBehaviour
{
    public static PieceTray Instance { get; private set; }

    [Header("트레이 설정")]
    [SerializeField] private float spacing = 1.5f;
    [SerializeField] private float trayMarginRight = 1.5f;
    [SerializeField] private float trayWidth = 4f;
    [SerializeField] private float trayTopMargin = 1.0f;
    [SerializeField] private float trayBottomMargin = 2.0f;
    [SerializeField] private float trayPieceScale = 0.55f;

    [Header("트레이 배경")]
    [SerializeField] private Color trayBgColor = new Color(0.78f, 0.85f, 0.95f, 0.8f);

    [Header("개수 배지")]
    [SerializeField] private Color badgeBgColor = new Color(0.2f, 0.2f, 0.2f, 0.85f);
    [SerializeField] private Color badgeTextColor = Color.white;

    private List<PieceGroup> _groups = new List<PieceGroup>();
    private List<PieceView> _allPieces = new List<PieceView>();
    private float _scrollOffset = 0f;
    private float _contentHeight = 0f;

    // 트레이 영역 (월드 좌표)
    private float _trayCenterX;
    private float _trayMinX, _trayMaxX;
    private float _trayMinY, _trayMaxY;
    private float _trayVisibleHeight;

    // 배경/마스크
    private GameObject _trayBgObj;
    private GameObject _maskObj;

    // 스크롤용
    private bool _isDraggingScroll = false;
    private float _lastDragY;

    public IReadOnlyList<PieceView> Pieces => _allPieces;
    public float TrayPieceScale => trayPieceScale;

    private class PieceGroup
    {
        public string pieceName;
        public PieceData data;
        public List<PieceView> pieces = new List<PieceView>();
        public float slotY;
        public GameObject countBadge;
        public TextMesh countText;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        EnhancedTouchSupport.Enable();
    }

    public void Init(StageData stage, Sprite[] gemSprites = null)
    {
        Debug.Log($"[FitBlock] PieceTray.Init: gemSprites={(gemSprites != null ? gemSprites.Length.ToString() : "NULL")}, pieces={stage.pieces.Count}");
        if (gemSprites != null)
            for (int d = 0; d < gemSprites.Length; d++)
                Debug.Log($"[FitBlock]   gemSprites[{d}]={(gemSprites[d] != null ? gemSprites[d].name : "NULL")}");

        ClearTray();
        _scrollOffset = 0f;

        CalculateTrayBounds();
        CreateTrayBackground();
        CreateTrayMask();

        // ── 1. 셀 수 기준 정렬 (적은 것부터) ──
        var sortedPieces = new List<PieceData>();
        foreach (var pieceData in stage.pieces)
            if (pieceData != null) sortedPieces.Add(pieceData);

        sortedPieces.Sort((a, b) =>
            a.GetNormalizedCells().Count.CompareTo(b.GetNormalizedCells().Count));

        // ── 1.5. 보석 스프라이트 랜덤 배정 ──
        Sprite[] assignedSprites = new Sprite[sortedPieces.Count];
        if (gemSprites != null && gemSprites.Length > 0)
        {
            // 셔플된 인덱스로 배정 (겹쳐도 OK, 최대한 다양하게)
            var indices = new List<int>();
            for (int j = 0; j < gemSprites.Length; j++) indices.Add(j);
            // Fisher-Yates 셔플
            for (int j = indices.Count - 1; j > 0; j--)
            {
                int r = Random.Range(0, j + 1);
                (indices[j], indices[r]) = (indices[r], indices[j]);
            }
            for (int j = 0; j < sortedPieces.Count; j++)
                assignedSprites[j] = gemSprites[indices[j % indices.Count]];
        }

        // ── 2. 각 조각 개별 슬롯 배치 ──
        _contentHeight = (sortedPieces.Count - 1) * spacing;
        float startY = _trayMaxY - spacing;

        int pieceId = 1;
        for (int i = 0; i < sortedPieces.Count; i++)
        {
            var pieceData = sortedPieces[i];

            var group = new PieceGroup();
            group.pieceName = pieceData.pieceName;
            group.data = pieceData;
            group.slotY = startY - i * spacing;

            Vector3 slotPos = new Vector3(_trayCenterX, group.slotY, 0f);

            var go = new GameObject($"Piece_{pieceId}_{pieceData.pieceName}");
            go.transform.SetParent(transform, false);
            go.AddComponent<BoxCollider2D>();

            var pieceView = go.AddComponent<PieceView>();
            pieceView.Init(pieceData, pieceId, slotPos, assignedSprites[i]);
            go.transform.localScale = Vector3.one * trayPieceScale;

            SetPieceMaskInteraction(pieceView, SpriteMaskInteraction.VisibleInsideMask);

            group.pieces.Add(pieceView);
            _allPieces.Add(pieceView);
            pieceId++;

            _groups.Add(group);
        }
    }

    // ── 트레이 영역 ─────────────────────────────────────────

    private void CalculateTrayBounds()
    {
        if (Camera.main == null) return;

        float camHalfHeight = Camera.main.orthographicSize;
        float camHalfWidth = camHalfHeight * Camera.main.aspect;

        _trayMaxX = camHalfWidth - trayMarginRight;
        _trayMinX = _trayMaxX - trayWidth;
        _trayCenterX = (_trayMinX + _trayMaxX) * 0.5f;

        _trayMaxY = camHalfHeight - trayTopMargin;
        _trayMinY = -camHalfHeight + trayBottomMargin;
        _trayVisibleHeight = _trayMaxY - _trayMinY;
    }

    private void CreateTrayBackground()
    {
        if (_trayBgObj != null) Destroy(_trayBgObj);

        _trayBgObj = new GameObject("TrayBG");
        _trayBgObj.transform.SetParent(transform, false);
        _trayBgObj.transform.position = new Vector3(_trayCenterX, (_trayMinY + _trayMaxY) * 0.5f, 1f);
        _trayBgObj.transform.localScale = new Vector3(trayWidth, _trayVisibleHeight, 1f);

        var bgSr = _trayBgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = CreateSquareSprite();
        bgSr.color = trayBgColor;
        bgSr.sortingOrder = -2;
    }

    private void CreateTrayMask()
    {
        if (_maskObj != null) Destroy(_maskObj);

        _maskObj = new GameObject("TrayMask");
        _maskObj.transform.SetParent(transform, false);
        _maskObj.transform.position = new Vector3(_trayCenterX, (_trayMinY + _trayMaxY) * 0.5f, 0f);
        _maskObj.transform.localScale = new Vector3(trayWidth, _trayVisibleHeight, 1f);

        var mask = _maskObj.AddComponent<SpriteMask>();
        mask.sprite = CreateSquareSprite();
    }

    public bool IsInsideTrayArea(Vector3 worldPos)
    {
        return worldPos.x >= _trayMinX && worldPos.x <= _trayMaxX &&
               worldPos.y >= _trayMinY && worldPos.y <= _trayMaxY;
    }

    // ── 마스크 제어 ─────────────────────────────────────────

    private void SetPieceMaskInteraction(PieceView piece, SpriteMaskInteraction interaction)
    {
        piece.SetMaskInteraction(interaction);
    }

    // ── 조각 복귀 ──────────────────────────────────────────

    public void ReturnPieceToGroup(PieceView piece)
    {
        piece.ReturnToTray();
        piece.transform.localScale = Vector3.one * trayPieceScale;
        SetPieceMaskInteraction(piece, SpriteMaskInteraction.VisibleInsideMask);

        var group = _groups.Find(g => g.pieces.Contains(piece));
        if (group == null) return;

        UpdateGroupVisibility(group);
        UpdateCountBadge(group);
        RecalculateGroupPositions();
    }

    /// <summary>조각을 트레이에서 꺼낼 때 — 마스크 해제하여 어디서든 보이게</summary>
    public void OnPieceLeftTray(PieceView piece)
    {
        SetPieceMaskInteraction(piece, SpriteMaskInteraction.None);
    }

    // ── 스크롤 ──────────────────────────────────────────────

    private void Update()
    {
        HandleScroll();
    }

    private void HandleScroll()
    {
        if (PieceView.AnyPieceDragging)
        {
            _isDraggingScroll = false;
            return;
        }

        // ── 마우스 클릭+드래그 스크롤 ──
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector3 mouseWorld = ScreenToWorld(mouse.position.ReadValue());

            if (mouse.leftButton.wasPressedThisFrame && IsInsideTrayArea(mouseWorld))
            {
                _isDraggingScroll = true;
                _lastDragY = mouseWorld.y;
            }

            if (mouse.leftButton.isPressed && _isDraggingScroll)
            {
                float delta = mouseWorld.y - _lastDragY;
                if (Mathf.Abs(delta) > 0.01f)
                {
                    _scrollOffset += delta;
                    ClampScroll();
                    UpdateGroupPositions();
                    _lastDragY = mouseWorld.y;
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _isDraggingScroll = false;
            }

            // 마우스 휠도 지원
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                _scrollOffset += wheel * 0.005f;
                ClampScroll();
                UpdateGroupPositions();
            }
        }

        // ── 터치 드래그 스크롤 ──
        var activeTouches = Touch.activeTouches;
        if (activeTouches.Count == 1)
        {
            var touch = activeTouches[0];
            Vector3 worldPos = ScreenToWorld(touch.screenPosition);

            if (touch.began && IsInsideTrayArea(worldPos))
            {
                _isDraggingScroll = true;
                _lastDragY = worldPos.y;
            }
            else if (touch.isInProgress && _isDraggingScroll)
            {
                float delta = worldPos.y - _lastDragY;
                if (Mathf.Abs(delta) > 0.01f)
                {
                    _scrollOffset += delta;
                    ClampScroll();
                    UpdateGroupPositions();
                    _lastDragY = worldPos.y;
                }
            }
            else if (touch.ended)
            {
                _isDraggingScroll = false;
            }
        }
    }

    private float GetMaxScroll()
    {
        float overflow = _contentHeight - _trayVisibleHeight + spacing * 2f;
        return Mathf.Max(0, overflow);
    }

    private void ClampScroll()
    {
        float max = GetMaxScroll();
        _scrollOffset = Mathf.Clamp(_scrollOffset, 0, max);
    }

    private void UpdateGroupPositions()
    {
        foreach (var group in _groups)
        {
            Vector3 newPos = new Vector3(_trayCenterX, group.slotY + _scrollOffset, 0f);

            foreach (var piece in group.pieces)
                piece.UpdateTrayPosition(newPos);

            if (group.countBadge != null)
                UpdateBadgePosition(group, newPos);
        }
    }

    // ── 조각 상태 변화 ─────────────────────────────────────

    public void OnPiecePickedUp(PieceView piece)
    {
        var group = _groups.Find(g => g.pieces.Contains(piece));
        if (group == null) return;

        UpdateGroupVisibility(group);
        UpdateCountBadge(group);
        RecalculateGroupPositions();
    }

    public void OnPieceStateChanged(PieceView changedPiece)
    {
        var group = _groups.Find(g => g.pieces.Contains(changedPiece));
        if (group == null) return;

        UpdateGroupVisibility(group);
        UpdateCountBadge(group);
        RecalculateGroupPositions();
    }

    private void UpdateGroupVisibility(PieceGroup group)
    {
        bool foundTrayVisible = false;
        foreach (var piece in group.pieces)
        {
            if (piece.IsPlaced || !piece.IsInTray)
            {
                piece.gameObject.SetActive(true);
                continue;
            }

            if (!foundTrayVisible)
            {
                piece.gameObject.SetActive(true);
                // 트레이에 있는 조각은 반드시 마스크 적용
                piece.SetMaskInteraction(SpriteMaskInteraction.VisibleInsideMask);
                foundTrayVisible = true;
            }
            else
            {
                piece.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>트레이에 남은 그룹만으로 슬롯 위치 재계산 (빈 슬롯 제거)</summary>
    private void RecalculateGroupPositions()
    {
        float startY = _trayMaxY - spacing;
        int visibleIndex = 0;

        foreach (var group in _groups)
        {
            int remaining = GetTrayRemainingCount(group);
            if (remaining <= 0)
            {
                // 트레이에 조각 없는 그룹 → 배지 숨김
                if (group.countBadge != null)
                    group.countBadge.SetActive(false);
                continue;
            }

            group.slotY = startY - visibleIndex * spacing;
            Vector3 newPos = new Vector3(_trayCenterX, group.slotY + _scrollOffset, 0f);

            foreach (var piece in group.pieces)
                piece.UpdateTrayPosition(newPos);

            if (group.countBadge != null)
                UpdateBadgePosition(group, newPos);

            visibleIndex++;
        }

        _contentHeight = Mathf.Max(0, (visibleIndex - 1) * spacing);
        ClampScroll();
    }

    private int GetTrayRemainingCount(PieceGroup group)
    {
        return group.pieces.Count(p => p.IsInTray && !p.IsPlaced);
    }

    private void UpdateCountBadge(PieceGroup group)
    {
        int remaining = GetTrayRemainingCount(group);

        if (group.countBadge != null)
        {
            if (remaining <= 1)
                group.countBadge.SetActive(false);
            else
            {
                group.countBadge.SetActive(true);
                group.countText.text = $"x{remaining}";
            }
        }
    }

    // ── 배지 ────────────────────────────────────────────────

    private void CreateCountBadge(PieceGroup group, Vector3 slotPos)
    {
        float cs = 1f;
        if (StageManager.Instance != null)
            cs = StageManager.Instance.Board.CellSize;

        var badgeObj = new GameObject($"Badge_{group.pieceName}");
        badgeObj.transform.SetParent(transform, false);

        var cells = group.data.GetNormalizedCells();
        int maxX = 0, maxY = 0;
        foreach (var c in cells)
        {
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }
        float s = trayPieceScale;
        Vector3 badgeLocalPos = slotPos + new Vector3((maxX + 0.8f) * cs * s, (maxY + 0.5f) * cs * s, -0.1f);
        badgeObj.transform.position = badgeLocalPos;

        var bgObj = new GameObject("BG");
        bgObj.transform.SetParent(badgeObj.transform, false);
        var bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = CreateCircleSprite();
        bgSr.color = badgeBgColor;
        bgSr.sortingOrder = 15;
        bgSr.drawMode = SpriteDrawMode.Sliced;
        bgSr.size = new Vector2(0.7f, 0.7f);
        bgSr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(badgeObj.transform, false);
        textObj.transform.localPosition = new Vector3(0, 0, -0.01f);

        var tm = textObj.AddComponent<TextMesh>();
        tm.text = $"x{group.pieces.Count}";
        tm.fontSize = 48;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = badgeTextColor;
        tm.fontStyle = FontStyle.Bold;

        var mr = textObj.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sortingOrder = 16;

        group.countBadge = badgeObj;
        group.countText = tm;
    }

    private void UpdateBadgePosition(PieceGroup group, Vector3 slotPos)
    {
        if (group.countBadge == null) return;

        float cs = 1f;
        if (StageManager.Instance != null)
            cs = StageManager.Instance.Board.CellSize;

        var cells = group.data.GetNormalizedCells();
        int maxX = 0, maxY = 0;
        foreach (var c in cells)
        {
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }
        float s = trayPieceScale;
        group.countBadge.transform.position = slotPos + new Vector3((maxX + 0.8f) * cs * s, (maxY + 0.5f) * cs * s, -0.1f);
    }

    // ── 유틸 ────────────────────────────────────────────────

    public bool AllPlaced() => _allPieces.TrueForAll(p => p.IsPlaced);

    private void ClearTray()
    {
        foreach (var p in _allPieces)
            if (p) Destroy(p.gameObject);
        _allPieces.Clear();

        foreach (var g in _groups)
        {
            if (g.countBadge != null) Destroy(g.countBadge);
        }
        _groups.Clear();

        if (_trayBgObj != null) Destroy(_trayBgObj);
        if (_maskObj != null) Destroy(_maskObj);
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

    private Sprite CreateCircleSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size);
        float center = size * 0.5f;
        float radius = center - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
