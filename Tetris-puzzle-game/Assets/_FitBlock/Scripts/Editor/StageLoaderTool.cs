using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// StageLoader 에셋을 생성하고 Data/Stages 폴더의 모든 스테이지를 자동 등록.
/// FitBlock > Refresh Stage Loader 실행.
/// </summary>
public static class StageLoaderTool
{
    private const string LOADER_PATH = "Assets/_FitBlock/Data/StageLoader.asset";
    private const string STAGES_PATH = "Assets/_FitBlock/Data/Stages";

    [MenuItem("FitBlock/Refresh Stage Loader")]
    public static void RefreshStageLoaderMenu()
    {
        int count = RefreshStageLoader();
        EditorUtility.DisplayDialog("Stage Loader 갱신",
            $"{count}개 스테이지가 등록됐습니다.", "확인");
    }

    /// <summary>StageLoader 자동 갱신. 반환값: 등록된 스테이지 수.</summary>
    public static int RefreshStageLoader()
    {
        var loader = AssetDatabase.LoadAssetAtPath<StageLoader>(LOADER_PATH);
        if (loader == null)
        {
            loader = ScriptableObject.CreateInstance<StageLoader>();
            AssetDatabase.CreateAsset(loader, LOADER_PATH);
        }

        var guids = AssetDatabase.FindAssets("t:StageData", new[] { STAGES_PATH });
        var stages = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null)
            .OrderBy(s => s.stageNumber)
            .ToList();

        var serialized = new SerializedObject(loader);
        var stagesProp = serialized.FindProperty("stages");
        stagesProp.arraySize = stages.Count;
        for (int i = 0; i < stages.Count; i++)
            stagesProp.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(loader);
        AssetDatabase.SaveAssets();

        Debug.Log($"[FitBlock] StageLoader 갱신 완료: {stages.Count}개 스테이지 등록");
        return stages.Count;
    }
}
