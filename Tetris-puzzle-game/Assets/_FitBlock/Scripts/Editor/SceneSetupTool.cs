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

        // ── Chess Studio 스프라이트 로드 ──────────────────────
        string chessBase = "Assets/Chess Studio/Block Puzzle GUI Pack/png";
        ConfigureSlicedSprite($"{chessBase}/popup/Btn.png", new Vector4(28, 28, 28, 28));
        ConfigureSlicedSprite($"{chessBase}/popup/Btn.w.png", new Vector4(28, 28, 28, 28));
        ConfigureSlicedSprite($"{chessBase}/popup/YellowBtn.png", new Vector4(28, 28, 28, 28));
        ConfigureSlicedSprite($"{chessBase}/Game/CardSlotBg.png", new Vector4(28, 28, 28, 28));
        ConfigureSlicedSprite($"{chessBase}/popup/bg.png", new Vector4(40, 40, 40, 40));

        var greenBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/Btn.w.png");
        var yellowBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/YellowBtn.png");
        var grayBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Game/CardSlotBg.png");
        var checkMarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/CheckMark.png");
        var backSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Hierarchical Challenge/Back.png");
        var xSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/x.png");
        var settingSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Main/Setting.png");
        var tabOnSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/TabOn.png");
        var tabOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/TabOff.png");
        var bgmImgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/BGMImg.png");
        var soundImgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/SoundImg.png");
        var popupBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/bg.png");
        var settingsTitleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/popup/SETTINGS.png");

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
            mainCam.backgroundColor = Color.white;
            mainCam.transform.position = new Vector3(0, 0, -10);
            if (mainCam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
                mainCam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
        }

        // ── 배경 (별도 Canvas, 보드/트레이 뒤에 배치) ────────
        var oldBg = GameObject.Find("GameBackground");
        if (oldBg != null) Object.DestroyImmediate(oldBg);
        var oldBgCanvas = GameObject.Find("BG_Canvas");
        if (oldBgCanvas != null) Object.DestroyImmediate(oldBgCanvas);

        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Main/bg.png");
        {
            var bgCanvasGo = new GameObject("BG_Canvas");
            var bgCanvas = bgCanvasGo.AddComponent<Canvas>();
            bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            bgCanvas.worldCamera = mainCam;
            bgCanvas.planeDistance = 100f;
            bgCanvas.sortingOrder = -10;
            var bgScaler = bgCanvasGo.AddComponent<CanvasScaler>();
            bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bgScaler.referenceResolution = new Vector2(1080, 1920);
            bgScaler.matchWidthOrHeight = 0.5f;

            var bgPanel = new GameObject("GameBackground");
            bgPanel.transform.SetParent(bgCanvasGo.transform, false);
            var bgRt = bgPanel.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgPanel.AddComponent<Image>();
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false;
            }
            else
            {
                bgImg.color = new Color(0.08f, 0.1f, 0.18f);
            }
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

        // 젬 스프라이트 자동 할당
        var gemNames = new[] { "Blue", "Green", "Purple", "Red", "RoseRed", "SkyBlue", "Yellow" };
        var gemProp = serialized.FindProperty("gemSprites");
        gemProp.arraySize = gemNames.Length;
        for (int i = 0; i < gemNames.Length; i++)
        {
            var gem = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Game/{gemNames[i]}.png");
            gemProp.GetArrayElementAtIndex(i).objectReferenceValue = gem;
        }

        serialized.ApplyModifiedProperties();

        // 트레이 배경 스프라이트 자동 할당
        var traySer = new SerializedObject(tray.GetComponent<PieceTray>());
        traySer.FindProperty("trayBgSprite").objectReferenceValue = grayBtnSprite;
        traySer.ApplyModifiedProperties();

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
            new Vector2(0, 160), new Vector2(0, 0));

        // 메뉴 버튼 (왼쪽) — Back 아이콘
        var menuBtn = CreateIconButton(hud.transform, "MenuButton", backSprite);
        SetRectTransform(menuBtn.GetComponent<RectTransform>(),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(60, 0), new Vector2(80, 80));

        // 설정 버튼 (오른쪽) — Setting 아이콘
        var settingsBtnHUD = CreateIconButton(hud.transform, "SettingsButtonHUD", settingSprite);
        SetRectTransform(settingsBtnHUD.GetComponent<RectTransform>(),
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-60, 0), new Vector2(80, 80));

        // 스테이지 표시 (중앙) — Level 이미지 + 숫자 스프라이트
        var levelSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Hierarchical Challenge/Level.png");

        var stageContainer = new GameObject("StageDisplay");
        stageContainer.transform.SetParent(hud.transform, false);
        var stageContRt = stageContainer.AddComponent<RectTransform>();
        stageContRt.anchorMin = new Vector2(0.5f, 0.5f);
        stageContRt.anchorMax = new Vector2(0.5f, 0.5f);
        stageContRt.pivot = new Vector2(0.5f, 0.5f);
        stageContRt.sizeDelta = new Vector2(300, 150);
        stageContRt.anchoredPosition = Vector2.zero;
        var stageLayout = stageContainer.AddComponent<HorizontalLayoutGroup>();
        stageLayout.spacing = 10;
        stageLayout.childAlignment = TextAnchor.MiddleCenter;
        stageLayout.childForceExpandWidth = false;
        stageLayout.childForceExpandHeight = false;
        stageLayout.childControlWidth = true;
        stageLayout.childControlHeight = true;

        // "Level" 이미지
        var levelGo = new GameObject("LevelImage");
        levelGo.transform.SetParent(stageContainer.transform, false);
        var levelImg = levelGo.AddComponent<Image>();
        levelImg.sprite = levelSprite;
        levelImg.preserveAspect = true;
        levelImg.raycastTarget = false;
        var levelLE = levelGo.AddComponent<LayoutElement>();
        levelLE.preferredWidth = 135;
        levelLE.preferredHeight = 143;

        // 숫자 컨테이너
        var digitContainer = new GameObject("DigitContainer");
        digitContainer.transform.SetParent(stageContainer.transform, false);
        var digitContRt = digitContainer.AddComponent<RectTransform>();
        digitContRt.sizeDelta = new Vector2(100, 52);
        var digitLE = digitContainer.AddComponent<LayoutElement>();
        digitLE.preferredWidth = 100;
        digitLE.preferredHeight = 52;

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

        var rotateBtn = CreateButton(bottomBar.transform, "RotateButton", "Rotate", greenBtnSprite);
        var flipBtn = CreateButton(bottomBar.transform, "FlipButton", "Flip", greenBtnSprite);
        var undoBtn = CreateButton(bottomBar.transform, "UndoButton", "Undo", greenBtnSprite);
        var resetBtn = CreateButton(bottomBar.transform, "ResetButton", "Reset", greenBtnSprite);

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
        if (checkMarkSprite != null)
        {
            popupCheckImg.sprite = checkMarkSprite;
            popupCheckImg.color = Color.white;
            popupCheckImg.preserveAspect = true;
        }
        else
        {
            popupCheckImg.color = new Color(0.7f, 0.7f, 0.7f);
        }

        var nextBtn = CreateButton(popupPanel.transform, "NextStageButton", "Next Stage", yellowBtnSprite);
        SetRectTransform(nextBtn.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 260), new Vector2(500, 90));

        var replayBtn = CreateButton(popupPanel.transform, "ReplayButton", "Replay", greenBtnSprite);
        SetRectTransform(replayBtn.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 160), new Vector2(500, 90));

        var selectBtn = CreateButton(popupPanel.transform, "SelectStageButton", "Stage Select", grayBtnSprite);
        SetRectTransform(selectBtn.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 60), new Vector2(500, 90));

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
        var ssBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Main/bg.png");
        if (ssBgSprite != null)
        {
            ssPanelImg.sprite = ssBgSprite;
            ssPanelImg.type = Image.Type.Simple;
            ssPanelImg.preserveAspect = false;
            ssPanelImg.color = Color.white;
        }
        else
        {
            ssPanelImg.color = Color.white;
        }

        // 타이틀 (flag 이미지)
        var flagSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Main/flag.png");
        var ssTitleGo = new GameObject("Title");
        ssTitleGo.transform.SetParent(ssPanel.transform, false);
        var ssTitleRt = ssTitleGo.AddComponent<RectTransform>();
        ssTitleRt.anchorMin = new Vector2(0.5f, 1); ssTitleRt.anchorMax = new Vector2(0.5f, 1);
        ssTitleRt.pivot = new Vector2(0.5f, 1);
        ssTitleRt.sizeDelta = new Vector2(400, 140);
        ssTitleRt.anchoredPosition = new Vector2(0, -15);
        var ssTitleImg = ssTitleGo.AddComponent<Image>();
        ssTitleImg.sprite = flagSprite;
        ssTitleImg.preserveAspect = true;
        ssTitleImg.raycastTarget = false;

        // 설정 버튼 (오른쪽 위) — 타이틀 위에 렌더링
        var settingsBtnSS = CreateIconButton(ssPanel.transform, "SettingsButtonSS", settingSprite);
        SetRectTransform(settingsBtnSS.GetComponent<RectTransform>(),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-30, -30), new Vector2(80, 80));

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
        ssSer.FindProperty("btnUnlockedSprite").objectReferenceValue = greenBtnSprite;
        ssSer.FindProperty("btnLockedSprite").objectReferenceValue = grayBtnSprite;
        ssSer.FindProperty("checkMarkSprite").objectReferenceValue = checkMarkSprite;
        // 숫자 스프라이트 (0~9) 자동 할당
        var digitProp = ssSer.FindProperty("digitSprites");
        digitProp.arraySize = 10;
        for (int d = 0; d < 10; d++)
        {
            var digitSpr = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Hierarchical Challenge/{d}.png");
            digitProp.GetArrayElementAtIndex(d).objectReferenceValue = digitSpr;
        }
        ssSer.ApplyModifiedProperties();

        // ── 설정 패널 ────────────────────────────────────
        var settingsBg = CreateUIPanel(canvasGo.transform, "SettingsPanel",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var settingsBgImg = settingsBg.AddComponent<Image>();
        settingsBgImg.color = new Color(0, 0, 0, 0.6f);

        var settingsBox = CreateUIPanel(settingsBg.transform, "SettingsBox",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(700, 800), Vector2.zero);
        var settingsBoxImg = settingsBox.AddComponent<Image>();
        if (popupBgSprite != null)
        {
            settingsBoxImg.sprite = popupBgSprite;
            settingsBoxImg.type = Image.Type.Sliced;
            settingsBoxImg.color = Color.white;
        }
        else
        {
            settingsBoxImg.color = new Color(1f, 1f, 1f, 0.97f);
        }

        // 설정 타이틀 (이미지)
        var setTitleGo = new GameObject("SettingsTitle");
        setTitleGo.transform.SetParent(settingsBox.transform, false);
        var setTitleRt = setTitleGo.AddComponent<RectTransform>();
        setTitleRt.anchorMin = new Vector2(0.5f, 1);
        setTitleRt.anchorMax = new Vector2(0.5f, 1);
        setTitleRt.pivot = new Vector2(0.5f, 1);
        setTitleRt.anchoredPosition = new Vector2(0.7f, -30.8f);
        setTitleRt.sizeDelta = new Vector2(363.49f, 59.52f);
        var setTitleImg = setTitleGo.AddComponent<Image>();
        if (settingsTitleSprite != null)
        {
            setTitleImg.sprite = settingsTitleSprite;
            setTitleImg.preserveAspect = true;
            setTitleImg.raycastTarget = false;
            setTitleImg.color = Color.white;
        }

        // 닫기 버튼 (X)
        var closeBtn = CreateIconButton(settingsBox.transform, "CloseButton", xSprite);
        SetRectTransform(closeBtn.GetComponent<RectTransform>(),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(70, 70));

        // BGM 토글
        var bgmToggle = CreateToggle(settingsBox.transform, "BGMToggle", "BGM", tabOnSprite, tabOffSprite, bgmImgSprite);
        SetRectTransform(bgmToggle.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -200), new Vector2(500, 70));

        // SOUND 토글
        var sfxToggle = CreateToggle(settingsBox.transform, "SFXToggle", "SOUND", tabOnSprite, tabOffSprite, soundImgSprite);
        SetRectTransform(sfxToggle.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -300), new Vector2(500, 70));


        // 버전 텍스트
        var versionTxt = CreateText(settingsBox.transform, "VersionText", "v1.0",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 30), new Vector2(300, 40), 24);
        versionTxt.color = new Color(1f, 1f, 1f, 0.6f);

        // SettingsPanel 컴포넌트 연결
        var settingsComp = settingsBg.AddComponent<SettingsPanel>();
        var spSer = new SerializedObject(settingsComp);
        spSer.FindProperty("bgmToggle").objectReferenceValue = bgmToggle.GetComponent<Toggle>();
        spSer.FindProperty("sfxToggle").objectReferenceValue = sfxToggle.GetComponent<Toggle>();
        spSer.FindProperty("vibrationToggle").objectReferenceValue = null;
        spSer.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
        spSer.FindProperty("versionText").objectReferenceValue = versionTxt;
        spSer.ApplyModifiedProperties();

        // ── GameUIManager 연결 ────────────────────────────
        var uiSer = new SerializedObject(uiManager);
        uiSer.FindProperty("stageLevelImage").objectReferenceValue = levelImg;
        uiSer.FindProperty("stageDigitContainer").objectReferenceValue = digitContRt;
        // 숫자 스프라이트 (0~9) 할당
        var uiDigitProp = uiSer.FindProperty("digitSprites");
        uiDigitProp.arraySize = 10;
        for (int d = 0; d < 10; d++)
        {
            var digit = AssetDatabase.LoadAssetAtPath<Sprite>($"{chessBase}/Hierarchical Challenge/{d}.png");
            uiDigitProp.GetArrayElementAtIndex(d).objectReferenceValue = digit;
        }
        uiSer.FindProperty("menuButton").objectReferenceValue = menuBtn.GetComponent<Button>();
        uiSer.FindProperty("rotateButton").objectReferenceValue = rotateBtn.GetComponent<Button>();
        uiSer.FindProperty("flipButton").objectReferenceValue = flipBtn.GetComponent<Button>();
        uiSer.FindProperty("undoButton").objectReferenceValue = undoBtn.GetComponent<Button>();
        uiSer.FindProperty("resetButton").objectReferenceValue = resetBtn.GetComponent<Button>();
        uiSer.FindProperty("clearPopup").objectReferenceValue = clearPopup;
        uiSer.FindProperty("gamePanel").objectReferenceValue = gamePanel;
        uiSer.FindProperty("stageSelectPanel").objectReferenceValue = ssComp;
        uiSer.FindProperty("settingsPanel").objectReferenceValue = settingsComp;
        uiSer.FindProperty("settingsButtonHUD").objectReferenceValue = settingsBtnHUD.GetComponent<Button>();
        uiSer.FindProperty("settingsButtonStageSelect").objectReferenceValue = settingsBtnSS.GetComponent<Button>();
        uiSer.ApplyModifiedProperties();

        // 팝업/셀렉트 비활성화
        popupBg.SetActive(false);
        settingsBg.SetActive(false);
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

    private static GameObject CreateButton(Transform parent, string name, string label, Sprite btnSprite = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 80);

        var img = go.AddComponent<Image>();
        if (btnSprite != null)
        {
            img.sprite = btnSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.23f, 0.49f, 0.96f);
        }

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        btn.colors = colors;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(0, 6);
        textRt.offsetMax = new Vector2(0, 6);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(30, 60, 30, 255);

        return go;
    }

    /// <summary>아이콘만 있는 버튼 (텍스트 없음)</summary>
    private static GameObject CreateIconButton(Transform parent, string name, Sprite iconSprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);

        var img = go.AddComponent<Image>();
        img.color = Color.clear; // 배경 투명

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
        btn.colors = colors;

        // 아이콘 이미지
        if (iconSprite != null)
        {
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(8, 8);
            iconRt.offsetMax = new Vector2(-8, -8);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            btn.targetGraphic = iconImg;
        }

        return go;
    }

    /// <summary>아이콘 + 라벨 + TabOn/TabOff 스위치 토글</summary>
    private static GameObject CreateToggle(Transform parent, string name, string label,
        Sprite onSprite, Sprite offSprite, Sprite iconSprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 70);

        var toggle = go.AddComponent<Toggle>();
        toggle.transition = Selectable.Transition.None;

        // 아이콘 (왼쪽)
        float labelLeft = 0;
        if (iconSprite != null)
        {
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(50, 50);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = Color.white;
            labelLeft = 60;
        }

        // 라벨
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0);
        labelRt.anchorMax = new Vector2(0.6f, 1);
        labelRt.offsetMin = new Vector2(labelLeft, 0);
        labelRt.offsetMax = Vector2.zero;
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 36;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.color = Color.white;
        labelTmp.raycastTarget = false;

        // TabOff 배경 (오른쪽)
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(1, 0.5f);
        bgRt.anchorMax = new Vector2(1, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = new Vector2(-60, 0);
        bgRt.sizeDelta = new Vector2(120, 60);
        var bgImg = bgGo.AddComponent<Image>();
        if (offSprite != null)
        {
            bgImg.sprite = offSprite;
            bgImg.preserveAspect = true;
        }
        else
        {
            bgImg.color = new Color(0.8f, 0.8f, 0.8f);
        }

        // TabOn 체크마크 (같은 위치, 회전 없음)
        var checkGo = new GameObject("Checkmark");
        checkGo.transform.SetParent(go.transform, false);
        var checkRt = checkGo.AddComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(1, 0.5f);
        checkRt.anchorMax = new Vector2(1, 0.5f);
        checkRt.pivot = new Vector2(0.5f, 0.5f);
        checkRt.anchoredPosition = new Vector2(-60, 0);
        checkRt.sizeDelta = new Vector2(120, 60);
        var checkImg = checkGo.AddComponent<Image>();
        if (onSprite != null)
        {
            checkImg.sprite = onSprite;
            checkImg.preserveAspect = true;
        }
        else
        {
            checkImg.color = new Color(0.2f, 0.8f, 0.2f);
        }

        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = true;

        return go;
    }

    /// <summary>스프라이트를 9-slice용으로 설정 (에디터 전용)</summary>
    private static void ConfigureSlicedSprite(string path, Vector4 border)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool needsReimport = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            needsReimport = true;
        }

        if (importer.spriteBorder != border)
        {
            importer.spriteBorder = border;
            needsReimport = true;
        }

        if (needsReimport)
            importer.SaveAndReimport();
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
