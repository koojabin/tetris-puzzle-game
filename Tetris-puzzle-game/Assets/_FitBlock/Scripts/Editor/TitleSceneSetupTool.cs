using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TitleScene 기본 구조를 자동으로 세팅하는 에디터 도구.
/// FitBlock > Setup Title Scene 실행.
/// </summary>
public static class TitleSceneSetupTool
{
    [MenuItem("FitBlock/Setup Title Scene")]
    public static void SetupTitleScene()
    {
        if (!EditorUtility.DisplayDialog("Title Scene 세팅",
            "현재 씬에 FitBlock 타이틀 오브젝트들을 생성합니다.", "실행", "취소"))
            return;

        // ── 카메라 ────────────────────────────────────────
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6f;
            mainCam.backgroundColor = new Color(0.08f, 0.1f, 0.18f);
            mainCam.transform.position = new Vector3(0, 0, -10);
        }

        // ── UI Canvas ─────────────────────────────────────
        var canvasGo = new GameObject("UI_Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── 배경 ──────────────────────────────────────────
        var bg = CreatePanel(canvasGo.transform, "Background",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.1f, 0.18f);

        // ── 타이틀 텍스트 ─────────────────────────────────
        var titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(canvasGo.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(900, 200);
        titleRt.anchoredPosition = new Vector2(0, 400);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "FIT BLOCK";
        titleTmp.fontSize = 96;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.fontStyle = FontStyles.Bold;

        // ── 서브타이틀 ────────────────────────────────────
        var subGo = new GameObject("SubtitleText");
        subGo.transform.SetParent(canvasGo.transform, false);
        var subRt = subGo.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.5f);
        subRt.anchorMax = new Vector2(0.5f, 0.5f);
        subRt.pivot = new Vector2(0.5f, 0.5f);
        subRt.sizeDelta = new Vector2(700, 80);
        subRt.anchoredPosition = new Vector2(0, 300);
        var subTmp = subGo.AddComponent<TextMeshProUGUI>();
        subTmp.text = "Puzzle Game";
        subTmp.fontSize = 42;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.7f, 0.8f, 1f);

        // ── 버튼 컨테이너 ─────────────────────────────────
        var btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(canvasGo.transform, false);
        var btnContRt = btnContainer.AddComponent<RectTransform>();
        btnContRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnContRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnContRt.pivot = new Vector2(0.5f, 0.5f);
        btnContRt.sizeDelta = new Vector2(500, 300);
        btnContRt.anchoredPosition = new Vector2(0, -100);
        var vLayout = btnContainer.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 30;
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        var startBtn  = CreateButton(btnContainer.transform, "StartButton",  "Game Start", new Color(0.23f, 0.6f, 0.35f), 120);
        var settingsBtn = CreateButton(btnContainer.transform, "SettingsButton", "Settings",  new Color(0.35f, 0.35f, 0.5f),  100);

        // ── 버전 텍스트 ───────────────────────────────────
        var verGo = new GameObject("VersionText");
        verGo.transform.SetParent(canvasGo.transform, false);
        var verRt = verGo.AddComponent<RectTransform>();
        verRt.anchorMin = new Vector2(1, 0);
        verRt.anchorMax = new Vector2(1, 0);
        verRt.pivot = new Vector2(1, 0);
        verRt.sizeDelta = new Vector2(200, 50);
        verRt.anchoredPosition = new Vector2(-20, 20);
        var verTmp = verGo.AddComponent<TextMeshProUGUI>();
        verTmp.text = "v0.1";
        verTmp.fontSize = 24;
        verTmp.alignment = TextAlignmentOptions.Right;
        verTmp.color = new Color(1, 1, 1, 0.4f);

        // ── 설정 패널 ─────────────────────────────────────
        var settingsBg = CreatePanel(canvasGo.transform, "SettingsPanel",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var settingsBgImg = settingsBg.AddComponent<Image>();
        settingsBgImg.color = new Color(0, 0, 0, 0.7f);

        var settingsBox = CreatePanel(settingsBg.transform, "SettingsBox",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(700, 700), Vector2.zero);
        var settingsBoxImg = settingsBox.AddComponent<Image>();
        settingsBoxImg.color = new Color(0.15f, 0.18f, 0.28f);

        // 설정 타이틀
        var stTitleGo = new GameObject("SettingsTitle");
        stTitleGo.transform.SetParent(settingsBox.transform, false);
        var stTitleRt = stTitleGo.AddComponent<RectTransform>();
        stTitleRt.anchorMin = new Vector2(0, 1); stTitleRt.anchorMax = new Vector2(1, 1);
        stTitleRt.pivot = new Vector2(0.5f, 1);
        stTitleRt.sizeDelta = new Vector2(0, 100);
        stTitleRt.anchoredPosition = Vector2.zero;
        var stTitleTmp = stTitleGo.AddComponent<TextMeshProUGUI>();
        stTitleTmp.text = "Settings";
        stTitleTmp.fontSize = 52;
        stTitleTmp.alignment = TextAlignmentOptions.Center;
        stTitleTmp.color = Color.white;

        // 토글 행들
        var bgmToggle   = CreateToggleRow(settingsBox.transform, "BGMRow",   "BGM",       new Vector2(0, -120));
        var sfxToggle   = CreateToggleRow(settingsBox.transform, "SFXRow",   "SFX",       new Vector2(0, -230));
        var vibToggle   = CreateToggleRow(settingsBox.transform, "VibRow",   "Vibration", new Vector2(0, -340));

        // 닫기 버튼
        var closeBtn = CreateButton(settingsBox.transform, "CloseButton", "Close", new Color(0.6f, 0.2f, 0.2f), 80);
        var closeBtnRt = closeBtn.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(0.5f, 0); closeBtnRt.anchorMax = new Vector2(0.5f, 0);
        closeBtnRt.pivot = new Vector2(0.5f, 0);
        closeBtnRt.sizeDelta = new Vector2(300, 80);
        closeBtnRt.anchoredPosition = new Vector2(0, 40);

        // SettingsPanel 컴포넌트 연결
        var settingsComp = settingsBg.AddComponent<SettingsPanel>();
        var spSer = new SerializedObject(settingsComp);
        spSer.FindProperty("bgmToggle").objectReferenceValue       = bgmToggle;
        spSer.FindProperty("sfxToggle").objectReferenceValue       = sfxToggle;
        spSer.FindProperty("vibrationToggle").objectReferenceValue = vibToggle;
        spSer.FindProperty("closeButton").objectReferenceValue     = closeBtn.GetComponent<Button>();
        spSer.ApplyModifiedProperties();

        settingsBg.SetActive(false);

        // ── TitleUI 컴포넌트 연결 ─────────────────────────
        var titleUI = canvasGo.AddComponent<TitleUI>();
        var tuSer = new SerializedObject(titleUI);
        tuSer.FindProperty("startButton").objectReferenceValue    = startBtn.GetComponent<Button>();
        tuSer.FindProperty("settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
        tuSer.FindProperty("settingsPanel").objectReferenceValue  = settingsComp;
        tuSer.ApplyModifiedProperties();

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

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[FitBlock] Title Scene 세팅 완료!");
        Selection.activeGameObject = canvasGo;
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
            target.AddComponent(moduleType);
        else
            target.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    // ── 헬퍼 ─────────────────────────────────────────────

    private static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.sizeDelta = sizeDelta; rt.anchoredPosition = anchoredPos;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Color color, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, height);

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        btn.colors = colors;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    private static Toggle CreateToggleRow(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        var rowGo = new GameObject(name);
        rowGo.transform.SetParent(parent, false);
        var rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 1); rowRt.anchorMax = new Vector2(0.5f, 1);
        rowRt.pivot = new Vector2(0.5f, 1);
        rowRt.sizeDelta = new Vector2(560, 90);
        rowRt.anchoredPosition = anchoredPos;

        // 라벨
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rowGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = new Vector2(0.7f, 1);
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 38;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.color = Color.white;

        // 토글
        var toggleGo = new GameObject("Toggle");
        toggleGo.transform.SetParent(rowGo.transform, false);
        var toggleRt = toggleGo.AddComponent<RectTransform>();
        toggleRt.anchorMin = new Vector2(0.7f, 0.5f); toggleRt.anchorMax = new Vector2(1f, 0.5f);
        toggleRt.pivot = new Vector2(1, 0.5f);
        toggleRt.sizeDelta = new Vector2(0, 60);
        toggleRt.anchoredPosition = Vector2.zero;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(toggleGo.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.4f);

        var checkGo = new GameObject("Checkmark");
        checkGo.transform.SetParent(bgGo.transform, false);
        var checkRt = checkGo.AddComponent<RectTransform>();
        checkRt.anchorMin = Vector2.zero; checkRt.anchorMax = Vector2.one;
        checkRt.sizeDelta = Vector2.zero;
        var checkImg = checkGo.AddComponent<Image>();
        checkImg.color = new Color(0.2f, 0.8f, 0.4f);

        var toggle = toggleGo.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = true;

        return toggle;
    }
}
