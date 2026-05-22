using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeSelectionGuard
{
    static PlayModeSelectionGuard()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
        {
            RemoveMissingSelectionTargets();
        }
    }

    static void RemoveMissingSelectionTargets()
    {
        Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            return;
        }

        List<Object> valid = null;
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] != null)
            {
                valid?.Add(selected[i]);
                continue;
            }

            valid ??= CopyValidBeforeMissing(selected, i);
        }

        if (valid != null)
        {
            Selection.objects = valid.ToArray();
        }
    }

    static List<Object> CopyValidBeforeMissing(Object[] selected, int missingIndex)
    {
        List<Object> valid = new List<Object>(selected.Length);
        for (int i = 0; i < missingIndex; i++)
        {
            if (selected[i] != null)
            {
                valid.Add(selected[i]);
            }
        }

        return valid;
    }
}
