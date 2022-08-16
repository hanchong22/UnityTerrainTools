﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.TerrainTools;

namespace UnityEngine.Experimental.TerrainAPI
{
    public class PaintContextExp : UnityEngine.TerrainTools.PaintContext
    {
        public PaintContextExp(Terrain terrain, RectInt pixelRect, int targetTextureWidth, int targetTextureHeight, bool texelPadding = true) : base(terrain, pixelRect, targetTextureWidth, targetTextureHeight, texelPadding)
        {
        }

        public static PaintContextExp CreateExpFromBounds(Terrain terrain, Rect boundsInTerrainSpace, int inputTextureWidth, int inputTextureHeight, int extraBorderPixels = 0, bool texelPadding = true)
        {
            return new PaintContextExp(
                terrain,
                TerrainExpandTools.CalcPixelRectFromBounds(terrain, boundsInTerrainSpace, inputTextureWidth, inputTextureHeight, extraBorderPixels, texelPadding),
                inputTextureWidth, inputTextureHeight, texelPadding);
        }

        public void GatherInitHeightmap()
        {
            var blitMaterial = UnityEngine.TerrainTools.TerrainPaintUtility.GetHeightBlitMaterial();
            blitMaterial.SetFloat("_Height_Offset", 0.0f);
            blitMaterial.SetFloat("_Height_Scale", 1.0f);

            GatherInternalExp(
                t => TerrainExpandTools.GetHeightMapByIdx(t), //t.terrainData.heightmapTexture, //
                new Color(0.0f, 0.0f, 0.0f, 0.0f),
                "PaintContext.GatherHeightmap",
                blitMaterial: blitMaterial,
                beforeBlit: t =>
                {
                    blitMaterial.SetFloat("_Height_Offset", (t.GetPosition().y - heightWorldSpaceMin) / heightWorldSpaceSize * kNormalizedHeightScale);
                    blitMaterial.SetFloat("_Height_Scale", t.terrainData.size.y / heightWorldSpaceSize);
                });
        }

        public void GatherInitCurvemap()
        {
            var blitMaterial = UnityEngine.TerrainTools.TerrainPaintUtility.GetHeightBlitMaterial();
            blitMaterial.SetFloat("_Height_Offset", 0.0f);
            blitMaterial.SetFloat("_Height_Scale", 1.0f);

            GatherInternalExp(
                t => TerrainExpandTools.GetCurveMapByIdx(t), //t.terrainData.heightmapTexture, //
                new Color(0.0f, 0.0f, 0.0f, 0.0f),
                "PaintContext.GatherHeightmap",
                blitMaterial: blitMaterial,
                beforeBlit: t =>
                {
                    blitMaterial.SetFloat("_Height_Offset", (t.GetPosition().y - heightWorldSpaceMin) / heightWorldSpaceSize * kNormalizedHeightScale);
                    blitMaterial.SetFloat("_Height_Scale", t.terrainData.size.y / heightWorldSpaceSize);
                });
        }

        public void GatherLeftHeightmap(List<int> layerIds)
        {
            var blitMaterial = UnityEngine.TerrainTools.TerrainPaintUtility.GetHeightBlitMaterial();

            blitMaterial.SetFloat("_Height_Offset", 0.0f);
            blitMaterial.SetFloat("_Height_Scale", 1.0f);

            var addMat = TerrainExpandTools.GetHeightmapBlitExtMat(); //TerrainExpandTools.GetHeightSubtractionMat();

            for (int i = 0; i < layerIds.Count; ++i)
            {
                var tmpTarget = RenderTexture.GetTemporary(this.destinationRenderTexture.width, this.destinationRenderTexture.height, 0, this.destinationRenderTexture.format, RenderTextureReadWrite.Linear);
                var tmpTarget2 = RenderTexture.GetTemporary(this.destinationRenderTexture.width, this.destinationRenderTexture.height, 0, this.destinationRenderTexture.format, RenderTextureReadWrite.Linear);


                GatherInternalExp(
                    t => TerrainExpandTools.GetHeightMapByIdx(t, layerIds[i]),
                    new Color(0.0f, 0.0f, 0.0f, 0.0f),
                    "PaintContext.GatherHeightmap",
                    blitMaterial: blitMaterial,
                    beforeBlit: null,
                    afterBlit: null,
                    oldTexture: null,

                    targetTex: () =>
                     {
                         return tmpTarget;
                     }
                    );

                addMat.SetTexture("_Tex1", tmpTarget);
                addMat.SetTexture("_Tex2", this.destinationRenderTexture);
                addMat.SetFloat("_Height_Offset", 0.0f);
                addMat.SetFloat("_Height_Scale", 1.0f);
                addMat.SetFloat("_Target_Height", 1.0f);
                addMat.SetFloat("_Overlay_Layer", 0.0f);
                addMat.EnableKeyword("_HEIGHT_TYPE");
                addMat.DisableKeyword("_HOLE_TYPE");

                Graphics.Blit(null, tmpTarget2, addMat);
                Graphics.Blit(tmpTarget2, this.destinationRenderTexture);

                RenderTexture.ReleaseTemporary(tmpTarget);
                RenderTexture.ReleaseTemporary(tmpTarget2);
            }
        }


        private void GatherInternalExp(
           Func<Terrain, Texture> terrainToTexture,
           Color defaultColor,
           string operationName,
           Material blitMaterial = null,
           int blitPass = 0,
           Action<Terrain> beforeBlit = null,
           Action<Terrain> afterBlit = null,
           Func<Texture> oldTexture = null,
           Func<RenderTexture> targetTex = null
           )
        {
            if (blitMaterial == null)
                blitMaterial = TerrainPaintUtility.GetHeightBlitMaterial();

            RenderTexture.active = targetTex != null ? targetTex() : sourceRenderTexture;
            GL.Clear(false, true, defaultColor);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, pixelRect.width, 0, pixelRect.height);
            for (int i = 0; i < terrainCount; i++)
            {
                var terrain = GetTerrain(i);

                Texture sourceTexture = terrainToTexture(terrain);
                if (!sourceTexture)
                    continue;

                beforeBlit?.Invoke(terrain);

                FilterMode oldFilterMode = sourceTexture.filterMode;
                sourceTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", sourceTexture);
                if (oldTexture != null)
                {
                    blitMaterial.SetTexture("_OldTexture", oldTexture());
                }

                blitMaterial.SetPass(blitPass);
                TerrainExpandTools.DrawQuad(GetClippedPixelRectInRenderTexturePixels(i), GetClippedPixelRectInTerrainPixels(i), sourceTexture);

                sourceTexture.filterMode = oldFilterMode;

                afterBlit?.Invoke(terrain);
            }
            GL.PopMatrix();
            RenderTexture.active = oldRenderTexture;
        }

    }


}
