using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TerrainTools;
#endif

[ExecuteInEditMode]
public class TerrainExpInitBehaviour : MonoBehaviour
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void beforeSceneLoad()
    {

    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void afterSceneLoaded()
    {
        Terrain[] allTerrians = TerrainExpandTools.GetAllTerrains();
        foreach (Terrain t in allTerrians)
        {
            t.gameObject.AddMissingComponent<TerrainExpand>();
        }
    }

    public void OnValidate()
    {
        afterSceneLoaded();
    }

    public void Awake()
    {
        afterSceneLoaded();
    }

#endif
}

