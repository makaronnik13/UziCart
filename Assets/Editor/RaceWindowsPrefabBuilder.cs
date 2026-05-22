#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RaceWindowsPrefabBuilder
{
    const string PrefabFolder = "Assets/Prefabs/WindowsSystem";
    const string ConfigFolder = "Assets/Configs/WindowsSystem";

    static RaceWindowsPrefabBuilder()
    {
        EditorApplication.delayCall += EnsureRaceWindows;
    }

    [MenuItem("Tools/UziCart/Build Race Windows")]
    public static void EnsureRaceWindows()
    {
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(ConfigFolder);

        WindowId pauseId = CreateWindowId("PauseWindowId");
        WindowId exitToMenuId = CreateWindowId("ExitToMenuConfirmationPopupId");
        WindowId finishId = CreateWindowId("FinishWindowId");

        GameObject pausePrefab = SavePrefabIfMissing(BuildPauseScreen(pauseId, exitToMenuId), "PauseScreen.prefab");
        GameObject exitToMenuPrefab = SavePrefabIfMissing(BuildExitToMenuPopup(exitToMenuId), "ExitToMenuConfirmationPopup.prefab");
        GameObject finishPrefab = AssignFinishWindowId(finishId);
        SetObject(pauseId, "prefab", pausePrefab);
        SetObject(exitToMenuId, "prefab", exitToMenuPrefab);
        SetObject(finishId, "prefab", finishPrefab);
        UpdateWindowsConfigs(pauseId, exitToMenuId, finishId);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static GameObject BuildPauseScreen(WindowId pauseId, WindowId exitToMenuId)
    {
        GameObject root = CreateWindowRoot<PauseScreen>("PauseScreen", pauseId, 150);
        RectTransform panel = CreatePanel(root.transform, "Panel", new Vector2(680f, 480f));
        AddText(panel, "Title", "Pause", 38, new Vector2(0f, 180f), new Vector2(520f, 60f));

        VolumeController music = AddVolumeController(panel, "MusicVolume", "Music", VolumeController.VolumeChannel.Music, new Vector2(0f, 80f));
        VolumeController sfx = AddVolumeController(panel, "SfxVolume", "SFX", VolumeController.VolumeChannel.Sfx, new Vector2(0f, -20f));
        Button resume = AddButton(panel, "ResumeButton", "Resume", new Vector2(-130f, -155f), new Vector2(220f, 58f));
        Button exit = AddButton(panel, "ExitToMenuButton", "Exit to menu", new Vector2(130f, -155f), new Vector2(220f, 58f));

        PauseScreen screen = root.GetComponent<PauseScreen>();
        SetObjectArray(screen, "_volumeControllers", music, sfx);
        SetObject(screen, "_resumeButton", resume);
        SetObject(screen, "_exitToMenuButton", exit);
        return root;
    }

    static GameObject BuildExitToMenuPopup(WindowId id)
    {
        GameObject root = CreateWindowRoot<ExitToMenuConfirmationPopup>("ExitToMenuConfirmationPopup", id, 250);
        RectTransform panel = CreatePanel(root.transform, "Panel", new Vector2(560f, 280f));
        AddText(panel, "Title", "Exit to menu?", 34, new Vector2(0f, 70f), new Vector2(460f, 70f));
        Button yes = AddButton(panel, "ConfirmButton", "Exit", new Vector2(-115f, -65f), new Vector2(190f, 58f));
        Button no = AddButton(panel, "CancelButton", "Cancel", new Vector2(115f, -65f), new Vector2(190f, 58f));

        ExitToMenuConfirmationPopup popup = root.GetComponent<ExitToMenuConfirmationPopup>();
        SetObject(popup, "_confirmButton", yes);
        SetObject(popup, "_cancelButton", no);
        return root;
    }

    static GameObject AssignFinishWindowId(WindowId finishId)
    {
        string path = $"{PrefabFolder}/FinishRaceWindow.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            return null;
        }

        FinishScreen finishScreen = prefab.GetComponentInChildren<FinishScreen>(true);
        if (finishScreen == null)
        {
            return prefab;
        }

        SetObject(finishScreen, "_windowId", finishId);
        EditorUtility.SetDirty(finishScreen);
        return prefab;
    }

    static GameObject CreateWindowRoot<T>(string name, WindowId id, int sortingOrder) where T : BaseWindow
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(T));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

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
        TextMeshProUGUI text = AddText(rect, "Text", label, 24, Vector2.zero, size);
        text.color = new Color(0.08f, 0.09f, 0.1f, 1f);
        return button;
    }

    static TextMeshProUGUI AddText(Transform parent, string name, string value, int size, Vector2 position, Vector2 rectSize)
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

    static VolumeController AddVolumeController(Transform parent, string name, string label, VolumeController.VolumeChannel channel, Vector2 position)
    {
        RectTransform row = CreateRect(parent, name, position, new Vector2(590f, 64f));
        AddText(row, "Label", label, 22, new Vector2(-235f, 0f), new Vector2(130f, 36f));
        Toggle toggle = AddToggle(row, "Toggle", new Vector2(-150f, 0f));
        Slider slider = AddBareSlider(row, "Slider", new Vector2(55f, 0f));
        TextMeshProUGUI valueText = AddText(row, "Value", "100%", 22, new Vector2(265f, 0f), new Vector2(90f, 36f));
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

    static void UpdateWindowsConfigs(WindowId pauseId, WindowId exitToMenuId, WindowId finishId)
    {
        string[] guids = AssetDatabase.FindAssets("t:WindowsConfig");
        for (int i = 0; i < guids.Length; i++)
        {
            WindowsConfig config = AssetDatabase.LoadAssetAtPath<WindowsConfig>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (config == null)
            {
                continue;
            }

            config.pauseWindowId = pauseId;
            config.exitToMenuConfirmationPopupId = exitToMenuId;
            config.finishWindowId = finishId;
            config.popupWindowIds ??= new System.Collections.Generic.List<WindowId>();
            if (!config.popupWindowIds.Contains(exitToMenuId))
            {
                config.popupWindowIds.Add(exitToMenuId);
            }

            EditorUtility.SetDirty(config);
        }
    }

    static GameObject SavePrefabIfMissing(GameObject source, string fileName)
    {
        string path = $"{PrefabFolder}/{fileName}";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!File.Exists(path))
        {
            prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        }

        Object.DestroyImmediate(source);
        return prefab;
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
}
#endif
