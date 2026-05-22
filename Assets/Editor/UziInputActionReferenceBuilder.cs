using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class UziInputActionReferenceBuilder
{
    const string InputAssetPath = "Assets/UziInput.inputactions";
    const string OutputFolder = "Assets/Configs/InputActionReferences";

    static readonly string[] RaceActions =
    {
        "Left",
        "Right",
        "MoveForward",
        "Stop",
    };

    [MenuItem("Tools/UziCart/Create Race Input Action References")]
    public static void CreateRaceInputActionReferences()
    {
        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        if (inputAsset == null)
        {
            Debug.LogError($"Input action asset not found: {InputAssetPath}");
            return;
        }

        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }

        for (int i = 0; i < RaceActions.Length; i++)
        {
            string actionName = RaceActions[i];
            InputAction action = inputAsset.FindAction(actionName);
            if (action == null)
            {
                Debug.LogError($"Action '{actionName}' not found in {InputAssetPath}.");
                continue;
            }

            string referencePath = $"{OutputFolder}/{actionName}.asset";
            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(referencePath);
            if (reference == null)
            {
                reference = InputActionReference.Create(action);
                AssetDatabase.CreateAsset(reference, referencePath);
            }
            else
            {
                reference.Set(action);
                EditorUtility.SetDirty(reference);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Race input action references created in {OutputFolder}.");
    }
}
