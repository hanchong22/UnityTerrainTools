using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
#if UNITY_EDITOR
using UnityEngine.TerrainTools;
using Object = UnityEngine.Object;
using UnityEditor;
#endif

public static class TerrainExpandTools
{
    public enum BuiltinPaintMaterialPasses
    {
        RaiseLowerHeight = 0,
        StampHeight,
        SetHeights,
        SmoothHeights,
        PaintTexture,
        PaintHoles
    }

    public static float kNormalizedHeightScale => 32766.0f / 65535.0f;


    private static Material heightMapBlitExtMat = null;
    public static Material GetHeightmapBlitExtMat()
    {
        if (!TerrainExpandTools.heightMapBlitExtMat)
        {
            TerrainExpandTools.heightMapBlitExtMat = new Material(Shader.Find("Hidden/TerrainEngine/HeightBlitAdd"));
        }

        return TerrainExpandTools.heightMapBlitExtMat;
    }

    public static Terrain[] GetAllTerrains()
    {
        return Terrain.activeTerrains;//TerrainManager.cpp: TerrainManager::GetActiveTerrainsScriptingArray()
        //return GameObject.FindObjectsOfType<Terrain>();
    }


#if UNITY_EDITOR
    public static PaintContextExp BeginPaintHeightmap(Terrain terrain, Rect boundsInTerrainSpace, int extraBorderPixels = 0)
    {
        int heightmapResolution = terrain.terrainData.heightmapResolution;
        PaintContextExp ctx = InitializePaintContext(terrain, heightmapResolution, heightmapResolution, RenderTextureFormat.RHalf, boundsInTerrainSpace, extraBorderPixels);
        ctx.GatherInitHeightmap();
        return ctx;
    }

    public static PaintContextExp BeginPaintCurvemap(Terrain terrain, Rect boundsInTerrainSpace, int extraBorderPixels = 0)
    {
        int heightmapResolution = terrain.terrainData.heightmapResolution;
        PaintContextExp ctx = InitializePaintContext(terrain, heightmapResolution, heightmapResolution, RenderTextureFormat.RHalf, boundsInTerrainSpace, extraBorderPixels);
        ctx.GatherInitCurvemap();
        return ctx;
    }

    internal static PaintContextExp InitializePaintContext(Terrain terrain, int targetWidth, int targetHeight, RenderTextureFormat pcFormat, Rect boundsInTerrainSpace, int extraBorderPixels = 0, bool texelPadding = true)
    {
        PaintContextExp ctx = PaintContextExp.CreateExpFromBounds(terrain, boundsInTerrainSpace, targetWidth, targetHeight, extraBorderPixels, texelPadding);
        ctx.CreateRenderTargets(pcFormat);
        return ctx;
    }

    internal static RectInt CalcPixelRectFromBounds(Terrain terrain, Rect boundsInTerrainSpace, int textureWidth, int textureHeight, int extraBorderPixels, bool texelPadding)
    {
        float scaleX = (textureWidth - (texelPadding ? 1.0f : 0.0f)) / terrain.terrainData.size.x;
        float scaleY = (textureHeight - (texelPadding ? 1.0f : 0.0f)) / terrain.terrainData.size.z;
        int xMin = Mathf.FloorToInt(boundsInTerrainSpace.xMin * scaleX) - extraBorderPixels;
        int yMin = Mathf.FloorToInt(boundsInTerrainSpace.yMin * scaleY) - extraBorderPixels;
        int xMax = Mathf.CeilToInt(boundsInTerrainSpace.xMax * scaleX) + extraBorderPixels;
        int yMax = Mathf.CeilToInt(boundsInTerrainSpace.yMax * scaleY) + extraBorderPixels;
        return new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
    }

    internal static void DrawQuad(RectInt destinationPixels, RectInt sourcePixels, Texture sourceTexture)
    {
        DrawQuad2(destinationPixels, sourcePixels, sourceTexture, sourcePixels, sourceTexture);
    }

    internal static void DrawQuad2(RectInt destinationPixels, RectInt sourcePixels, Texture sourceTexture, RectInt sourcePixels2, Texture sourceTexture2)
    {
        if ((destinationPixels.width > 0) && (destinationPixels.height > 0))
        {
            Rect sourceUVs = new Rect(
                (sourcePixels.x) / (float)sourceTexture.width,
                (sourcePixels.y) / (float)sourceTexture.height,
                (sourcePixels.width) / (float)sourceTexture.width,
                (sourcePixels.height) / (float)sourceTexture.height);

            Rect sourceUVs2 = new Rect(
                (sourcePixels2.x) / (float)sourceTexture2.width,
                (sourcePixels2.y) / (float)sourceTexture2.height,
                (sourcePixels2.width) / (float)sourceTexture2.width,
                (sourcePixels2.height) / (float)sourceTexture2.height);

            GL.Begin(GL.QUADS);
            GL.Color(new Color(1.0f, 1.0f, 1.0f, 1.0f));
            GL.MultiTexCoord2(0, sourceUVs.x, sourceUVs.y);
            GL.MultiTexCoord2(1, sourceUVs2.x, sourceUVs2.y);
            GL.Vertex3(destinationPixels.x, destinationPixels.y, 0.0f);
            GL.MultiTexCoord2(0, sourceUVs.x, sourceUVs.yMax);
            GL.MultiTexCoord2(1, sourceUVs2.x, sourceUVs2.yMax);
            GL.Vertex3(destinationPixels.x, destinationPixels.yMax, 0.0f);
            GL.MultiTexCoord2(0, sourceUVs.xMax, sourceUVs.yMax);
            GL.MultiTexCoord2(1, sourceUVs2.xMax, sourceUVs2.yMax);
            GL.Vertex3(destinationPixels.xMax, destinationPixels.yMax, 0.0f);
            GL.MultiTexCoord2(0, sourceUVs.xMax, sourceUVs.y);
            GL.MultiTexCoord2(1, sourceUVs2.xMax, sourceUVs2.y);
            GL.Vertex3(destinationPixels.xMax, destinationPixels.y, 0.0f);
            GL.End();
        }
    }

    public static Texture GetHeightMapByIdx(Terrain t, int idx = -3)
    {
        TerrainExpand terrainExp = t.gameObject.AddMissingComponent<TerrainExpand>();
        return terrainExp.GetCurrentHeightMap(idx < -2 || idx >= TerrainExpandConfig.Instance.HeightMapCount ? TerrainExpandConfig.Instance.CurrentHeightLayer : idx);
    }

    public static Texture GetCurveMapByIdx(Terrain t, int idx = -2)
    {
        TerrainExpand terrainExp = t.gameObject.AddMissingComponent<TerrainExpand>();
        return terrainExp.GetCurrentHeightMap(idx < -2 || idx >= TerrainExpandConfig.Instance.HeightMapCount ? TerrainExpandConfig.Instance.CurrentHeightLayer : idx);
    }

#endif
}

#if UNITY_EDITOR
public class AssetDatabaseTools
{
    public static void CreateAssetFromObjects(Object[] assets, string path)
    {
        AssetDatabase.CreateAsset(assets[0], path);
        Object mainObj = AssetDatabase.LoadAssetAtPath<Object>(path);
        for (int i = 1; i < assets.Length; i++)
        {
            AssetDatabase.AddObjectToAsset(assets[i], mainObj);
        }

        AssetDatabase.SaveAssets();
    }
}
#endif

