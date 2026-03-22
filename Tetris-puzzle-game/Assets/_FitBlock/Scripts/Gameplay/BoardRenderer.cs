using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 격자 보드를 시각화하는 MonoBehaviour.
/// GridSystem 데이터를 받아 셀 오브젝트를 생성/갱신.
/// </summary>
public class BoardRenderer : MonoBehaviour
{
    [Header("셀 크기 설정")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float cellGap = 0.05f;

    [Header("셀 색상")]
    [SerializeField] private Color emptyColor = new Color(0.88f, 0.92f, 1f);
    [SerializeField] private Color borderColor = new Color(0.65f, 0.75f, 0.9f);
    [SerializeField] private Color validHighlight = new Color(0.4f, 0.9f, 0.5f, 0.6f);
    [SerializeField] private Color invalidHighlight = new Color(1f, 0.3f, 0.3f, 0.6f);

    private GridSystem _grid;
    private SpriteRenderer[,] _cellRenderers;
    private SpriteRenderer[,] _highlightRenderers;
    private Sprite _squareSprite;

    public float CellSize => cellSize;
    public float CellGap => cellGap;

    private void Awake()
    {
        _squareSprite = CreateSquareSprite();
    }

    public void Init(GridSystem grid)
    {
        _grid = grid;
        ClearBoard();
        BuildBoard();
        CenterBoard();
    }

    private void BuildBoard()
    {
        _cellRenderers = new SpriteRenderer[_grid.Width, _grid.Height];
        _highlightRenderers = new SpriteRenderer[_grid.Width, _grid.Height];

        for (int y = 0; y < _grid.Height; y++)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                if (!_grid.IsActiveCell(x, y)) continue;

                Vector3 pos = GridToLocal(new Vector2Int(x, y));

                // 배경 셀
                var cellObj = new GameObject($"Cell_{x}_{y}");
                cellObj.transform.SetParent(transform, false);
                cellObj.transform.localPosition = pos;

                var sr = cellObj.AddComponent<SpriteRenderer>();
                sr.sprite = _squareSprite;
                sr.color = emptyColor;
                sr.sortingOrder = 0;
                sr.size = Vector2.one * (cellSize - cellGap);
                _cellRenderers[x, y] = sr;

                // 테두리
                var borderObj = new GameObject($"Border_{x}_{y}");
                borderObj.transform.SetParent(cellObj.transform, false);
                var borderSr = borderObj.AddComponent<SpriteRenderer>();
                borderSr.sprite = _squareSprite;
                borderSr.color = borderColor;
                borderSr.sortingOrder = -1;
                borderSr.size = Vector2.one * cellSize;

                // 하이라이트 레이어
                var hlObj = new GameObject($"Highlight_{x}_{y}");
                hlObj.transform.SetParent(cellObj.transform, false);
                var hlSr = hlObj.AddComponent<SpriteRenderer>();
                hlSr.sprite = _squareSprite;
                hlSr.color = Color.clear;
                hlSr.sortingOrder = 2;
                hlSr.size = Vector2.one * (cellSize - cellGap);
                _highlightRenderers[x, y] = hlSr;
            }
        }
    }

    /// <summary>조각 미리보기 하이라이트 표시 (valid=초록, invalid=빨강)</summary>
    public void ShowHighlight(List<Vector2Int> cells, bool isValid)
    {
        ClearHighlight();
        Color color = isValid ? validHighlight : invalidHighlight;
        foreach (var c in cells)
        {
            if (c.x < 0 || c.x >= _grid.Width || c.y < 0 || c.y >= _grid.Height) continue;
            if (_highlightRenderers[c.x, c.y] != null)
                _highlightRenderers[c.x, c.y].color = color;
        }
    }

    public void ClearHighlight()
    {
        if (_highlightRenderers == null) return;
        foreach (var sr in _highlightRenderers)
            if (sr != null) sr.color = Color.clear;
    }

    /// <summary>그리드 좌표 → 로컬 좌표 변환</summary>
    public Vector3 GridToLocal(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
    }

    /// <summary>월드 좌표 → 그리드 좌표 변환</summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.y / cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>그리드 좌표 → 월드 좌표 변환</summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return transform.TransformPoint(GridToLocal(gridPos));
    }

    private void CenterBoard()
    {
        float offsetX = (_grid.Width - 1) * cellSize * 0.5f;
        float offsetY = (_grid.Height - 1) * cellSize * 0.5f;
        transform.localPosition = new Vector3(-offsetX, -offsetY + 1f, 0f);
    }

    private void ClearBoard()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    // 런타임에서 흰색 사각형 스프라이트 생성
    private Sprite CreateSquareSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
