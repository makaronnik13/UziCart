#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WindowsSystemPrefabBuilder
{
    const string BaseFolder = "Assets/Prefabs/WindowsSystem";
    const string ConfigFolder = "Assets/Configs/WindowsSystem";

    [MenuItem("Tools/UziCart/Build Windows System Prefabs")]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog(
                "Rebuild Windows System Prefabs",
                "This will overwrite Windows prefabs and WindowsConfig. Continue?",
                "Rebuild",
                "Cancel"))
        {
            return;
        }

        EnsureFolders();

        WindowId menuId = CreateWindowId("MenuWindowId");
        WindowId exitId = CreateWindowId("ExitConfirmationPopupId");
        WindowId settingsId = CreateWindowId("SettingsWindowId");
        WindowId carId = CreateWindowId("CarSelectionWindowId");
        WindowId trackId = CreateWindowId("TrackSelectionWindowId");

        SavePrefab(BuildMenu(menuId, carId, settingsId, exitId), "MenuWindow.prefab");
        SavePrefab(BuildExitPopup(exitId), "ExitConfirmationPopup.prefab");
        SavePrefab(BuildSettings(settingsId), "SettingsWindow.prefab");
        SavePrefab(BuildCarSelection(carId), "CarSelectionWindow.prefab");
        SavePrefab(BuildTrackSelection(trackId), "TrackSelectionWindow.prefab");

        WindowsConfig windowsConfig = CreateWindowsConfig();
        windowsConfig.menuWindowId = menuId;
        windowsConfig.exitConfirmationPopupId = exitId;
        windowsConfig.settingsWindowId = settingsId;
        windowsConfig.carSelectionWindowId = carId;
        windowsConfig.trackSelectionWindowId = trackId;
        windowsConfig.popupWindowIds.Add(exitId);
        EditorUtility.SetDirty(windowsConfig);

        AssignWindowsConfigToGlobalSettings(windowsConfig);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Windows System prefabs built in " + BaseFolder);
    }

    static GameObject BuildMenu(WindowId id, WindowId carId, WindowId settingsId, WindowId exitId)
    {
        GameObject root = CreateWindowRoot<MainMenuWindow>("MenuWindow", id);
        RectTransform panel = CreatePanel(root.transform, "Panel", new Vector2(520f, 420f));
        AddText(panel, "Title", "UziCart", 42, new Vector2(0f, 145f), new Vector2(440f, 70f));
        Button newGame = AddButton(panel, "NewGameButton", "New Game", new Vector2(0f, 55f), new Vector2(320f, 64f));
        Button settings = AddButton(panel, "SettingsButton", "Settings", new Vector2(0f, -30f), new Vector2(320f, 64f));
        Button exit = AddButton(panel, "ExitButton", "Exit", new Vector2(0f, -115f), new Vector2(320f, 64f));
        SetObject(root.GetComponent<MainMenuWindow>(), "_newGameButton", newGame);
        SetObject(root.GetComponent<MainMenuWindow>(), "_settingsButton", settings);
        SetObject(root.GetComponent<MainMenuWindow>(), "_exitButton", exit);
        return root;
    }

    static GameObject BuildExitPopup(WindowId id)
    {
        GameObject root = CreateWindowRoot<ExitConfirmationPopup>("ExitConfirmationPopup", id);
        RectTransform panel = CreatePanel(root.transform, "Panel", new Vector2(520f, 260f));
        AddText(panel, "Title", "Exit game?", 34, new Vector2(0f, 60f), new Vector2(420f, 70f));
        Button yes = AddButton(panel, "ConfirmButton", "Exit", new Vector2(-105f, -55f), new Vector2(180f, 58f));
        Button no = AddButton(panel, "CancelButton", "Cancel", new Vector2(105f, -55f), new Vector2(180f, 58f));
        SetObject(root.GetComponent<ExitConfirmationPopup>(), "_confirmButton", yes);
        SetObject(root.GetComponent<ExitConfirmationPopup>(), "_cancelButton", no);
        return root;
    }

    static GameObject BuildSettings(WindowId id)
    {
        GameObject root = CreateWindowRoot<SettingsWindow>("SettingsWindow", id);
        RectTransform panel = CreatePanel(root.transform, "Panel", new Vector2(660f, 420f));
        AddText(panel, "Title", "Settings", 36, new Vector2(0f, 150f), new Vector2(520f, 60f));
        VolumeController musicController = AddVolumeController(panel, "MusicVolume", "Music", VolumeController.VolumeChannel.Music, new Vector2(0f, 55f));
        VolumeController sfxController = AddVolumeController(panel, "SfxVolume", "SFX", VolumeController.VolumeChannel.Sfx, new Vector2(0f, -40f));
        Button back = AddButton(panel, "BackButton", "Back", new Vector2(0f, -145f), new Vector2(220f, 56f));
        SettingsWindow window = root.GetComponent<SettingsWindow>();
        SetObjectArray(window, "_volumeControllers", musicController, sfxController);
        SetObject(window, "_backButton", back);
        return root;
    }

    static GameObject BuildCarSelection(WindowId id)
    {
        GameObject root = CreateWindowRoot<CarSelectionWindow>("CarSelectionWindow", id);
        RectTransform panel = CreatePanel(root.transform, "Panel", new Vector2(1040f, 680f));
        AddText(panel, "Title", "Select Car", 34, new Vector2(0f, 292f), new Vector2(500f, 52f));
        Transform previewRoot = new GameObject("PreviewRoot").transform;
        previewRoot.SetParent(panel, false);
        previewRoot.localPosition = new Vector3(0f, 60f, 0f);
        Button previous = AddButton(panel, "PreviousButton", "<", new Vector2(-420f, 80f), new Vector2(72f, 72f));
        Button next = AddButton(panel, "NextButton", ">", new Vector2(420f, 80f), new Vector2(72f, 72f));
        Button back = AddButton(panel, "BackButton", "Back", new Vector2(-430f, 292f), new Vector2(120f, 46f));
        Button confirm = AddButton(panel, "ConfirmButton", "Confirm", new Vector2(390f, -292f), new Vector2(180f, 54f));
        Text carName = AddText(panel, "CarName", "Car", 28, new Vector2(0f, -86f), new Vector2(420f, 44f));
        Slider speed = AddSlider(panel, "SpeedSlider", "Speed", new Vector2(0f, -145f));
        Slider handling = AddSlider(panel, "HandlingSlider", "Handling", new Vector2(0f, -200f));
        Slider lethality = AddSlider(panel, "LethalitySlider", "Lethality", new Vector2(0f, -255f));
        CharacterStatsPanel statsPanel = panel.gameObject.AddComponent<CharacterStatsPanel>();
        SetObject(statsPanel, "_legacyNameText", carName);
        SetObject(statsPanel, "_speedSlider", speed);
        SetObject(statsPanel, "_handlingSlider", handling);
        SetObject(statsPanel, "_lethalitySlider", lethality);
        RectTransform grid = CreateRect(panel, "CarGrid", new Vector2(0f, -330f), new Vector2(720f, 96f));
        GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(120f, 86f);
        gridLayout.spacing = new Vector2(12f, 0f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        gridLayout.constraintCount = 1;
        Button template = AddButton(grid, "CarButtonTemplate", "Car", Vector2.zero, new Vector2(120f, 86f));
        CharacterButton characterButton = template.gameObject.AddComponent<CharacterButton>();
        CarSelectionWindow window = root.GetComponent<CarSelectionWindow>();
        SetObject(window, "_previewRoot", previewRoot);
        SetObject(window, "_previousButton", previous);
        SetObject(window, "_nextButton", next);
        SetObject(window, "_backButton", back);
        SetObject(window, "_confirmButton", confirm);
        SetObject(window, "_gridRoot", grid);
        SetObject(window, "_characterButtonTemplate", characterButton);
        SetObject(window, "_statsPanel", statsPanel);
        return root;
    }

    static GameObject BuildTrackSelection(WindowId id)
    {
        GameObject root = CreateWindowRoot<TrackSelectionWindow>("TrackSelectionWindow", id);
        RectTransform panel = CreatePanel(root.transform, "Panel", new Vector2(1040f, 680f));
        AddText(panel, "Title", "Select Track", 34, new Vector2(0f, 292f), new Vector2(500f, 52f));
        Button back = AddButton(panel, "BackButton", "Back", new Vector2(-430f, 292f), new Vector2(120f, 46f));
        Button confirm = AddButton(panel, "ConfirmButton", "Confirm", new Vector2(390f, -292f), new Vector2(180f, 54f));
        RectTransform grid = CreateRect(panel, "TrackGrid", new Vector2(0f, -10f), new Vector2(840f, 440f));
        GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(260f, 190f);
        gridLayout.spacing = new Vector2(24f, 24f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        Button template = AddButton(grid, "TrackButtonTemplate", "Track", Vector2.zero, new Vector2(260f, 190f));
        TrackSelectionButton trackButton = template.gameObject.AddComponent<TrackSelectionButton>();
        TrackSelectionWindow window = root.GetComponent<TrackSelectionWindow>();
        SetObject(window, "_gridRoot", grid);
        SetObject(window, "_trackButtonTemplate", trackButton);
        SetObject(window, "_backButton", back);
        SetObject(window, "_confirmButton", confirm);
        return root;
    }

    static GameObject CreateWindowRoot<T>(string name, WindowId id) where T : BaseWindow
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(T));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        root.GetComponent<Image>();
        T window = root.GetComponent<T>();
        SetObject(window, "_windowId", id);
        SetObject(window, "_canvasGroup", root.GetComponent<CanvasGroup>());
        return root;
    }

    static RectTransform CreatePanel(Transform parent, string name, Vector2 size)
    {
        RectTransform panel = CreateRect(parent, name, Vector2.zero, size);
        Image image = panel.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.09f, 0.1f, 0.94f);
        return panel;
    }

    static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    static Button AddButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(parent, name, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.92f, 0.92f, 0.9f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        AddText(rect, "Text", label, 24, Vector2.zero, size);
        return button;
    }

    static Text AddText(Transform parent, string name, string value, int size, Vector2 position, Vector2 rectSize)
    {
        RectTransform rect = CreateRect(parent, name, position, rectSize);
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    static Slider AddSlider(Transform parent, string name, string label, Vector2 position)
    {
        AddText(parent, name + "Label", label, 22, position + new Vector2(-250f, 0f), new Vector2(160f, 36f));
        return AddBareSlider(parent, name, position + new Vector2(70f, 0f));
    }

    static VolumeController AddVolumeController(Transform parent, string name, string label, VolumeController.VolumeChannel channel, Vector2 position)
    {
        RectTransform row = CreateRect(parent, name, position, new Vector2(590f, 64f));
        AddText(row, "Label", label, 22, new Vector2(-235f, 0f), new Vector2(130f, 36f));
        Toggle toggle = AddToggle(row, "Toggle", new Vector2(-150f, 0f));
        Slider slider = AddBareSlider(row, "Slider", new Vector2(55f, 0f));
        TextMeshProUGUI valueText = AddTmpText(row, "Value", "100%", 22, new Vector2(265f, 0f), new Vector2(90f, 36f));
        VolumeController controller = row.gameObject.AddComponent<VolumeController>();
        SetInt(controller, "_channel", (int)channel);
        SetObject(controller, "_slider", slider);
        SetObject(controller, "_toggle", toggle);
        SetObject(controller, "_valueText", valueText);
        return controller;
    }

    static Slider AddBareSlider(Transform parent, string name, Vector2 position)
    {
        RectTransform root = CreateRect(parent, name, position, new Vector2(360f, 24f));
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        RectTransform background = CreateRect(root, "Background", Vector2.zero, new Vector2(360f, 14f));
        background.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.24f, 1f);
        RectTransform fill = CreateRect(background, "Fill", Vector2.zero, new Vector2(360f, 14f));
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = new Color(1f, 0.78f, 0.2f, 1f);
        RectTransform handle = CreateRect(root, "Handle", Vector2.zero, new Vector2(26f, 32f));
        handle.gameObject.AddComponent<Image>().color = Color.white;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    static TextMeshProUGUI AddTmpText(Transform parent, string name, string value, int size, Vector2 position, Vector2 rectSize)
    {
        RectTransform rect = CreateRect(parent, name, position, rectSize);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    static Toggle AddToggle(Transform parent, string name, Vector2 position)
    {
        RectTransform root = CreateRect(parent, name, position, new Vector2(34f, 34f));
        Toggle toggle = root.gameObject.AddComponent<Toggle>();
        Image background = root.gameObject.AddComponent<Image>();
        background.color = Color.white;
        RectTransform check = CreateRect(root, "Checkmark", Vector2.zero, new Vector2(22f, 22f));
        Image checkImage = check.gameObject.AddComponent<Image>();
        checkImage.color = new Color(1f, 0.78f, 0.2f, 1f);
        toggle.targetGraphic = background;
        toggle.graphic = checkImage;
        toggle.isOn = true;
        return toggle;
    }

    static void SetObject(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetObjectArray(Object target, string propertyName, params Object[] values)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.isArray)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetInt(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static WindowId CreateWindowId(string name)
    {
        string path = $"{ConfigFolder}/{name}.asset";
        WindowId id = AssetDatabase.LoadAssetAtPath<WindowId>(path);
        if (id == null)
        {
            id = ScriptableObject.CreateInstance<WindowId>();
            AssetDatabase.CreateAsset(id, path);
        }

        return id;
    }

    static WindowsConfig CreateWindowsConfig()
    {
        string path = $"{ConfigFolder}/WindowsConfig.asset";
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        WindowsConfig config = ScriptableObject.CreateInstance<WindowsConfig>();
        AssetDatabase.CreateAsset(config, path);
        return config;
    }

    static GameObject SavePrefab(GameObject source, string fileName)
    {
        string path = $"{BaseFolder}/{fileName}";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        Object.DestroyImmediate(source);
        return prefab;
    }

    static void AssignWindowsConfigToGlobalSettings(WindowsConfig config)
    {
        string[] guids = AssetDatabase.FindAssets("t:GlobalSettings");
        for (int i = 0; i < guids.Length; i++)
        {
            GlobalSettings settings = AssetDatabase.LoadAssetAtPath<GlobalSettings>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (settings == null)
            {
                continue;
            }

            settings.windowsConfig = config;
            EditorUtility.SetDirty(settings);
        }
    }

    static void EnsureFolders()
    {
        Directory.CreateDirectory(BaseFolder);
        Directory.CreateDirectory(ConfigFolder);
    }
}
#endif
