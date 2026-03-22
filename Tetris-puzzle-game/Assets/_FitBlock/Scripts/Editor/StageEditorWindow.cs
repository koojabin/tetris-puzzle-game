using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class StageEditorWindow : EditorWindow
{
    private StageData _stage;
    private const float CELL_SIZE = 36f;
    private const float PADDING = 10f;
    private Vector2 _scrollPos;

    // 프로젝트 내 모든 PieceData 에셋 캐시
    private PieceData[] _allPieces;

    [MenuItem("FitBlock/Stage Editor")]
    public static void Open()
    {
        var window = GetWindow<StageEditorWindow>("Stage Editor");
        window.minSize = new Vector2(500, 700);
        window.RefreshPieceAssets();
    }

    public static void OpenWithStage(StageData stage)
    {
        var window = GetWindow<StageEditorWindow>("Stage Editor");
        window.minSize = new Vector2(500, 700);
        window._stage = stage;
        window.RefreshPieceAssets();
    }

    private void OnEnable()
    {
        RefreshPieceAssets();
    }

    private void RefreshPieceAssets()
    {
        var guids = AssetDatabase.FindAssets("t:PieceData", new[] { "Assets/_FitBlock/Data/Pieces" });
        var list = new List<PieceData>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var piece = AssetDatabase.LoadAssetAtPath<PieceData>(path);
            if (piece != null) list.Add(piece);
        }
        list.Sort((a, b) =>
        {
            int cmp = a.GetNormalizedCells().Count.CompareTo(b.GetNormalizedCells().Count);
            return cmp != 0 ? cmp : string.Compare(a.pieceName, b.pieceName);
        });
        _allPieces = list.ToArray();
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        GUILayout.Label("Stage Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        var newStage = (StageData)EditorGUILayout.ObjectField("스테이지 에셋", _stage, typeof(StageData), false);
        if (newStage != _stage) _stage = newStage;

        EditorGUILayout.Space(5);

        if (GUILayout.Button("새 스테이지 만들기"))
            CreateNewStage();

        if (_stage == null)
        {
            EditorGUILayout.HelpBox("스테이지 에셋을 선택하거나 새로 만드세요.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space(10);
        DrawStageSettings();
        EditorGUILayout.Space(10);
        DrawBoardEditor();
        EditorGUILayout.Space(10);
        DrawAllowedPieces();
        EditorGUILayout.Space(10);
        DrawValidation();
        EditorGUILayout.Space(10);
        DrawSaveButton();

        EditorGUILayout.EndScrollView();
    }

    private void DrawStageSettings()
    {
        GUILayout.Label("스테이지 설정", EditorStyles.boldLabel);

        _stage.stageNumber = EditorGUILayout.IntField("스테이지 번호", _stage.stageNumber);
        _stage.allowFlip = EditorGUILayout.Toggle("좌우 반전 허용", _stage.allowFlip);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        int newWidth = EditorGUILayout.IntSlider("보드 너비", _stage.boardWidth, 2, 10);
        int newHeight = EditorGUILayout.IntSlider("보드 높이", _stage.boardHeight, 2, 10);
        EditorGUILayout.EndHorizontal();

        if (newWidth != _stage.boardWidth || newHeight != _stage.boardHeight)
        {
            if (EditorUtility.DisplayDialog("보드 크기 변경", "보드 크기를 변경하면 기존 데이터가 초기화됩니다.", "확인", "취소"))
            {
                _stage.InitBoard(newWidth, newHeight);
                _stage.isValidated = false;
                EditorUtility.SetDirty(_stage);
            }
        }
    }

    private void DrawBoardEditor()
    {
        GUILayout.Label($"보드 그리기 (좌클릭=활성, 우클릭=비활성)  [{_stage.boardWidth}×{_stage.boardHeight}]", EditorStyles.boldLabel);

        int activeCells = _stage.GetActiveCellCount();
        GUILayout.Label($"보드 셀: {activeCells}  |  사용 조각 수: {_stage.pieceCount}");

        if (_stage.boardCells == null || _stage.boardCells.Length != _stage.boardWidth * _stage.boardHeight)
            _stage.InitBoard(_stage.boardWidth, _stage.boardHeight);

        float totalWidth = _stage.boardWidth * CELL_SIZE + PADDING * 2;
        float totalHeight = _stage.boardHeight * CELL_SIZE + PADDING * 2;
        Rect startRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
        float startX = startRect.x + PADDING;
        float startY = startRect.y + PADDING;

        for (int y = 0; y < _stage.boardHeight; y++)
        {
            for (int x = 0; x < _stage.boardWidth; x++)
            {
                Rect cellRect = new Rect(startX + x * CELL_SIZE, startY + y * CELL_SIZE, CELL_SIZE - 2, CELL_SIZE - 2);
                bool active = _stage.GetBoardCell(x, y);

                EditorGUI.DrawRect(cellRect, active ? new Color(0.3f, 0.6f, 1f) : new Color(0.85f, 0.85f, 0.85f));
                DrawCellBorder(cellRect, Color.gray);

                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    bool setValue = Event.current.button == 0;
                    _stage.SetBoardCell(x, y, setValue);
                    _stage.isValidated = false;
                    EditorUtility.SetDirty(_stage);
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("전체 채우기"))
        {
            for (int i = 0; i < _stage.boardCells.Length; i++) _stage.boardCells[i] = true;
            _stage.isValidated = false;
            EditorUtility.SetDirty(_stage);
        }
        if (GUILayout.Button("초기화"))
        {
            for (int i = 0; i < _stage.boardCells.Length; i++) _stage.boardCells[i] = false;
            _stage.isValidated = false;
            EditorUtility.SetDirty(_stage);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAllowedPieces()
    {
        GUILayout.Label("허용 조각 풀 (체크된 조각이 후보군)", EditorStyles.boldLabel);

        // 사용 조각 개수
        int newCount = EditorGUILayout.IntSlider("사용 조각 개수", _stage.pieceCount, 1, 20);
        if (newCount != _stage.pieceCount)
        {
            _stage.pieceCount = newCount;
            _stage.isValidated = false;
            EditorUtility.SetDirty(_stage);
        }

        EditorGUILayout.Space(5);

        if (_allPieces == null || _allPieces.Length == 0)
        {
            EditorGUILayout.HelpBox("Assets/_FitBlock/Data/Pieces 에 PieceData 에셋이 없습니다.", MessageType.Warning);
            if (GUILayout.Button("새로고침"))
                RefreshPieceAssets();
            return;
        }

        // 현재 허용 목록을 HashSet으로
        var allowedSet = new HashSet<PieceData>(_stage.allowedPieces);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("전체 선택"))
        {
            _stage.allowedPieces = new List<PieceData>(_allPieces);
            _stage.isValidated = false;
            EditorUtility.SetDirty(_stage);
            allowedSet = new HashSet<PieceData>(_stage.allowedPieces);
        }
        if (GUILayout.Button("전체 해제"))
        {
            _stage.allowedPieces.Clear();
            _stage.isValidated = false;
            EditorUtility.SetDirty(_stage);
            allowedSet.Clear();
        }
        if (GUILayout.Button("새로고침"))
            RefreshPieceAssets();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        // 각 조각을 체크박스로 표시
        for (int i = 0; i < _allPieces.Length; i++)
        {
            var piece = _allPieces[i];
            bool isAllowed = allowedSet.Contains(piece);

            EditorGUILayout.BeginHorizontal();

            bool newAllowed = EditorGUILayout.Toggle(isAllowed, GUILayout.Width(20));

            // 조각 색상 미리보기
            var colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            EditorGUI.DrawRect(colorRect, piece.pieceColor);

            EditorGUILayout.LabelField($"{piece.pieceName} ({piece.GetNormalizedCells().Count}칸)", GUILayout.Width(150));

            // 조각 모양 미니 프리뷰
            DrawMiniPiecePreview(piece);

            if (GUILayout.Button("편집", GUILayout.Width(40)))
                PieceEditorWindow.OpenWithPiece(piece);

            EditorGUILayout.EndHorizontal();

            if (newAllowed != isAllowed)
            {
                if (newAllowed)
                    _stage.allowedPieces.Add(piece);
                else
                    _stage.allowedPieces.Remove(piece);
                _stage.isValidated = false;
                EditorUtility.SetDirty(_stage);
            }
        }

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField($"선택된 조각: {_stage.allowedPieces.Count}종류", EditorStyles.miniLabel);
    }

    private void DrawMiniPiecePreview(PieceData piece)
    {
        var cells = piece.GetNormalizedCells();
        if (cells.Count == 0) return;

        float cellSize = 8f;
        int maxX = 0, maxY = 0;
        foreach (var c in cells) { if (c.x > maxX) maxX = c.x; if (c.y > maxY) maxY = c.y; }

        float w = (maxX + 1) * cellSize + 2;
        float h = (maxY + 1) * cellSize + 2;
        var rect = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));

        foreach (var c in cells)
        {
            var cellRect = new Rect(rect.x + c.x * cellSize, rect.y + c.y * cellSize, cellSize - 1, cellSize - 1);
            EditorGUI.DrawRect(cellRect, piece.pieceColor);
        }
    }

    private void DrawValidation()
    {
        GUILayout.Label("검증", EditorStyles.boldLabel);

        if (_stage.allowedPieces.Count == 0)
        {
            EditorGUILayout.HelpBox("허용 조각을 1개 이상 선택하세요.", MessageType.Warning);
            return;
        }

        if (_stage.GetActiveCellCount() == 0)
        {
            EditorGUILayout.HelpBox("보드에 활성 셀이 없습니다.", MessageType.Warning);
            return;
        }

        if (_stage.isValidated)
        {
            string msg = _stage.solutionCount == 0
                ? "❌ 테스트한 랜덤 조합 중 풀이 가능한 것 없음"
                : $"✅ {_stage.solutionCount}개 랜덤 조합이 풀이 가능";
            MessageType msgType = _stage.solutionCount == 0 ? MessageType.Error : MessageType.Info;
            EditorGUILayout.HelpBox(msg, msgType);
        }

        if (GUILayout.Button("랜덤 조합 검증 (50회 시도)", GUILayout.Height(30)))
        {
            int solvable = TestRandomCombinations(50);
            _stage.solutionCount = solvable;
            _stage.isValidated = true;
            EditorUtility.SetDirty(_stage);
            Repaint();
        }
    }

    private int TestRandomCombinations(int attempts)
    {
        int solvable = 0;
        var pool = new List<PieceData>();
        foreach (var p in _stage.allowedPieces)
            if (p != null) pool.Add(p);

        if (pool.Count == 0) return 0;

        int targetCells = _stage.GetActiveCellCount();
        var cellCounts = new Dictionary<PieceData, int>();
        foreach (var p in pool)
            if (!cellCounts.ContainsKey(p))
                cellCounts[p] = p.GetNormalizedCells().Count;

        for (int i = 0; i < attempts; i++)
        {
            var candidate = PickRandomForValidation(pool, cellCounts, _stage.pieceCount, targetCells);
            if (candidate == null) continue;

            int solutions = PuzzleSolver.Solve(_stage, candidate, 1);
            if (solutions > 0) solvable++;
        }

        Debug.Log($"[FitBlock] 검증 결과: {attempts}회 중 {solvable}회 풀이 가능");
        return solvable;
    }

    private List<PieceData> PickRandomForValidation(List<PieceData> pool, Dictionary<PieceData, int> cellCounts,
        int pieceCount, int targetCells)
    {
        var result = new List<PieceData>();
        int currentCells = 0;

        int minCells = int.MaxValue, maxCells = 0;
        foreach (var p in pool)
        {
            int c = cellCounts[p];
            if (c < minCells) minCells = c;
            if (c > maxCells) maxCells = c;
        }

        for (int i = 0; i < pieceCount; i++)
        {
            int slotsLeft = pieceCount - i - 1;
            int neededCells = targetCells - currentCells;

            var valid = new List<PieceData>();
            foreach (var p in pool)
            {
                int pc = cellCounts[p];
                int after = currentCells + pc;
                int minPossible = after + slotsLeft * minCells;
                int maxPossible = after + slotsLeft * maxCells;
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

    private void DrawSaveButton()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("저장", GUILayout.Height(35)))
        {
            EditorUtility.SetDirty(_stage);
            AssetDatabase.SaveAssets();
            StageLoaderTool.RefreshStageLoader();
            Debug.Log($"[FitBlock] Stage {_stage.stageNumber} 저장 완료");
        }

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("삭제", GUILayout.Height(35), GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("스테이지 삭제",
                $"Stage {_stage.stageNumber} 을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", "삭제", "취소"))
            {
                string path = AssetDatabase.GetAssetPath(_stage);
                _stage = null;
                AssetDatabase.DeleteAsset(path);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void CreateNewStage()
    {
        string folderPath = "Assets/_FitBlock/Data/Stages";
        if (!System.IO.Directory.Exists(folderPath))
            System.IO.Directory.CreateDirectory(folderPath);
        string path = EditorUtility.SaveFilePanelInProject("새 스테이지 저장", "Stage_01", "asset", "저장 위치 선택", folderPath);
        if (string.IsNullOrEmpty(path)) return;

        var stage = CreateInstance<StageData>();
        stage.stageNumber = 1;
        stage.InitBoard(4, 4);
        AssetDatabase.CreateAsset(stage, path);
        AssetDatabase.SaveAssets();
        _stage = stage;
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
