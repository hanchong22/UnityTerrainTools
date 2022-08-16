using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using UnityEngine.Pool;
using UnityEngine.TerrainTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TerrainTools;
#endif


[RequireComponent(typeof(Terrain))]
[ExecuteInEditMode]
public class TerrainExpand : MonoBehaviour
{
#if UNITY_EDITOR
    private List<Texture2D> heightMapList;
    public List<RenderTexture> rtHeightMapList { get; private set; } = new List<RenderTexture>();

    //基础层（Atlas 层）
    private Texture2D baseLayerMap;
    public RenderTexture baseLayerRt;

    //导入层(Houdini 层)
    private Texture2D importLayerMap;
    public RenderTexture importLayerRt;

    private TerrainData terrainData;
    private Terrain terrain;
    private string terrainDataPath;

    private List<bool> changedIds = new List<bool>();

    private void Awake()
    {
        this.terrain = gameObject.GetComponent<Terrain>();
        this.terrainData = terrain.terrainData;
        string dataPath = AssetDatabase.GetAssetPath(this.terrainData);
        this.terrainDataPath = dataPath.Substring(0, dataPath.IndexOf(System.IO.Path.GetFileName(dataPath)));
    }

    private void OnEnable()
    {
        this.InitHeightMaps();
    }

    private void OnDestroy()
    {
        this.ClearHeightMaps();
    }

    public void InitHeightMaps()
    {
        this.changedIds.Clear();

        bool hasTexUpdated = false;
        hasTexUpdated = this.InitBaseLayer();
        if (this.InitImportLayer())
        {
            hasTexUpdated = true;
        }


        if (this.heightMapList == null)
        {
            this.heightMapList = new List<Texture2D>();
        }

        for (int i = 0; i < TerrainExpandConfig.Instance.HeightMapCount; ++i)
        {
            RenderTexture rt = null;
            Texture2D tex = null;

            this.changedIds.Add(false);

            if (this.heightMapList.Count > i)
            {
                tex = this.heightMapList[i];
            }

            if (this.rtHeightMapList.Count > i)
            {
                rt = this.rtHeightMapList[i];
            }

            if (!rt)
            {
                rt = RenderTexture.GetTemporary(this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height, 0, RenderTextureFormat.ARGBHalf);
            }

            string texFileName = System.IO.Path.Combine(this.terrainDataPath, $"{this.terrainData.name}_heightmap{i}.asset");

            if (!tex)
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texFileName);

                if (!tex)
                {
                    tex = new Texture2D(this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height, TextureFormat.RGBAHalf, false, true);


                    Graphics.Blit(Texture2D.blackTexture, rt);
                    CopyRtToTexture2D(rt, tex);


                    tex.Apply();

                    AssetDatabase.CreateAsset(tex, texFileName);

                    hasTexUpdated = true;
                }
            }

            Graphics.Blit(tex, rt);

            if (this.rtHeightMapList.Count > i)
            {
                this.rtHeightMapList[i] = rt;
            }
            else
            {
                this.rtHeightMapList.Add(rt);
            }

            if (this.heightMapList.Count > i)
            {
                this.heightMapList[i] = tex;
            }
            else
            {
                this.heightMapList.Add(tex);
            }
        }

        if (hasTexUpdated)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private void ClearHeightMaps()
    {
        if (this.baseLayerRt)
        {
            RenderTexture.ReleaseTemporary(this.baseLayerRt);
        }

        if (this.baseLayerMap)
        {
            GameObject.DestroyImmediate(this.baseLayerMap);
        }

        if (this.importLayerRt)
        {
            RenderTexture.ReleaseTemporary(this.importLayerRt);
        }

        if (this.importLayerMap)
        {
            GameObject.DestroyImmediate(this.importLayerMap);
        }


        if (this.rtHeightMapList != null)
        {
            for (int i = 0; i < this.rtHeightMapList.Count; ++i)
            {
                if (this.rtHeightMapList[i])
                {
                    RenderTexture.ReleaseTemporary(this.rtHeightMapList[i]);
                }
            }

            this.rtHeightMapList.Clear();
        }


        if (this.heightMapList != null)
        {
            for (int i = 0; i < this.heightMapList.Count; ++i)
            {
                if (this.heightMapList[i])
                {
                    GameObject.DestroyImmediate(this.heightMapList[i]);
                }
            }

            this.heightMapList.Clear();
        }

        this.changedIds.Clear();
    }

    private void CheckOrInitData()
    {
        if (this.heightMapList == null || this.heightMapList.Count != TerrainExpandConfig.Instance.HeightMapCount || this.rtHeightMapList.Count != TerrainExpandConfig.Instance.HeightMapCount || this.changedIds.Count != TerrainExpandConfig.Instance.HeightMapCount)
        {
            this.InitHeightMaps();
            return;
        }

        for (int i = 0; i < TerrainExpandConfig.Instance.HeightMapCount; ++i)
        {
            if (!this.heightMapList[i])
            {
                this.InitHeightMaps();
                return;
            }

            if (!this.rtHeightMapList[i])
            {
                this.InitHeightMaps();
                return;
            }
        }
    }

    public Texture GetCurrentHeightMap(int idx)
    {
        this.CheckOrInitData();

        if (idx == -1)
        {
            return this.baseLayerRt;
        }
        else if (idx == -2)
        {
            return this.importLayerRt;
        }
        else
        {
            return this.rtHeightMapList[idx];
        }
    }

    public void SaveData()
    {
        for (int i = 0; i < this.changedIds.Count; ++i)
        {
            if (this.changedIds[i])
            {
                string path = System.IO.Path.Combine(this.terrainDataPath, $"{this.terrainData.name}_heightmap{i}.asset");

                this.heightMapList[i].Apply();

                if (File.Exists(path))
                {
                    //AssetDatabase.SaveAssets();
                }
                else
                {
                    AssetDatabase.CreateAsset(this.heightMapList[i], path);
                    AssetDatabase.ImportAsset(path);
                    this.heightMapList[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
        }
    }

    public bool InitBaseLayer(bool forceReBuild = false)
    {
        string path = System.IO.Path.Combine(this.terrainDataPath, $"{this.terrainData.name}_baseHeightMap.asset");

        if (!this.baseLayerRt)
        {
            this.baseLayerRt = RenderTexture.GetTemporary(this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height, 0, RenderTextureFormat.ARGBHalf);
        }

        this.baseLayerMap = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        bool isNewMap = false;

        if (!this.baseLayerMap || forceReBuild)
        {
            isNewMap = true;
            if (!this.baseLayerMap)
            {
                this.baseLayerMap = new Texture2D(this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height, TextureFormat.RGBAHalf, false, true);
                AssetDatabase.CreateAsset(this.baseLayerMap, path);
            }

            Graphics.Blit(this.terrainData.heightmapTexture, this.baseLayerRt);
            CopyRtToTexture2D(this.baseLayerRt, this.baseLayerMap);

            this.baseLayerMap.Apply();
        }

        if (isNewMap)
        {

        }
        else
        {
            Graphics.Blit(this.baseLayerMap, this.baseLayerRt);
        }

        return isNewMap;
    }

    public bool InitImportLayer()
    {
        string path = System.IO.Path.Combine(this.terrainDataPath, $"{this.terrainData.name}_importHeightMap.asset");

        if (!this.importLayerRt)
        {
            this.importLayerRt = RenderTexture.GetTemporary(this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height, 0, RenderTextureFormat.ARGBHalf);
        }

        this.importLayerMap = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        bool isNewMap = false;
        if (!this.importLayerMap)
        {
            this.importLayerMap = new Texture2D(this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height, TextureFormat.RGBAHalf, false, true);
            isNewMap = true;
        }

        if (isNewMap)
        {
            Graphics.Blit(Texture2D.blackTexture, this.importLayerRt);
            CopyRtToTexture2D(this.importLayerRt, this.importLayerMap);
        }
        else
        {
            Graphics.Blit(this.importLayerMap, this.importLayerRt);
        }

        this.importLayerMap.Apply();

        if (isNewMap)
        {
            AssetDatabase.CreateAsset(this.importLayerMap, path);
        }

        return isNewMap;
    }

    RectInt dstPixels;
    RectInt sourcePixels;

    public void OnPaint(UnityEngine.TerrainTools.PaintContext editContext, int tileIndex, int heightNormal)
    {
        int heightMapIdx = TerrainExpandConfig.Instance.CurrentHeightLayer;

        if (heightMapIdx < 0 && heightMapIdx >= TerrainExpandConfig.Instance.HeightMapCount)
        {
            return;
        }

        // Hidden/TerrainEngine/HeightBlitCopy
        Material blitMaterial = TerrainPaintUtility.GetHeightBlitMaterial(); // TerrainExpandTools.GetHeightSubtractionMat();

        this.CheckOrInitData();

        blitMaterial.SetFloat("_Height_Offset", (editContext.heightWorldSpaceMin - this.terrain.GetPosition().y) / this.terrain.terrainData.size.y * TerrainExpandTools.kNormalizedHeightScale);
        blitMaterial.SetFloat("_Height_Scale", editContext.heightWorldSpaceSize / this.terrain.terrainData.size.y);

        RenderTexture oldRT = RenderTexture.active;
        RenderTexture targetRt = null;
        RenderTexture sourceRt = editContext.destinationRenderTexture;      //已经绘制结果：原地型高度 + 笔刷   
                                                                            // RenderTexture oldTerrainHeight = editContext.sourceRenderTexture;   //原地型高度
        Texture2D targetTex = null;

        this.CheckOrInitData();

        this.dstPixels = editContext.GetClippedPixelRectInTerrainPixels(tileIndex);         //画笔触及的区域（地型的相对坐标）
        this.sourcePixels = editContext.GetClippedPixelRectInRenderTexturePixels(tileIndex); //画笔与当前地型块重叠的区域 （相对于画笔图章）

        targetRt = this.rtHeightMapList[heightMapIdx];
        targetTex = this.heightMapList[heightMapIdx];
        this.changedIds[heightMapIdx] = true;

        RenderTexture.active = targetRt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, targetRt.width, 0, targetRt.height);
        {
            FilterMode oldFilterMode = sourceRt.filterMode;
            sourceRt.filterMode = FilterMode.Bilinear;

            blitMaterial.SetTexture("_MainTex", sourceRt);
            blitMaterial.SetInt("_HeightNormal", heightNormal);
            blitMaterial.SetPass(0);
            TerrainExpandTools.DrawQuad(dstPixels, sourcePixels, sourceRt);

            sourceRt.filterMode = oldFilterMode;
        }
        GL.PopMatrix();

        targetTex.ReadPixels(new Rect(dstPixels.x, targetRt.height - dstPixels.yMax, dstPixels.width, dstPixels.height), dstPixels.x, dstPixels.y);
        //为避免操作时卡顿,将apply移到SaveData中，如果未点保存就关闭程序，则会导致数据丢失
        //targetTex.Apply();
        RenderTexture.active = oldRT;
    }

    public void MergeHeightMapWithUpper(int idx)
    {
        if (this.heightMapList.Count <= idx || this.rtHeightMapList.Count <= idx)
        {
            this.CheckOrInitData();
        }

        Texture2D dstTex = this.heightMapList[idx - 1];
        RenderTexture dstRt = this.rtHeightMapList[idx - 1];

        Texture2D sourceTex = this.heightMapList[idx];
        RenderTexture sourceRt = this.rtHeightMapList[idx];

        var addMat = TerrainExpandTools.GetHeightmapBlitExtMat();

        addMat.SetFloat("_Overlay_Layer", 0);
        addMat.SetTexture("_Tex1", dstTex);
        addMat.SetTexture("_Tex2", sourceTex);
        addMat.SetFloat("_Height_Offset", 0.0f);
        addMat.SetFloat("_Height_Scale", 1.0f);
        addMat.SetFloat("_Target_Height", 1.0f);
        addMat.SetFloat("_Overlay_Layer", 0.0f);
        addMat.EnableKeyword("_HEIGHT_TYPE");
        addMat.DisableKeyword("_HOLE_TYPE");

        Graphics.Blit(null, dstRt, addMat);
        CopyRtToTexture2D(dstRt, dstTex);
        dstTex.Apply();

        AssetDatabase.SaveAssets();
    }

    public void ReLoadLayer(float scale)
    {
        this.CheckOrInitData();

        float targetHeight = 1;// TerrainExpandConfig.Instance.BrashTargetHeight / terrain.terrainData.size.y;

        var addMaterial = TerrainExpandTools.GetHeightmapBlitExtMat();

        addMaterial.SetFloat("_Height_Offset", 0.0f);
        addMaterial.SetFloat("_Height_Scale", scale);
        addMaterial.SetFloat("_Target_Height", targetHeight);
        addMaterial.EnableKeyword("_HEIGHT_TYPE");
        addMaterial.DisableKeyword("_HOLE_TYPE");

        float[,] heights = new float[this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height];

        RenderTexture rtTmp1 = RenderTexture.GetTemporary(this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height, 0, RenderTextureFormat.ARGBHalf);
        RenderTexture rtTmp2 = RenderTexture.GetTemporary(this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height, 0, RenderTextureFormat.ARGBHalf);

        Graphics.Blit(Texture2D.blackTexture, rtTmp1);

        List<RenderTexture> allHeightMap = ListPool<RenderTexture>.Get();

        if (TerrainExpandConfig.Instance.ShowBaseLayer)
        {
            allHeightMap.Add(this.baseLayerRt);
        }

        if (TerrainExpandConfig.Instance.ShowHoudiniLayer)
        {
            allHeightMap.Add(this.importLayerRt);
        }

        for (int i = 0; i < this.heightMapList.Count; ++i)
        {
            if (!TerrainExpandConfig.Instance.HeightMaps[i].isValid || !this.heightMapList[i])
            {
                continue;
            }

            allHeightMap.Add(this.rtHeightMapList[i]);
        }

        for (int i = 0; i < allHeightMap.Count; ++i)
        {
            addMaterial.SetTexture("_Tex1", rtTmp1);
            addMaterial.SetTexture("_Tex2", allHeightMap[i]);
            int idx = this.rtHeightMapList.IndexOf(allHeightMap[i]);

            addMaterial.SetFloat("_Overlay_Layer", idx >= 0 && TerrainExpandConfig.Instance.HeightMaps[idx].isOverlay ? 1 : 0.0f);

            Graphics.Blit(null, rtTmp2, addMaterial);
            Graphics.Blit(rtTmp2, rtTmp1);
        }

        ListPool<RenderTexture>.Release(allHeightMap);

        Texture2D texTmp = new Texture2D(this.terrainData.heightmapTexture.width, this.terrainData.heightmapTexture.height, TextureFormat.RGBAHalf, false);
        CopyRtToTexture2D(rtTmp1, texTmp);
        texTmp.Apply();

        for (int y = 0; y < texTmp.height; ++y)
        {
            for (int x = 0; x < texTmp.width; ++x)
            {
                Vector4 value = texTmp.GetPixel(x, y);

                float height = value.x + value.y;

                heights[y, x] = Mathf.Clamp(height, 0, 1);
            }
        }

        terrainData.SetHeights(0, 0, heights);

        RenderTexture.ReleaseTemporary(rtTmp1);
        RenderTexture.ReleaseTemporary(rtTmp2);
        Texture2D.DestroyImmediate(texTmp);
    }

    public void RemoveLayer(int idx)
    {
        if (idx >= this.heightMapList.Count)
        {
            return;
        }

        Texture2D waitToDelTex = this.heightMapList[idx];
        RenderTexture waitToDelRt = this.rtHeightMapList[idx];

        string path = AssetDatabase.GetAssetPath(waitToDelTex);

        AssetDatabase.DeleteAsset(path);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        GameObject.DestroyImmediate(waitToDelTex);
        RenderTexture.ReleaseTemporary(waitToDelRt);

        this.heightMapList.RemoveAt(idx);
        this.rtHeightMapList.RemoveAt(idx);
    }

    public void WriteRaw(string path, bool flipVertically, int exportLayer)
    {
        int curSelectIDx = exportLayer;
        Texture2D tex;
        if (curSelectIDx == -2)
        {
            tex = this.importLayerMap;
        }
        else if (curSelectIDx == -1)
        {
            tex = this.baseLayerMap;
        }
        else
        {
            tex = this.heightMapList[curSelectIDx];
        }

        int heightmapRes = terrainData.heightmapResolution;
        byte[] data = new byte[heightmapRes * heightmapRes * 2];

        float normalize = (1 << 16);
        for (int y = 0; y < heightmapRes; ++y)
        {
            for (int x = 0; x < heightmapRes; ++x)
            {
                int srcY = flipVertically ? heightmapRes - 1 - y : y;
                Vector4 value = tex.GetPixel(x, srcY);
                float heightFloat = value.x + value.y;
                int index = x + y * heightmapRes;

                int height = Mathf.RoundToInt(heightFloat * normalize);
                ushort compressedHeight = (ushort)Mathf.Clamp(height, 0, ushort.MaxValue);

                byte[] byteData = System.BitConverter.GetBytes(compressedHeight);

                if ((SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX) == System.BitConverter.IsLittleEndian)
                {
                    data[index * 2 + 0] = byteData[1];
                    data[index * 2 + 1] = byteData[0];
                }
                else
                {
                    data[index * 2 + 0] = byteData[0];
                    data[index * 2 + 1] = byteData[1];
                }
            }
        }

        string filename = Path.Combine(path, $"{tex.name}.r16");

        FileStream fs = new FileStream(filename, FileMode.Create);
        fs.Write(data, 0, data.Length);
        fs.Close();
    }

    public void ReadRaw(string path, bool flipVertically, int importLayer)
    {
        int heightmapRes = terrainData.heightmapResolution;

        int curSelectIDx = importLayer;
        Texture2D tex;
        RenderTexture rt;
        if (curSelectIDx == -2)
        {
            tex = this.importLayerMap;
            rt = this.importLayerRt;
        }
        else if (curSelectIDx == -1)
        {
            tex = this.baseLayerMap;
            rt = this.baseLayerRt;
        }
        else
        {
            tex = this.heightMapList[curSelectIDx];
            rt = this.rtHeightMapList[curSelectIDx];
        }

        byte[] data;
        using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)))
        {
            data = br.ReadBytes(heightmapRes * heightmapRes * 2);
            br.Close();
        }

        float normalize = 1.0F / (1 << 16);
        for (int y = 0; y < heightmapRes; ++y)
        {
            for (int x = 0; x < heightmapRes; ++x)
            {
                int index = Mathf.Clamp(x, 0, heightmapRes - 1) + Mathf.Clamp(y, 0, heightmapRes - 1) * heightmapRes;
                if ((SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX) == System.BitConverter.IsLittleEndian)
                {
                    byte temp;
                    temp = data[index * 2];
                    data[index * 2 + 0] = data[index * 2 + 1];
                    data[index * 2 + 1] = temp;
                }

                ushort compressedHeight = System.BitConverter.ToUInt16(data, index * 2);

                float height = compressedHeight * normalize;
                int destY = flipVertically ? heightmapRes - 1 - y : y;

                tex.SetPixel(destY, x, new Color(height, 0, 0, 0));
            }
        }

        tex.Apply();
        Graphics.Blit(tex, rt);

        this.ReLoadLayer(1);

        AssetDatabase.SaveAssets();
    }


    static void CopyRtToTexture2D(RenderTexture rt, Texture2D tex)
    {
        RenderTexture oldRT = RenderTexture.active;

        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);

        RenderTexture.active = oldRT;
    }



#endif
}
