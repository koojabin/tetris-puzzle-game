using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class StageListWindow : EditorWindow
{
    private List<StageData> _stages = new List<StageData>();
    private Vector2 _scrollPos;

    [MenuItem("FitBlock/Stage List")]
    public static void Open()
    {
        var window = GetWindow<StageListWindow>("Stage List");
        window.minSize = new Vector2(500, 400);
        window.RefreshStageList();
    }

    private void OnFocus() => RefreshStageList();

    private void RefreshStageList()
    {
        _stages.Clear();
        string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { "Assets/_FitBlock/Data/Stages" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (stage != null) _stages.Add(stage);
        }
        _stages = _stages.OrderBy(s => s.stageNumber).ToList();
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(5);
        DrawStats();
        EditorGUILayout.Space(5);
        DrawStageList();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Stage List", EditorStyles.boldLabel, GUILayout.Width(100));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
            RefreshStageList();

        if (GUILayout.Button("새 스테이지", EditorStyles.toolbarButton, GUILayout.Width(80)))
            StageEditorWindow.Open();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStats()
    {
        int total = _stages.Count;
        int validated = _stages.Count(s => s.isValidated && s.solutionCount > 0);
        int invalid = _stages.Count(s => s.isValidated && s.solutionCount == 0);
        int unvalidated = total - validated - invalid;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"총 스테이지: {total}", EditorStyles.miniLabel);
        GUILayout.Label($"✅ 검증됨: {validated}", EditorStyles.miniLabel);
        GUILayout.Label($"❌ 정답없음: {invalid}", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.red } });
        GUILayout.Label($"⚠️ 미검증: {unvalidated}", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.8f, 0.6f, 0f) } });
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStageList()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        foreach (var stage in _stages)
            DrawStageRow(stage);

        EditorGUILayout.EndScrollView();
    }

    private void DrawStageRow(StageData stage)
    {
        // 검증 상태 색상
        Color bgColor;
        string statusIcon;
        if (!stage.isValidated)
        { bgColor = new Color(0.9f, 0.85f, 0.5f, 0.3f); statusIcon = "⚠️"; }
        else if (stage.solutionCount == 0)
        { bgColor = new Color(1f, 0.5f, 0.5f, 0.3f); statusIcon = "❌"; }
        else
        { bgColor = new Color(0.5f, 1f, 0.6f, 0.3f); statusIcon = "✅"; }

        Rect rowRect = EditorGUILayout.BeginHorizontal();
        EditorGUI.DrawRect(rowRect, bgColor);

        // 스테이지 번호
        GUILayout.Label($"{statusIcon} Stage {stage.stageNumber}", GUILayout.Width(100));

        // 보드 크기
        GUILayout.Label($"{stage.boardWidth}×{stage.boardHeight}", GUILayout.Width(50));

        // 조각 수
        GUILayout.Label($"조각 {stage.pieceCount}개 / 풀 {stage.allowedPieces.Count}종", GUILayout.Width(120));

        // 정답 수
        if (stage.isValidated)
            GUILayout.Label($"정답 {stage.solutionCount}개", GUILayout.Width(70));
        else
            GUILayout.Label("미검증", GUILayout.Width(70));

        GUILayout.FlexibleSpace();

        // 편집 버튼
        if (GUILayout.Button("편집", GUILayout.Width(50)))
            StageEditorWindow.OpenWithStage(stage);

        // 빠른 검증 버튼
        if (GUILayout.Button("검증", GUILayout.Width(50)))
        {
            int count = PuzzleSolver.Solve(stage);
            stage.solutionCount = count;
            stage.isValidated = true;
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        // 에셋 선택
        if (GUILayout.Button("◎", GUILayout.Width(30)))
            Selection.activeObject = stage;

        // 삭제
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("삭제", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("스테이지 삭제",
                $"Stage {stage.stageNumber} 을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", "삭제", "취소"))
            {
                string path = AssetDatabase.GetAssetPath(stage);
                AssetDatabase.DeleteAsset(path);
                StageLoaderTool.RefreshStageLoader();
                RefreshStageList();
                Repaint();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }
}
