using System;
using System.IO;
using UnityEngine;
using UnityEngine.TerrainUtils;

namespace UnityEditor.TerrainTools
{
    internal class CreateTerrainTool : TerrainPaintTool<CreateTerrainTool>
    {
        private class Styles
        {
            public GUIContent fillHeightmapUsingNeighbors = EditorGUIUtility.TrTextContent("根据邻接地块生成高度图", "高度图与邻居的高度平滑过渡");
            public GUIContent fillAddressMode = EditorGUIUtility.TrTextContent("高度图的地址模式", "邻居高度图采样地址模式");
            public GUIContent terrainToolPropertyChange = EditorGUIUtility.TrTextContent("Terrain tool property change");
            public GUIContent enableEditor = EditorGUIUtility.TrTextContent("开启编辑", "开始编辑此地块，并锁定此地块");
            public GUIContent completeEditor = EditorGUIUtility.TrTextContent("完成编辑", "完成对编辑此地块的编辑，并解除锁定");
            public GUIContent terrainEditorServer = EditorGUIUtility.TrTextContent("地型编辑管理服务器", "服务器地址与端口");
            public GUIContent connectedState = EditorGUIUtility.TrTextContent("连接状态", "与服务器连接状态");
        }

        private enum FillAddressMode
        {
            Clamp = 0,
            Mirror = 1
        }

        private class TerrainNeighborInfo
        {
            public TerrainData terrainData;
            public Texture texture;
            public float offset;
        }

        private static Styles s_Styles;

        [SerializeField] private bool m_FillHeightmapUsingNeighbors = true;
        [SerializeField] private FillAddressMode m_FillAddressMode;
        private Material m_CrossBlendMaterial;


        private Material GetOrCreateCrossBlendMaterial()
        {
            if (m_CrossBlendMaterial == null)
                m_CrossBlendMaterial = new Material(Shader.Find("Hidden/TerrainEngine/CrossBlendNeighbors"));
            return m_CrossBlendMaterial;
        }

        public override string GetName()
        {
            return "Create Neighbor Terrains";
        }

        public override string GetDescription()
        {
            return "点击边缘创建邻接地块";
        }

        public override void OnEnable()
        {
            LoadInspectorSettings();
        }

        public override void OnDisable()
        {
            SaveInspectorSettings();
        }

        private void LoadInspectorSettings()
        {
            m_FillHeightmapUsingNeighbors = EditorPrefs.GetBool("TerrainFillHeightmapUsingNeighbors", true);
            m_FillAddressMode = (FillAddressMode)EditorPrefs.GetInt("TerrainFillAddressMode", 0);
            TerrainEditorConfig.IsLocalServer = EditorPrefs.GetBool("TerrainEditorConfig.IsLocalServer", true);
            TerrainEditorConfig.EditorServerIP = EditorPrefs.GetString("TerrainEditorConfig.EditorServerIP", "192.168.120.20");
            TerrainEditorConfig.EditorServerPort = EditorPrefs.GetInt("TerrainEditorConfig.EditorServerPort", 8081);
        }

        private void SaveInspectorSettings()
        {
            EditorPrefs.SetBool("TerrainFillHeightmapUsingNeighbors", m_FillHeightmapUsingNeighbors);
            EditorPrefs.SetInt("TerrainFillAddressMode", (int)m_FillAddressMode);
            EditorPrefs.SetBool("TerrainEditorConfig.IsLocalServer", TerrainEditorConfig.IsLocalServer);
            EditorPrefs.SetString("TerrainEditorConfig.EditorServerIP", TerrainEditorConfig.EditorServerIP);
            EditorPrefs.SetInt("TerrainEditorConfig.EditorServerPort", TerrainEditorConfig.EditorServerPort);
        }

        public override void OnInspectorGUI(Terrain terrain, UnityEditor.TerrainTools.IOnInspectorGUI editContext)
        {
            base.OnInspectorGUI(terrain, editContext);

            if (s_Styles == null)
                s_Styles = new Styles();

            EditorGUI.BeginChangeCheck();
            bool fillHeightmapUsingNeighbors = EditorGUILayout.Toggle(s_Styles.fillHeightmapUsingNeighbors, m_FillHeightmapUsingNeighbors);
            EditorGUI.BeginDisabledGroup(!fillHeightmapUsingNeighbors);
            FillAddressMode fillAddressMode = (FillAddressMode)EditorGUILayout.EnumPopup(s_Styles.fillAddressMode, m_FillAddressMode);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, s_Styles.terrainToolPropertyChange.text);

                m_FillHeightmapUsingNeighbors = fillHeightmapUsingNeighbors;
                m_FillAddressMode = fillAddressMode;
            }


            EditorGUILayout.LabelField(s_Styles.terrainEditorServer);
            EditorGUI.indentLevel++;
            if (TerrainEditorConfig.ConnectedState == NetworkStateDefine.Connected)
            {
                if (TerrainEditorConfig.IsLocalServer)
                {
                    EditorGUILayout.LabelField($"已作为服务器启动，IP:{(TerrainEditorConfig.network as Server).LocalIpAddress}");
                }
                else
                {
                    EditorGUILayout.LabelField($"已经连接到{TerrainEditorConfig.EditorServerIP}:{ TerrainEditorConfig.EditorServerPort}");
                }
            }
            else
            {
                TerrainEditorConfig.IsLocalServer = EditorGUILayout.Toggle("本地服务器或单机模式：", TerrainEditorConfig.IsLocalServer);
                if (!TerrainEditorConfig.IsLocalServer)
                {
                    TerrainEditorConfig.EditorServerIP = EditorGUILayout.TextField("IP:", TerrainEditorConfig.EditorServerIP);
                }

                TerrainEditorConfig.EditorServerPort = EditorGUILayout.IntField("Port:", TerrainEditorConfig.EditorServerPort);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(s_Styles.connectedState);
            if (TerrainEditorConfig.ConnectedState == NetworkStateDefine.Connecting)
            {
                EditorGUILayout.LabelField("正在连接...");
            }
            else if (TerrainEditorConfig.ConnectedState == NetworkStateDefine.Error)
            {
                if (GUILayout.Button("错误，请查看Console窗口"))
                {
                    TerrainEditorConfig.ConnectToServer();
                }
            }
            else
            {
                if (GUILayout.Button(TerrainEditorConfig.ConnectedState == NetworkStateDefine.Connected ? EditorGUIUtility.IconContent("lightMeter/greenLight") : EditorGUIUtility.IconContent("lightMeter/redLight"), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (TerrainEditorConfig.ConnectedState == NetworkStateDefine.None)
                    {
                        TerrainEditorConfig.ConnectToServer();
                    }
                    else if (TerrainEditorConfig.ConnectedState == NetworkStateDefine.Connected)
                    {
                        TerrainEditorConfig.DisConnect();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;

            if (TerrainEditorConfig.ConnectedState == NetworkStateDefine.Connected)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (TerrainEditorConfig.CanEditor(TerrainExpandConfig.CurrentSelectedTerrain))
                    {
                        if (GUILayout.Button(s_Styles.completeEditor))
                        {
                            TerrainEditorConfig.DisableTerrainEdit(TerrainExpandConfig.CurrentSelectedTerrain);
                        }
                    }
                    else
                    {

                        if (GUILayout.Button(s_Styles.enableEditor))
                        {
                            TerrainEditorConfig.EnableTerrainToEdit(TerrainExpandConfig.CurrentSelectedTerrain);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }


        }

        Terrain CreateNeighbor(Terrain parent, Vector3 position)
        {
            string uniqueName = "Terrain_" + position;

            if (null != GameObject.Find(uniqueName))
            {
                Debug.LogWarning("这条边已经有邻接地块了");
                return null;
            }

            TerrainData terrainData = new TerrainData();
            terrainData.baseMapResolution = parent.terrainData.baseMapResolution;
            terrainData.heightmapResolution = parent.terrainData.heightmapResolution;
            terrainData.alphamapResolution = parent.terrainData.alphamapResolution;
            if (parent.terrainData.terrainLayers != null && parent.terrainData.terrainLayers.Length > 0)
            {
                var newarray = new TerrainLayer[1];
                newarray[0] = parent.terrainData.terrainLayers[0];
                terrainData.terrainLayers = newarray;
            }
            terrainData.SetDetailResolution(parent.terrainData.detailResolution, parent.terrainData.detailResolutionPerPatch);
            terrainData.wavingGrassSpeed = parent.terrainData.wavingGrassSpeed;
            terrainData.wavingGrassAmount = parent.terrainData.wavingGrassAmount;
            terrainData.wavingGrassStrength = parent.terrainData.wavingGrassStrength;
            terrainData.wavingGrassTint = parent.terrainData.wavingGrassTint;
            terrainData.name = Guid.NewGuid().ToString();
            terrainData.size = parent.terrainData.size;
            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);

            terrainGO.name = uniqueName;
            terrainGO.transform.position = position;

            Terrain terrain = terrainGO.GetComponent<Terrain>();
            terrain.groupingID = parent.groupingID;
            terrain.drawInstanced = parent.drawInstanced;
            terrain.allowAutoConnect = parent.allowAutoConnect;
            terrain.drawTreesAndFoliage = parent.drawTreesAndFoliage;
            terrain.bakeLightProbesForTrees = parent.bakeLightProbesForTrees;
            terrain.deringLightProbesForTrees = parent.deringLightProbesForTrees;
            terrain.preserveTreePrototypeLayers = parent.preserveTreePrototypeLayers;
            terrain.detailObjectDistance = parent.detailObjectDistance;
            terrain.detailObjectDensity = parent.detailObjectDensity;
            terrain.treeDistance = parent.treeDistance;
            terrain.treeBillboardDistance = parent.treeBillboardDistance;
            terrain.treeCrossFadeLength = parent.treeCrossFadeLength;
            terrain.treeMaximumFullLODCount = parent.treeMaximumFullLODCount;

            string parentTerrainDataDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(parent.terrainData));

            var assetsToSave = new UnityEngine.Object[1 + terrainData.alphamapTextureCount];
            assetsToSave[0] = terrainData;
            for (int i = 0; i < terrainData.alphamapTextureCount; ++i)
                assetsToSave[i + 1] = terrainData.alphamapTextures[i];

            AssetDatabaseTools.CreateAssetFromObjects(assetsToSave, Path.Combine(parentTerrainDataDir, "TerrainData_" + terrainData.name + ".asset"));
            if (m_FillHeightmapUsingNeighbors)
                FillHeightmapUsingNeighbors(terrain);

            TerrainEditorConfig.AddNewTerrain(terrain);

            Undo.RegisterCreatedObjectUndo(terrainGO, "Add New neighbor");

            return terrain;
        }

        private void FillHeightmapUsingNeighbors(Terrain terrain)
        {
            UnityEngine.TerrainUtils.TerrainUtility.AutoConnect();

            Terrain[] nbrTerrains = new Terrain[4] { terrain.topNeighbor, terrain.bottomNeighbor, terrain.leftNeighbor, terrain.rightNeighbor };

            // Position of the terrain must be lowest
            Vector3 position = terrain.transform.position;
            foreach (Terrain nbrTerrain in nbrTerrains)
            {
                if (nbrTerrain)
                    position.y = Mathf.Min(position.y, nbrTerrain.transform.position.y);
            }
            terrain.transform.position = position;

            TerrainNeighborInfo top = new TerrainNeighborInfo();
            TerrainNeighborInfo bottom = new TerrainNeighborInfo();
            TerrainNeighborInfo left = new TerrainNeighborInfo();
            TerrainNeighborInfo right = new TerrainNeighborInfo();
            TerrainNeighborInfo[] nbrInfos = new TerrainNeighborInfo[4] { top, bottom, left, right };

            const float kNeightNormFactor = 2.0f;
            for (int i = 0; i < 4; ++i)
            {
                TerrainNeighborInfo nbrInfo = nbrInfos[i];
                Terrain nbrTerrain = nbrTerrains[i];
                if (nbrTerrain)
                {
                    nbrInfo.terrainData = nbrTerrain.terrainData;
                    if (nbrInfo.terrainData)
                    {
                        nbrInfo.texture = nbrInfo.terrainData.heightmapTexture;
                        nbrInfo.offset = (nbrTerrain.transform.position.y - terrain.transform.position.y) / (nbrInfo.terrainData.size.y * kNeightNormFactor);
                    }
                }
            }

            RenderTexture heightmap = terrain.terrainData.heightmapTexture;
            Vector4 texCoordOffsetScale = new Vector4(-0.5f / heightmap.width, -0.5f / heightmap.height,
                (float)heightmap.width / (heightmap.width - 1), (float)heightmap.height / (heightmap.height - 1));

            Material crossBlendMat = GetOrCreateCrossBlendMaterial();
            Vector4 slopeEnableFlags = new Vector4(bottom.texture ? 0.0f : 1.0f, top.texture ? 0.0f : 1.0f, left.texture ? 0.0f : 1.0f, right.texture ? 0.0f : 1.0f);
            crossBlendMat.SetVector("_SlopeEnableFlags", slopeEnableFlags);
            crossBlendMat.SetVector("_TexCoordOffsetScale", texCoordOffsetScale);
            crossBlendMat.SetVector("_Offsets", new Vector4(bottom.offset, top.offset, left.offset, right.offset));
            crossBlendMat.SetFloat("_AddressMode", (float)m_FillAddressMode);
            crossBlendMat.SetTexture("_TopTex", top.texture);
            crossBlendMat.SetTexture("_BottomTex", bottom.texture);
            crossBlendMat.SetTexture("_LeftTex", left.texture);
            crossBlendMat.SetTexture("_RightTex", right.texture);

            Graphics.Blit(null, heightmap, crossBlendMat);

            terrain.terrainData.DirtyHeightmapRegion(new RectInt(0, 0, heightmap.width, heightmap.height), TerrainHeightmapSyncControl.HeightAndLod);
        }

        public override void OnSceneGUI(Terrain terrain, UnityEditor.TerrainTools.IOnSceneGUI editContext)
        {
            if ((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) &&
                (Event.current.button == 2 || Event.current.alt)
                || terrain.terrainData == null)
            {
                return;
            }

            Quaternion rot = new Quaternion();
            rot.eulerAngles = new Vector3(90, 00, 0);

            Handles.color = new Color(0.9f, 1.0f, 0.8f, 1.0f);
            Vector3 size = terrain.terrainData.size;

            TerrainMap mapGroup = TerrainMap.CreateFromPlacement(terrain);
            if (mapGroup == null)
                return;

            foreach (TerrainTileCoord coord in mapGroup.terrainTiles.Keys)
            {
                int x = coord.tileX;
                int y = coord.tileZ;

                Terrain t = mapGroup.GetTerrain(x, y);

                if (t == null)
                    continue;

                Terrain left = mapGroup.GetTerrain(x - 1, y);
                Terrain right = mapGroup.GetTerrain(x + 1, y);
                Terrain top = mapGroup.GetTerrain(x, y + 1);
                Terrain bottom = mapGroup.GetTerrain(x, y - 1);

                Vector3 pos = t.transform.position + 0.5f * new Vector3(size.x, 0, size.z);

                if ((bottom == null) && Handles.Button(pos + new Vector3(0, 0, -size.z), rot, 0.5f * size.x, 0.5f * size.x, RectangleHandleCapWorldSpace))
                    CreateNeighbor(terrain, t.transform.position + Vector3.back * size.z);
                if ((top == null) && Handles.Button(pos + new Vector3(0, 0, size.z), rot, 0.5f * size.x, 0.5f * size.x, RectangleHandleCapWorldSpace))
                    CreateNeighbor(terrain, t.transform.position + Vector3.forward * size.z);
                if ((right == null) && Handles.Button(pos + new Vector3(size.x, 0, 0), rot, 0.5f * size.x, 0.5f * size.x, RectangleHandleCapWorldSpace))
                    CreateNeighbor(terrain, t.transform.position + Vector3.right * size.x);
                if ((left == null) && Handles.Button(pos + new Vector3(-size.x, 0, 0), rot, 0.5f * size.x, 0.5f * size.x, RectangleHandleCapWorldSpace))
                    CreateNeighbor(terrain, t.transform.position + Vector3.left * size.x);
            }
        }

        #region Handles
        static Vector3[] s_RectangleHandlePointsCache = new Vector3[5];
        static void RectangleHandleCapWorldSpace(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            RectangleHandleCapWorldSpace(controlID, position, rotation, new Vector2(size, size), eventType);
        }

        static void RectangleHandleCapWorldSpace(int controlID, Vector3 position, Quaternion rotation, Vector2 size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, DistanceToRectangleInternalWorldSpace(position, rotation, size));
                    break;
                case (EventType.Repaint):
                    Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
                    Vector3 up = rotation * new Vector3(0, size.y, 0);
                    s_RectangleHandlePointsCache[0] = position + sideways + up;
                    s_RectangleHandlePointsCache[1] = position + sideways - up;
                    s_RectangleHandlePointsCache[2] = position - sideways - up;
                    s_RectangleHandlePointsCache[3] = position - sideways + up;
                    s_RectangleHandlePointsCache[4] = position + sideways + up;
                    Handles.DrawPolyLine(s_RectangleHandlePointsCache);
                    break;
            }
        }

        static float DistanceToRectangleInternalWorldSpace(Vector3 position, Quaternion rotation, Vector2 size)
        {
            Quaternion invRotation = Quaternion.Inverse(rotation);

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            ray.origin = invRotation * (ray.origin - position);
            ray.direction = invRotation * ray.direction;

            Plane plane = new Plane(Vector3.forward, Vector3.zero);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                Vector3 d = new Vector3(
                    Mathf.Max(Mathf.Abs(hitPoint.x) - size.x, 0.0f) * Mathf.Sign(hitPoint.x),
                    Mathf.Max(Mathf.Abs(hitPoint.y) - size.y, 0.0f) * Mathf.Sign(hitPoint.y),
                    0.0f);

                Vector3 nearestPoint = hitPoint - d;

                hitPoint = rotation * hitPoint + position;
                nearestPoint = rotation * nearestPoint + position;

                return Vector2.Distance(HandleUtility.WorldToGUIPoint(hitPoint), HandleUtility.WorldToGUIPoint(nearestPoint));
            }

            return float.PositiveInfinity;
        }
        #endregion
    }
}
