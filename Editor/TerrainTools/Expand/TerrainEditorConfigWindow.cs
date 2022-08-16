using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class TerrainEditorConfigWindow : EditorWindow
{
    public static TerrainEditorConfigWindow GetWindow()
    {
        TerrainEditorConfigWindow window = EditorWindow.GetWindow<TerrainEditorConfigWindow>();
        window.titleContent = new GUIContent("地型编辑器配置", "连接编辑器服务，选择当前编辑的地块");
        window.minSize = new Vector2(480.0f, 256.0f);

        return window;
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }

    private void OnGUI()
    {
        var terrains = TerrainExpandTools.GetAllTerrains();
        foreach (var t in terrains)
        {

        }
    }

    private void GetTerrainsBounds()
    {
        var terrains = TerrainExpandTools.GetAllTerrains();
        Rect rect = new Rect(terrains[0].gameObject.transform.position.x, terrains[0].gameObject.transform.position.y, 0, 0);
        foreach (var t in terrains)
        {
            rect.x = Math.Min(t.gameObject.transform.position.x, rect.x);
            rect.y = Math.Min(t.gameObject.transform.position.y, rect.y);

            float width = t.gameObject.transform.position.x - rect.x;
            float height = t.gameObject.transform.position.y - rect.y;

            rect.width = Mathf.Max(width, rect.width);
            rect.height = Mathf.Max(height, rect.height);
        }
    }
}

