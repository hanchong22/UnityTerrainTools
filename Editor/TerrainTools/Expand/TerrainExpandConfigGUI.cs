using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEditor.TerrainTools
{
    public class TerrainExpandConfigGUI
    {
        private static TerrainExpandConfigGUI _instance;
        public static TerrainExpandConfigGUI Instance
        {
            get
            {
                if (TerrainExpandConfigGUI._instance != null)
                {
                    return TerrainExpandConfigGUI._instance;
                }

                TerrainExpandConfigGUI._instance = new TerrainExpandConfigGUI();
                return TerrainExpandConfigGUI._instance;
            }
        }

        private static void AddHeightLayer()
        {
            TerrainExpandConfig.Instance.HeightMapCount++;
            TerrainExpandConfig.InitLayerInfos();
            TerrainExpandConfig.SaveSetting();
        }

        private static void RemoveHeightLayer(int idx, bool dialog = true)
        {
            if (TerrainExpandConfig.Instance.HeightMaps[idx].isLocked)
            {
                EditorUtility.DisplayDialog("错误", "无法删除已锁定的图层", "确定");
                return;
            }

            if (idx < 0)
            {
                EditorUtility.DisplayDialog("错误", "不得删除基础层", "确定");
                return;
            }

            if (dialog && !EditorUtility.DisplayDialog("确认删除", $"真的删除{TerrainExpandConfig.Instance.HeightMaps[idx].title}吗？", "删除", "不删"))
            {
                return;
            }

            TerrainExpandConfig.Instance.HeightMapCount--;

            if (TerrainExpandConfig.Instance.CurrentHeightLayer <= TerrainExpandConfig.Instance.HeightMapCount)
            {
                TerrainExpandConfig.Instance.CurrentHeightLayer = TerrainExpandConfig.Instance.HeightMapCount - 1;
            }

            if (TerrainExpandConfig.Instance.HeightMaps.Count > idx)
            {
                TerrainExpandConfig.Instance.HeightMaps.RemoveAt(idx);
            }

            var allTerrains = TerrainExpandTools.GetAllTerrains();
            for (int i = 0; i < allTerrains.Length; ++i)
            {
                var t = allTerrains[i];
                if (t.gameObject.TryGetComponent<TerrainExpand>(out var terrainExp))
                {
                    terrainExp.RemoveLayer(idx);
                }
            }

            TerrainExpandConfig.SaveSetting();

            ReloadSelectedLayers();
        }

        private static void ReloadSelectedLayers()
        {
            var allTerrains = TerrainExpandTools.GetAllTerrains();

            for (int i = 0; i < allTerrains.Length; ++i)
            {
                var t = allTerrains[i];
                if (t.gameObject.TryGetComponent<TerrainExpand>(out var terrainExp))
                {
                    terrainExp.ReLoadLayer(1);
                }
            }

        }

        private void MergeWithUpper(int idx)
        {
            if (idx <= 0 || TerrainExpandConfig.Instance.HeightMapCount <= 1)
            {
                return;
            }

            if (TerrainExpandConfig.Instance.HeightMaps[idx].isLocked)
            {
                EditorUtility.DisplayDialog("错误", "无法操作已锁定的图层", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog(TerrainExpandConfigGUI.GetStyles().MergeWithUpper.text, "确认将此图层与下一层合并，并且删除此图层吗？", "确认", "取消"))
            {
                return;
            }

            var terrains = TerrainExpandTools.GetAllTerrains();

            for (int i = 0; i < terrains.Length; ++i)
            {
                if (terrains[i].TryGetComponent<TerrainExpand>(out var terrainExpand))
                {
                    terrainExpand.MergeHeightMapWithUpper(idx);
                }
            }

            TerrainExpandConfigGUI.RemoveHeightLayer(idx, false);
        }


        #region editor gui

        private static int titleEditorIdx = -1;
        private List<TerrainExpand> waitToSaveTerrains = new List<TerrainExpand>();

        private class Styles
        {
            public readonly GUIContent heightValueScale = EditorGUIUtility.TrTextContent("高度值缩放");
            public readonly GUIContent MergeWithUpper = EditorGUIUtility.TrTextContent("向下合并");
            public readonly GUIContent save = EditorGUIUtility.TrTextContent("保存", "保存所修改");
            public readonly GUIStyle redTitle = new GUIStyle()
            {
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState()
                {
                    textColor = Color.red,
                },
                alignment = TextAnchor.MiddleLeft,
            };
        }

        private static Styles m_styles;
        private static Styles GetStyles()
        {
            if (m_styles == null)
            {
                m_styles = new Styles();
            }

            return m_styles;
        }

        private void SaveAllHeightmapToFile()
        {
            foreach (var te in this.waitToSaveTerrains)
            {
                te?.SaveData();
            }

            this.waitToSaveTerrains.Clear();
        }

        public void SetDirty(Terrain d)
        {
            if (d.gameObject.TryGetComponent<TerrainExpand>(out var terrainExp) && !this.waitToSaveTerrains.Contains(terrainExp))
            {
                this.waitToSaveTerrains.Add(terrainExp);
            }
        }

        public static void ShowExpandUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical("sv_iconselector_back");
            {

                if (GUILayout.Button("", "OL Plus", GUILayout.Width(20)))
                {
                    titleEditorIdx = -1;
                    AddHeightLayer();
                }

                for (int i = TerrainExpandConfig.Instance.HeightMapCount - 1; i >= 0; i--)
                {
                    if (i == TerrainExpandConfig.Instance.CurrentHeightLayer)
                    {
                        GUILayout.Space(5);
                    }

                    EditorGUILayout.BeginHorizontal(i == TerrainExpandConfig.Instance.CurrentHeightLayer ? "LightmapEditorSelectedHighlight" : "SelectionRect");
                    {
                        GUILayout.Space(5);
                        if (titleEditorIdx == i)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                TerrainExpandConfig.Instance.HeightMaps[i].title = GUILayout.TextField(TerrainExpandConfig.Instance.HeightMaps[i].title, "ToolbarTextField");

                                if (GUILayout.Button(EditorGUIUtility.IconContent("AvatarInspector/DotSelection"), GUILayout.Width(25)))
                                {
                                    titleEditorIdx = -1;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            if (GUILayout.Button(TerrainExpandConfig.Instance.HeightMaps[i].title, "BoldLabel"))
                            {
                                TerrainExpandConfig.Instance.CurrentHeightLayer = i;
                            }

                            if (GUILayout.Button(EditorGUIUtility.IconContent("editicon.sml"), GUILayout.Width(25)))
                            {
                                titleEditorIdx = i;
                            }
                        }


                        if (TerrainExpandConfig.Instance.HeightMaps[i].isLocked)
                        {
                            if (GUILayout.Button(EditorGUIUtility.IconContent("IN LockButton on"), GUILayout.Width(25)))
                            {
                                TerrainExpandConfig.Instance.HeightMaps[i].isLocked = false;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(EditorGUIUtility.IconContent("IN LockButton"), GUILayout.Width(25)))
                            {
                                TerrainExpandConfig.Instance.HeightMaps[i].isLocked = true;
                            }
                        }

                        //显示与隐藏
                        EditorGUI.BeginChangeCheck();
                        {
                            TerrainExpandConfig.Instance.HeightMaps[i].isValid = GUILayout.Toggle(TerrainExpandConfig.Instance.HeightMaps[i].isValid, "", "OL ToggleWhite", GUILayout.Width(20));
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            titleEditorIdx = -1;

                            ReloadSelectedLayers();
                        }

                        // 导入高度图
                        if (GUILayout.Button(EditorGUIUtility.IconContent("SceneLoadIn"), GUILayout.Width(25)))
                        {
                            TerrainExpandConfigGUI.Instance.ImportHeightmapFile(i);
                        }
                        // 导出高度图
                        if (GUILayout.Button(EditorGUIUtility.IconContent("SceneLoadOut"), GUILayout.Width(25)))
                        {
                            TerrainExpandConfigGUI.Instance.ExportHeightmapFile(i);
                        }

                        //删除
                        if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(25)))
                        {
                            titleEditorIdx = -1;
                            RemoveHeightLayer(i);
                        }

                    }

                    GUILayout.Space(5);

                    EditorGUILayout.EndHorizontal();


                    if (i == TerrainExpandConfig.Instance.CurrentHeightLayer)
                    {
                        GUILayout.Space(5);
                    }
                }//  for (int i = TerrainExpandConfig.Instance.HeightMapCount - 1; i >= 0; i--)


                // houdini layer
                if (-2 == TerrainExpandConfig.Instance.CurrentHeightLayer)
                {
                    GUILayout.Space(5);
                }
                EditorGUILayout.BeginHorizontal(TerrainExpandConfig.Instance.CurrentHeightLayer == -2 ? "LightmapEditorSelectedHighlight" : "SelectionRect");
                {
                    GUILayout.Space(5);
                    EditorGUI.BeginChangeCheck();
                    {
                        if (GUILayout.Button("Houdini Layer", "BoldLabel", GUILayout.Width(150)))
                        {
                            TerrainExpandConfig.Instance.CurrentHeightLayer = -2;
                        }
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        titleEditorIdx = -1;
                    }

                    // 导入高度图
                    if (GUILayout.Button(EditorGUIUtility.IconContent("SceneLoadIn"), GUILayout.Width(25)))
                    {
                        TerrainExpandConfigGUI.Instance.ImportHeightmapFile(-2);
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        TerrainExpandConfig.Instance.ShowHoudiniLayer = GUILayout.Toggle(TerrainExpandConfig.Instance.ShowHoudiniLayer, "", "OL ToggleWhite", GUILayout.Width(20));
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        ReloadSelectedLayers();
                    }

                    GUILayout.Label(EditorGUIUtility.IconContent("IN LockButton on"));

                    GUILayout.Space(5);
                }
                EditorGUILayout.EndHorizontal();
                if (-2 == TerrainExpandConfig.Instance.CurrentHeightLayer)
                {
                    GUILayout.Space(5);
                }

                if (-1 == TerrainExpandConfig.Instance.CurrentHeightLayer)
                {
                    GUILayout.Space(5);
                }
                EditorGUILayout.BeginHorizontal(TerrainExpandConfig.Instance.CurrentHeightLayer == -1 ? "LightmapEditorSelectedHighlight" : "SelectionRect");
                {
                    GUILayout.Space(5);
                    EditorGUI.BeginChangeCheck();
                    {
                        if (GUILayout.Button("Base Layer", "BoldLabel", GUILayout.Width(150)))
                        {
                            TerrainExpandConfig.Instance.CurrentHeightLayer = -1;
                        }
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        titleEditorIdx = -1;
                    }

                    // 重新生成纹理
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), GUILayout.Width(25)))
                    {
                        TerrainExpandConfig.CurrentSelectedTerrain.gameObject.GetComponent<TerrainExpand>()?.InitBaseLayer(true);
                    }

                    // 导出高度图
                    if (GUILayout.Button(EditorGUIUtility.IconContent("SceneLoadOut"), GUILayout.Width(25)))
                    {
                        TerrainExpandConfigGUI.Instance.ExportHeightmapFile(-1);
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        TerrainExpandConfig.Instance.ShowBaseLayer = GUILayout.Toggle(TerrainExpandConfig.Instance.ShowBaseLayer, "", "OL ToggleWhite", GUILayout.Width(20));
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        ReloadSelectedLayers();
                    }

                    GUILayout.Label(EditorGUIUtility.IconContent("IN LockButton on"));

                    GUILayout.Space(5);
                }
                EditorGUILayout.EndHorizontal();

                if (-1 == TerrainExpandConfig.Instance.CurrentHeightLayer)
                {
                    GUILayout.Space(5);
                }


                GUILayout.Space(2);

                if (TerrainExpandConfig.Instance.CurrentHeightLayer > 0)
                {
                    if (GUILayout.Button(GetStyles().MergeWithUpper))
                    {
                        TerrainExpandConfigGUI.Instance.MergeWithUpper(TerrainExpandConfig.Instance.CurrentHeightLayer);
                    }
                }

                GUILayout.Space(3);

                if (TerrainExpandConfigGUI.Instance.waitToSaveTerrains.Count > 0)
                {
                    if (GUILayout.Button(GetStyles().save))
                    {
                        TerrainExpandConfigGUI.Instance.SaveAllHeightmapToFile();
                    }
                }
            }
            EditorGUILayout.EndVertical();


            if (EditorGUI.EndChangeCheck())
            {
                TerrainExpandConfig.SaveSetting();
            }
        }

        private void ImportHeightmapFile(int importLayer)
        {
            string importFilePath = EditorUtility.OpenFilePanelWithFilters("选择地型高度图", System.IO.Directory.GetCurrentDirectory(), new string[] { "地型数据", "raw,r16", "All files", "*" });
            if (!string.IsNullOrEmpty(importFilePath))
            {
                TerrainExpandConfig.CurrentSelectedTerrain.gameObject.GetComponent<TerrainExpand>()?.ReadRaw(importFilePath, false, importLayer);
            }
        }

        private void ExportHeightmapFile(int exportLayer)
        {
            string exportPath = EditorUtility.OpenFolderPanel("选择目标文件夹", System.IO.Directory.GetCurrentDirectory(), "打开文件夹");
            if (!string.IsNullOrEmpty(exportPath))
            {
                TerrainExpandConfig.CurrentSelectedTerrain.gameObject.GetComponent<TerrainExpand>()?.WriteRaw(exportPath, false, exportLayer);
            }
        }

        #endregion
    }
}
