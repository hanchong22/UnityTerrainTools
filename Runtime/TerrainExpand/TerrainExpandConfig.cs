using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class TerrainExpandConfig
{
#if UNITY_EDITOR
    private static TerrainExpandConfig _instance;
    public static TerrainExpandConfig Instance
    {
        get
        {
            if (TerrainExpandConfig._instance != null)
            {
                return TerrainExpandConfig._instance;
            }

            TerrainExpandConfig._instance = new TerrainExpandConfig();
            TerrainExpandConfig.LoadSetting();
            TerrainExpandConfig.InitLayerInfos();
            return TerrainExpandConfig._instance;
        }
    }

    public static Terrain CurrentSelectedTerrain { get; set; }

    private int _HeightMapCount = 1;
    private int _CurrentHeightLayer = 0;

    public int HeightMapCount
    {
        get
        {
            return Math.Max(1, this._HeightMapCount);
        }
        set
        {
            this._HeightMapCount = Math.Max(1, value);
        }
    }

    public int CurrentHeightLayer
    {
        get
        {
            return this._CurrentHeightLayer < this.HeightMapCount && this._CurrentHeightLayer >= -2 ? this._CurrentHeightLayer : -2;
        }
        set
        {
            this._CurrentHeightLayer = value < this.HeightMapCount && value >= -2 ? value : this.HeightMapCount - 1;
        }
    }

    public class MapLayerInfo
    {
        public string title;
        public bool isLocked;
        public bool isValid;
        public bool isOverlay;
    }

    public List<MapLayerInfo> HeightMaps;

    public bool ShowHoudiniLayer = true;
    public bool ShowBaseLayer = true;    

    public TerrainExpandConfig()
    {
        this.HeightMapCount = 1;
        this.CurrentHeightLayer = 0;
    }

    public static void InitLayerInfos()
    {
        if (Instance.HeightMaps == null)
        {
            Instance.HeightMaps = new List<TerrainExpandConfig.MapLayerInfo>();
        }

        for (int i = Instance.HeightMaps.Count; i < Instance.HeightMapCount; ++i)
        {
            TerrainExpandConfig.MapLayerInfo item = new TerrainExpandConfig.MapLayerInfo()
            {
                title = $"Layer {i + 1}",
                isLocked = false,
                isValid = true,
            };

            Instance.HeightMaps.Add(item);
        }
    }

    public static void LoadSetting()
    {
        Instance.HeightMapCount = EditorPrefs.GetInt("Unity.TerrainTools.TerrainExpandConfig.HeightMapCount", 1);
        Instance.CurrentHeightLayer = EditorPrefs.GetInt("Unity.TerrainTools.TerrainExpandConfig.CurrentHeightLayer", 0);
    }

    public static void SaveSetting()
    {
        EditorPrefs.SetInt("Unity.TerrainTools.TerrainExpandConfig.HeightMapCount", Instance.HeightMapCount);
        EditorPrefs.SetInt("Unity.TerrainTools.TerrainExpandConfig.CurrentHeightLayer", Instance.CurrentHeightLayer);
    }

    public bool isCanEditorTerrain
    {
        get
        {
            return TerrainExpandConfig.Instance.CurrentHeightLayer >= 0 &&                
                TerrainExpandConfig.Instance.CurrentHeightLayer < TerrainExpandConfig.Instance.HeightMapCount &&
                TerrainExpandConfig.Instance.HeightMaps.Count > TerrainExpandConfig.Instance.CurrentHeightLayer &&
                TerrainExpandConfig.Instance.HeightMaps[TerrainExpandConfig.Instance.CurrentHeightLayer].isValid &&
                !TerrainExpandConfig.Instance.HeightMaps[TerrainExpandConfig.Instance.CurrentHeightLayer].isLocked;
        }
    }

#endif
}

#if UNITY_EDITOR
public class TerrainHeightMapConfig
{
    public RenderTexture rt;
    public Texture2D heightMap;
}

#endif