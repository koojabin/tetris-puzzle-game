using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 유틸리티.
/// </summary>
public static class SceneLoader
{
    public const string TITLE_SCENE = "TitleScene";
    public const string GAME_SCENE  = "GameScene";

    public static void LoadTitle() => SceneManager.LoadScene(TITLE_SCENE);
    public static void LoadGame()  => SceneManager.LoadScene(GAME_SCENE);
}
