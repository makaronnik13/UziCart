using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

public abstract class ConfigBase : ScriptableObject
{
#if ODIN_INSPECTOR
    [FoldoutGroup("General")]
    [PropertyOrder(-1000)]
    [ShowIf(nameof(ShowPrefabInInspector))]
#endif
    [SerializeField]
    GameObject prefab;
#if ODIN_INSPECTOR
    [FoldoutGroup("General")]
    [PropertyOrder(-999)]
    [ShowIf(nameof(ShowDisplayNameInInspector))]
#endif
    [SerializeField]
    LocalizedString displayName;
#if ODIN_INSPECTOR
    [FoldoutGroup("General")]
    [PropertyOrder(-998)]
    [ShowIf(nameof(ShowDescriptionInInspector))]
#endif
    [SerializeField]
    LocalizedString description;
#if ODIN_INSPECTOR
    [FoldoutGroup("General")]
    [PropertyOrder(-997)]
    [ShowIf(nameof(ShowIconInInspector))]
#endif
    [SerializeField]
    Sprite icon;
#if ODIN_INSPECTOR
    [FoldoutGroup("General")]
    [PropertyOrder(-996)]
    [ShowIf(nameof(ShowUseInRepositoryInInspector))]
#endif
    [SerializeField]
    bool useInRepository = true;
#if ODIN_INSPECTOR
    [FoldoutGroup("General")]
    [PropertyOrder(-995)]
    [ShowIf(nameof(ShowInitialUnlockedInInspector))]
#endif
    [SerializeField]
    bool initialUnclocked = false;


#if ODIN_INSPECTOR
    [FoldoutGroup("General")]
    [PropertyOrder(-995)]
    [ReadOnly]
    [ShowInInspector]
#endif
    [SerializeField]
    string configId;

    public GameObject Prefab => prefab;
    public LocalizedString DisplayName => displayName;
    public LocalizedString Description => description;
    public Sprite Icon => icon;
    public string ConfigId => configId;
    public bool UseInRepository => useInRepository;
  

#if ODIN_INSPECTOR
    protected virtual bool ShowCardCostsInInspector => true;
    protected virtual bool ShowPrefabInInspector => true;
    protected virtual bool ShowDisplayNameInInspector => true;
    protected virtual bool ShowDescriptionInInspector => true;
    protected virtual bool ShowIconInInspector => true;
    protected virtual bool ShowUseInRepositoryInInspector => true;
    protected virtual bool ShowInitialUnlockedInInspector => true;
    protected virtual bool ShowLifetimeInInspector => true;
#endif

#if UNITY_EDITOR
    // Keep editor-only cache non-serialized to avoid player/editor serialization layout mismatch.
    string assetGuid;

    protected virtual void OnValidate()
    {
        EnsureGuidSync();
    }

#if ODIN_INSPECTOR
    [FoldoutGroup("General")]
    [PropertyOrder(-996)]
    [Button]
#endif
    public void GenerateNewId()
    {
        GenerateNewIdInternal();
    }

    void EnsureGuidSync()
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid))
        {
            return;
        }

        if (string.IsNullOrEmpty(configId))
        {
            GenerateNewIdInternal();
            return;
        }

        assetGuid = guid;
    }

    void GenerateNewIdInternal()
    {
        configId = Guid.NewGuid().ToString("N");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
