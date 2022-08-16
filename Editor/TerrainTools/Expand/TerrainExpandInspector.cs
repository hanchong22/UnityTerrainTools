using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(TerrainExpand))]
public class TerrainExpandInspector : Editor
{
    class Styles
    {
        public readonly GUIContent currentHeitMapTitle = EditorGUIUtility.TrTextContent("当前图层", "当前正在编辑的高度图");
    }

    private static Styles m_styles;
    private Styles GetStyles()
    {
        if (m_styles == null)
        {
            m_styles = new Styles();
        }
        return m_styles;
    }

    private TerrainExpand script;

    public void OnEnable()
    {
        this.script = this.target as TerrainExpand;
        TerrainExpandConfig.CurrentSelectedTerrain = this.script.gameObject.GetComponent<Terrain>();
    }

    public void OnDestroy()
    {
        if (TerrainExpandConfig.CurrentSelectedTerrain == this.script.gameObject.GetComponent<Terrain>())
        {
            TerrainExpandConfig.CurrentSelectedTerrain = null;
        }
    }

    public override void OnInspectorGUI()
    {
        Styles styles = GetStyles();
        base.serializedObject.Update();

        GUILayout.Space(150);
        if (TerrainExpandConfig.Instance.CurrentHeightLayer >= 0)
        {
            if (this.script && this.script.rtHeightMapList.Count >= TerrainExpandConfig.Instance.HeightMapCount)
            {
                GUI.DrawTexture(new Rect(5, 5, 100, 100), this.script.rtHeightMapList[TerrainExpandConfig.Instance.CurrentHeightLayer], ScaleMode.ScaleToFit, false);
            }
        }
        else if (TerrainExpandConfig.Instance.CurrentHeightLayer == -1)
        {
            GUI.DrawTexture(new Rect(5, 5, 100, 100), this.script.baseLayerRt, ScaleMode.ScaleToFit, false);
        }
        else if (TerrainExpandConfig.Instance.CurrentHeightLayer == -2)
        {
            GUI.DrawTexture(new Rect(5, 5, 100, 100), this.script.importLayerRt, ScaleMode.ScaleToFit, false);
        }
        else
        {
            GUI.DrawTexture(new Rect(5, 5, 100, 100), this.script.rtHeightMapList[0], ScaleMode.ScaleToFit, false);
        }


        base.serializedObject.ApplyModifiedProperties();
    }
}

