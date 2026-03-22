using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameScene 기본 구조를 자동으로 세팅하는 에디터 도구.
/// FitBlock > Setup Game Scene 실행.
/// </summary>
public static class SceneSetupTool
{
    [MenuItem("FitBlock/Setup Game Scene")]
    public static void SetupGameScene()
    {
        if (!EditorUtility.DisplayDialog("Game Scene 세팅",
            "현재 씬에 FitBlock 게임 오브젝트들을 생성합니다.\n기존 FitBlock 오브젝트는 삭제 후 재생성됩니다.", "실행", "취소"))
            return;

        // ── 기존 오브젝트 정리 ──────────────────────────────
        var oldNames = new[] { "GameManager", "UI_Canvas" };
        foreach (var name in oldNames)
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        // ── 카메라 ────────────────────────────────────────
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6f;
            mainCam.backgroundColor = new Color(0.95f, 0.97f, 1f);
            mainCam.transform.position = new Vector3(0, 0, -10);
            if (mainCam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
                mainCam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
        }

        // ── GameManager ───────────────────────────────────
        var gameManager = new GameObject("GameManager");
        var sm = gameManager.AddComponent<StageManager>();

        var board = new GameObject("Board");
        board.AddComponent<BoardRenderer>();
        board.transform.SetParent(gameManager.transform);

        var tray = new GameObject("PieceTray");
        tray.AddComponent<PieceTray>();
        tray.transform.SetParent(gameManager.transform);

        var serialized = new SerializedObject(sm);
        serialized.FindProperty("_board").objectReferenceValue = board.GetComponent<BoardRenderer>();
        serialized.FindProperty("_tray").objectReferenceValue = tray.GetComponent<PieceTray>();

        var loaderAsset = AssetDatabase.LoadAssetAtPath<StageLoader>("Assets/_FitBlock/Data/StageLoader.asset");
        if (loaderAsset != null)
            serialized.FindProperty("_stageLoader").objectReferenceValue = loaderAsset;
        serialized.ApplyModifiedProperties();

        // ── UI Canvas ─────────────────────────────────────
        var canvasGo = new GameObject("UI_Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var uiManager = canvasGo.AddComponent<GameUIManager>();

        // ── GamePanel (HUD + 게임 영역) ────────────────────
        var gamePanel = new GameObject("GamePanel");
        gamePanel.transform.SetParent(canvasGo.transform, false);
        var gamePanelRt = gamePanel.AddComponent<RectTransform>();
        gamePanelRt.anchorMin = Vector2.zero;
        gamePanelRt.anchorMax = Vector2.one;
        gamePanelRt.offsetMin = gamePanelRt.offsetMax = Vector2.zero;

        // ── HUD (상단) ────────────────────────────────────
        var hud = CreateUIPanel(gamePanel.transform, "HUD",
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            new Vector2(0, 120), new Vector2(0, 0));

        // 메뉴 버튼 (왼쪽)
        var menuBtn = CreateButton(hud.transform, "MenuButton", "Menu");
        SetRectTransform(menuBtn.GetComponent<RectTransform>(),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(80, 0), new Vector2(140, 80));

        // 스테이지 텍스트 (중앙)
        var stageText = CreateText(hud.transform, "StageNumberText", "STAGE 1",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 0), new Vector2(300, 80), 48);

        // 클리어 체크 아이콘 (오른쪽)
        var checkIconGo = new GameObject("ClearCheckIcon");
        checkIconGo.transform.SetParent(hud.transform, false);
        var checkIconRt = checkIconGo.AddComponent<RectTransform>();
        checkIconRt.anchorMin = new Vector2(1, 0.5f);
        checkIconRt.anchorMax = new Vector2(1, 0.5f);
        checkIconRt.pivot = new Vector2(1, 0.5f);
        checkIconRt.anchoredPosition = new Vector2(-30, 0);
        checkIconRt.sizeDelta = new Vector2(60, 60);
        var clearCheckImg = checkIconGo.AddComponent<Image>();
        clearCheckImg.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);

        // ── 하단 버튼 ─────────────────────────────────────
        var bottomBar = CreateUIPanel(gamePanel.transform, "BottomBar",
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
            new Vector2(0, 140), new Vector2(0, 70));
        var bottomLayout = bottomBar.AddComponent<HorizontalLayoutGroup>();
        bottomLayout.spacing = 30;
        bottomLayout.childAlignment = TextAnchor.MiddleCenter;
        bottomLayout.padding = new RectOffset(20, 20, 0, 0);
        bottomLayout.childForceExpandWidth = false;
        bottomLayout.childForceExpandHeight = false;

        var rotateBtn = CreateButton(bottomBar.transform, "RotateButton", "Rotate");
        var flipBtn = CreateButton(bottomBar.transform, "FlipButton", "Flip");
        var undoBtn = CreateButton(bottomBar.transform, "UndoButton", "Undo");
        var resetBtn = CreateButton(bottomBar.transform, "ResetButton", "Reset");

        // ── 클리어 팝업 ───────────────────────────────────
        var popupBg = CreateUIPanel(canvasGo.transform, "ClearPopup",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var popupBgImg = popupBg.AddComponent<Image>();
        popupBgImg.color = new Color(0, 0, 0, 0.6f);

        var popupPanel = CreateUIPanel(popupBg.transform, "PopupPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(700, 900), Vector2.zero);
        var panelImg = popupPanel.AddComponent<Image>();
        panelImg.color = new Color(1f, 1f, 1f, 0.97f);

        var titleTxt = CreateText(popupPanel.transform, "TitleText", "Clear!",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -80), new Vector2(600, 100), 72);

        var stageTxt = CreateText(popupPanel.transform, "StageText", "Stage 1",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -180), new Vector2(400, 60), 36);

        // 팝업 체크 아이콘
        var popupCheckGo = new GameObject("PopupCheckIcon");
        popupCheckGo.transform.SetParent(popupPanel.transform, false);
        var popupCheckRt = popupCheckGo.AddComponent<RectTransform>();
        popupCheckRt.anchorMin = new Vector2(0.5f, 0.5f);
        popupCheckRt.anchorMax = new Vector2(0.5f, 0.5f);
        popupCheckRt.pivot = new Vector2(0.5f, 0.5f);
        popupCheckRt.anchoredPosition = new Vector2(0, 80);
        popupCheckRt.sizeDelta = new Vector2(100, 100);
        var popupCheckImg = popupCheckGo.AddComponent<Image>();
        popupCheckImg.color = new Color(0.7f, 0.7f, 0.7f);

        var nextBtn = CreateButton(popupPanel.transform, "NextStageButton", "Next Stage");
        SetRectTransform(nextBtn.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 260), new Vector2(500, 90));

        var replayBtn = CreateButton(popupPanel.transform, "ReplayButton", "Replay");
        SetRectTransform(replayBtn.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 160), new Vector2(500, 90));

        var selectBtn = CreateButton(popupPanel.transform, "SelectStageButton", "Stage Select");
        SetRectTransform(selectBtn.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 60), new Vector2(500, 90));
        // Stage Select 버튼은 회색 계열
        selectBtn.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.6f);

        // ClearPopup 컴포넌트 연결
        var clearPopup = popupBg.AddComponent<ClearPopup>();
        var cpSer = new SerializedObject(clearPopup);
        cpSer.FindProperty("titleText").objectReferenceValue = titleTxt;
        cpSer.FindProperty("stageText").objectReferenceValue = stageTxt;
        cpSer.FindProperty("nextStageButton").objectReferenceValue = nextBtn.GetComponent<Button>();
        cpSer.FindProperty("replayButton").objectReferenceValue = replayBtn.GetComponent<Button>();
        cpSer.FindProperty("selectStageButton").objectReferenceValue = selectBtn.GetComponent<Button>();
        cpSer.FindProperty("checkImage").objectReferenceValue = popupCheckImg;
        cpSer.ApplyModifiedProperties();

        // ── StageSelect 패널 ──────────────────────────────
        var ssPanel = CreateUIPanel(canvasGo.transform, "StageSelectPanel",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var ssPanelImg = ssPanel.AddComponent<Image>();
        ssPanelImg.color = new Color(0.12f, 0.15f, 0.25f, 0.97f);

        // 타이틀
        var ssTitleGo = new GameObject("Title");
        ssTitleGo.transform.SetParent(ssPanel.transform, false);
        var ssTitleRt = ssTitleGo.AddComponent<RectTransform>();
        ssTitleRt.anchorMin = new Vector2(0, 1); ssTitleRt.anchorMax = new Vector2(1, 1);
        ssTitleRt.pivot = new Vector2(0.5f, 1);
        ssTitleRt.sizeDelta = new Vector2(0, 140);
        ssTitleRt.anchoredPosition = Vector2.zero;
        var ssTitleTmp = ssTitleGo.AddComponent<TextMeshProUGUI>();
        ssTitleTmp.text = "SELECT STAGE";
        ssTitleTmp.fontSize = 54;
        ssTitleTmp.alignment = TextAlignmentOptions.Center;
        ssTitleTmp.color = Color.white;

        // 스크롤 뷰
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(ssPanel.transform, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0); scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.offsetMin = new Vector2(40, 60);
        scrollRt.offsetMax = new Vector2(-40, -140);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        var scrollImg = scrollGo.AddComponent<Image>();
        scrollImg.color = new Color(0, 0, 0, 0);

        // 뷰포트
        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = viewportRt.offsetMax = Vector2.zero;
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        viewportGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

        // 콘텐츠 (버튼 컨테이너)
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(0, 1);
        contentRt.pivot = new Vector2(0, 1);
        contentRt.sizeDelta = new Vector2(800, 600);
        contentRt.anchoredPosition = new Vector2(40, 0);

        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;

        // StageSelectPanel 컴포넌트
        var ssComp = ssPanel.AddComponent<StageSelectPanel>();
        var ssSer = new SerializedObject(ssComp);
        ssSer.FindProperty("buttonContainer").objectReferenceValue = contentGo.GetComponent<RectTransform>();
        ssSer.FindProperty("scrollRect").objectReferenceValue = scrollRect;
        if (loaderAsset != null)
            ssSer.FindProperty("stageLoader").objectReferenceValue = loaderAsset;
        ssSer.ApplyModifiedProperties();

        // ── GameUIManager 연결 ────────────────────────────
        var uiSer = new SerializedObject(uiManager);
        uiSer.FindProperty("stageNumberText").objectReferenceValue = stageText;
        uiSer.FindProperty("menuButton").objectReferenceValue = menuBtn.GetComponent<Button>();
        uiSer.FindProperty("rotateButton").objectReferenceValue = rotateBtn.GetComponent<Button>();
        uiSer.FindProperty("flipButton").objectReferenceValue = flipBtn.GetComponent<Button>();
        uiSer.FindProperty("undoButton").objectReferenceValue = undoBtn.GetComponent<Button>();
        uiSer.FindProperty("resetButton").objectReferenceValue = resetBtn.GetComponent<Button>();
        uiSer.FindProperty("clearPopup").objectReferenceValue = clearPopup;
        uiSer.FindProperty("gamePanel").objectReferenceValue = gamePanel;
        uiSer.FindProperty("stageSelectPanel").objectReferenceValue = ssComp;
        uiSer.FindProperty("clearCheckIcon").objectReferenceValue = clearCheckImg;
        uiSer.ApplyModifiedProperties();

        // 팝업/셀렉트 비활성화
        popupBg.SetActive(false);
        ssPanel.SetActive(true);

        // ── EventSystem ───────────────────────────────────
        var existingES = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (existingES != null)
        {
            var oldModule = existingES.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null) Object.DestroyImmediate(oldModule);
            AddInputModule(existingES.gameObject);
        }
        else
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            AddInputModule(esGo);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FitBlock] Game Scene 세팅 완료!");
        Selection.activeGameObject = gameManager;
    }

    // ── UI 헬퍼 ──────────────────────────────────────────

    private static GameObject CreateUIPanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.15f, 0.2f, 0.35f);
        return tmp;
    }

    private static GameObject CreateButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 80);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.23f, 0.49f, 0.96f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.6f, 1f);
        colors.pressedColor = new Color(0.15f, 0.35f, 0.75f);
        btn.colors = colors;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    private static void SetRectTransform(RectTransform rt,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    private static void AddInputModule(GameObject target)
    {
        System.Type moduleType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            moduleType = assembly.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (moduleType != null) break;
        }

        if (moduleType != null)
        {
            target.AddComponent(moduleType);
            Debug.Log("[FitBlock] InputSystemUIInputModule 추가 완료");
        }
        else
        {
            target.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.LogWarning("[FitBlock] StandaloneInputModule 사용 (Input System 패키지 없음)");
        }
    }
}
