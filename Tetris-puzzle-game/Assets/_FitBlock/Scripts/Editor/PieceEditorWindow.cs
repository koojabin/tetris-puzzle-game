using UnityEngine;
using UnityEditor;
using System.IO;

public class PieceEditorWindow : EditorWindow
{
    private PieceData _piece;
    private int _gridSize = 4;
    private const float CELL_SIZE = 40f;
    private const float PADDING = 10f;

    [MenuItem("FitBlock/Piece Editor")]
    public static void Open()
    {
        var window = GetWindow<PieceEditorWindow>("Piece Editor");
        window.minSize = new Vector2(400, 500);
    }

    public static void OpenWithPiece(PieceData piece)
    {
        var window = GetWindow<PieceEditorWindow>("Piece Editor");
        window.minSize = new Vector2(400, 500);
        window._piece = piece;
        window._gridSize = piece.gridSize;
    }

    private void OnGUI()
    {
        GUILayout.Label("Piece Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 기존 에셋 불러오기
        var newPiece = (PieceData)EditorGUILayout.ObjectField("피스 에셋", _piece, typeof(PieceData), false);
        if (newPiece != _piece)
        {
            _piece = newPiece;
            if (_piece != null) _gridSize = _piece.gridSize;
        }

        EditorGUILayout.Space(5);

        // 새 피스 생성
        if (GUILayout.Button("새 피스 만들기"))
            CreateNewPiece();

        if (_piece == null)
        {
            EditorGUILayout.HelpBox("피스 에셋을 선택하거나 새로 만드세요.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        DrawPieceSettings();
        EditorGUILayout.Space(10);
        DrawGrid();
        EditorGUILayout.Space(10);
        DrawRotationPreviews();
        EditorGUILayout.Space(10);
        DrawSaveButton();
    }

    private void DrawPieceSettings()
    {
        GUILayout.Label("기본 설정", EditorStyles.boldLabel);

        _piece.pieceName = EditorGUILayout.TextField("이름", _piece.pieceName);
        _piece.pieceColor = EditorGUILayout.ColorField("색상", _piece.pieceColor);

        int newSize = EditorGUILayout.IntSlider("격자 크기", _gridSize, 2, 5);
        if (newSize != _gridSize)
        {
            _gridSize = newSize;
            _piece.Init(_gridSize);
        }
    }

    private void DrawGrid()
    {
        GUILayout.Label("모양 그리기 (클릭으로 켜고 끄기)", EditorStyles.boldLabel);

        if (_piece.cells == null || _piece.cells.Length != _gridSize * _gridSize)
            _piece.Init(_gridSize);

        float totalWidth = _gridSize * CELL_SIZE;
        Rect startRect = GUILayoutUtility.GetRect(totalWidth, _gridSize * CELL_SIZE + PADDING);
        float startX = startRect.x + PADDING;
        float startY = startRect.y + PADDING / 2f;

        for (int y = 0; y < _gridSize; y++)
        {
            for (int x = 0; x < _gridSize; x++)
            {
                Rect cellRect = new Rect(startX + x * CELL_SIZE, startY + y * CELL_SIZE, CELL_SIZE - 2, CELL_SIZE - 2);
                bool filled = _piece.GetCell(x, y);

                // 셀 배경
                EditorGUI.DrawRect(cellRect, filled ? _piece.pieceColor : new Color(0.85f, 0.85f, 0.85f));

                // 테두리
                DrawCellBorder(cellRect, Color.gray);

                // 클릭 처리
                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    _piece.SetCell(x, y, !filled);
                    EditorUtility.SetDirty(_piece);
                    Event.current.Use();
                    Repaint();
                }
            }
        }
    }

    private void DrawRotationPreviews()
    {
        GUILayout.Label("회전 미리보기", EditorStyles.boldLabel);

        float previewCellSize = 16f;
        Rect rowRect = GUILayoutUtility.GetRect(position.width, 80f);
        float startX = rowRect.x + PADDING;
        float startY = rowRect.y;

        string[] labels = { "0°", "90°", "180°", "270°" };

        for (int rot = 0; rot < 4; rot++)
        {
            var cells = _piece.GetRotatedCells(rot);

            int maxX = 0, maxY = 0;
            foreach (var c in cells) { if (c.x > maxX) maxX = c.x; if (c.y > maxY) maxY = c.y; }

            float previewX = startX + rot * 90f;

            // 라벨
            GUI.Label(new Rect(previewX, startY, 80, 16), labels[rot], EditorStyles.miniLabel);

            // 미리보기 격자
            foreach (var c in cells)
            {
                Rect cellRect = new Rect(previewX + c.x * previewCellSize, startY + 18 + c.y * previewCellSize, previewCellSize - 1, previewCellSize - 1);
                EditorGUI.DrawRect(cellRect, _piece.pieceColor);
            }
        }
    }

    private void DrawSaveButton()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("저장", GUILayout.Height(35)))
        {
            EditorUtility.SetDirty(_piece);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FitBlock] {_piece.pieceName} 저장 완료");
        }

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("삭제", GUILayout.Height(35), GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("피스 삭제",
                $"'{_piece.pieceName}' 을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", "삭제", "취소"))
            {
                string path = AssetDatabase.GetAssetPath(_piece);
                _piece = null;
                AssetDatabase.DeleteAsset(path);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void CreateNewPiece()
    {
        string folderPath = "Assets/_FitBlock/Data/Pieces";
        if (!System.IO.Directory.Exists(folderPath))
            System.IO.Directory.CreateDirectory(folderPath);
        string path = EditorUtility.SaveFilePanelInProject("새 피스 저장", "NewPiece", "asset", "저장 위치 선택", folderPath);
        if (string.IsNullOrEmpty(path)) return;

        var piece = CreateInstance<PieceData>();
        piece.pieceName = Path.GetFileNameWithoutExtension(path);
        piece.Init(4);
        AssetDatabase.CreateAsset(piece, path);
        AssetDatabase.SaveAssets();
        _piece = piece;
        _gridSize = 4;
    }

    private void DrawCellBorder(Rect rect, Color color)
    {
        float t = 1f;
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, t), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - t, rect.width, t), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, t, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - t, rect.y, t, rect.height), color);
    }
}
