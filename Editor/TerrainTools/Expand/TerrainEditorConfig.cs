using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;
using System.Net.Sockets;
using UnityEditor.TerrainTools;
using UnityEditor;
using System.IO;

public static class TerrainEditorConfig
{
    public class TerrainState
    {
        public long seccionID;
        public int isEditing = 0;       // = 0未标记， =1 请求中 =2已标记
        public string demo = "";
    }

    public static bool IsLocalServer = false;
    public static string EditorServerIP = "192.168.120.20";
    public static int EditorServerPort = 8081;

    public static List<Terrain> CurrentOpendTerrains = new List<Terrain>();
    public static Dictionary<string, TerrainState> TerrainStates = new Dictionary<string, TerrainState>();

    public static ITerrainEditorNetwork network;

    public static bool RegUpdate;

    public static bool CanEditor(Terrain terrain)
    {
        if (TerrainEditorConfig.ConnectedState != NetworkStateDefine.Connected)
        {
            return false;
        }

        return CurrentOpendTerrains.Contains(terrain);
    }


    public static void ConnectToServer()
    {
        if (!RegUpdate)
        {
            EditorApplication.update += TerrainEditorConfig.Update;
            RegUpdate = true;
        }

        if (TerrainEditorConfig.network != null)
        {
            TerrainEditorConfig.network.Dispose();
        }

        if (IsLocalServer)
        {
            TerrainEditorConfig.network = new Server(EditorServerPort);
        }
        else
        {
            TerrainEditorConfig.network = new Client(EditorServerIP, EditorServerPort);
        }

        TerrainEditorConfig.TerrainStates.Clear();
        TerrainEditorConfig.CurrentOpendTerrains.Clear();
        TerrainEditorConfig.network.Start(TerrainEditorConfig.OnRecvData, TerrainEditorConfig.OnConnected);
    }

    public static void DisConnect()
    {
        TerrainEditorConfig.network?.Dispose();
        TerrainEditorConfig.network = null;
    }


    public static NetworkStateDefine ConnectedState
    {
        get
        {
            if (TerrainEditorConfig.network == null)
            {
                return NetworkStateDefine.None;
            }

            return TerrainEditorConfig.network.NetworkState;
        }
    }

    public static void EnableTerrainToEdit(Terrain t)
    {
        if (TerrainEditorConfig.IsLocalServer)
        {
            if (!TerrainEditorConfig.TerrainStates.TryGetValue(t.name, out var state) || state.isEditing == 0 || state.seccionID == TerrainEditorConfig.network.Id)
            {
                if (!TerrainEditorConfig.CurrentOpendTerrains.Contains(t))
                {
                    TerrainEditorConfig.CurrentOpendTerrains.Add(t);
                }

                if (state == null)
                {
                    state = new TerrainState();
                }

                state.isEditing = 2;
                state.demo = "服务器";
                state.seccionID = TerrainEditorConfig.network.Id;

                TerrainEditorConfig.TerrainStates[t.name] = state;
            }
            else
            {
                EditorUtility.DisplayDialog("失败", $"当前地块已经由{state.demo}锁定，无法启用编辑", "确定");
            }
        }
        else
        {
            if (!TerrainEditorConfig.CurrentOpendTerrains.Contains(t))
            {
                TerrainEditorConfig.TerrainStates[t.name] = new TerrainState()
                {
                    isEditing = 1,
                };

                TerrainEditorConfig.network.SendBinary(1, null, (b) =>
                 {
                     b.WriteString(t.name);
                 });

            }
        }
    }

    public static void DisableTerrainEdit(Terrain t)
    {
        if (TerrainEditorConfig.IsLocalServer)
        {
            if (CurrentOpendTerrains.Contains(t))
            {
                CurrentOpendTerrains.Remove(t);
            }

            if (TerrainStates.TryGetValue(t.name, out var state) && state.seccionID == network.Id)
            {
                state.isEditing = 0;
                state.demo = "";
            }
        }
        else
        {
            TerrainEditorConfig.network.SendBinary(2, null, (b) =>
            {
                b.WriteString(t.name);
            });
        }

    }

    public static void AddNewTerrain(Terrain t)
    {
        TerrainEditorConfig.SynchronizeTerrainsInfoToServer();
    }

    public static void SynchronizeTerrainsInfoToServer()
    {
        var terrains = TerrainExpandTools.GetAllTerrains();

    }

    private static void OnRecvData(MemoryStream stream, ushort opCode, Channel c)
    {
        var bReader = new BinByteBufReader();
        bReader.Init(stream.GetBuffer(), (int)stream.Length, Packet.PACK_CONTENT_OFFSET);

        if (opCode == 1)//启用编辑
        {
            if (TerrainEditorConfig.IsLocalServer)
            {
                var terrainName = bReader.ReadString();

                if (!TerrainEditorConfig.TerrainStates.TryGetValue(terrainName, out var state) || state.isEditing == 0 || state.seccionID == c.Id)
                {
                    if (state == null)
                    {
                        state = new TerrainState();
                    }

                    state.isEditing = 2;
                    state.demo = c.RemoteAddress.Address.ToString();
                    state.seccionID = c.Id;

                    TerrainEditorConfig.TerrainStates[terrainName] = state;

                    TerrainEditorConfig.network.SendBinary(1, c, (b) =>
                      {
                          b.WriteByte(1);
                          b.WriteString(terrainName);
                          b.WriteString("");
                      });
                }
                else
                {
                    TerrainEditorConfig.network.SendBinary(1, c, (b) =>
                    {
                        b.WriteByte(0);
                        b.WriteString(terrainName);
                        b.WriteString(state.demo);
                    });
                }
            }
            else
            {
                var state = bReader.ReadByte();
                var terrainName = bReader.ReadString();
                var demoText = bReader.ReadString();

                if (TerrainEditorConfig.TerrainStates.ContainsKey(terrainName))
                {
                    TerrainEditorConfig.TerrainStates.Remove(terrainName);
                }

                if (state == 1)
                {
                    if (TerrainExpandConfig.CurrentSelectedTerrain.name.Equals(terrainName))
                    {
                        if (!TerrainEditorConfig.CurrentOpendTerrains.Contains(TerrainExpandConfig.CurrentSelectedTerrain))
                        {
                            TerrainEditorConfig.CurrentOpendTerrains.Add(TerrainExpandConfig.CurrentSelectedTerrain);
                        }
                    }
                    else
                    {
                        foreach (var t in TerrainExpandTools.GetAllTerrains())
                        {
                            if (t.name.Equals(terrainName))
                            {
                                if (!TerrainEditorConfig.CurrentOpendTerrains.Contains(t))
                                {
                                    TerrainEditorConfig.CurrentOpendTerrains.Add(t);
                                }

                                EditorUtility.DisplayDialog("启用编辑成功", $"地块{terrainName}已启用编辑", "确定");

                                break;
                            }
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("启用编辑失败", $"地块{terrainName}已经由{demoText}锁定，无法启用编辑", "确定");
                }
            }
        }
        else if (opCode == 2)
        {
            var terrainName = bReader.ReadString();
            if (TerrainEditorConfig.IsLocalServer)
            {
                if (TerrainStates.TryGetValue(terrainName, out var state) && state.seccionID == c.Id)
                {
                    state.isEditing = 0;
                    state.demo = "";
                    state.seccionID = 0;
                }

                TerrainEditorConfig.network.SendBinary(2, c, (b) =>
                {
                    b.WriteString(terrainName);
                }
                );

            }
            else
            {
                foreach (var t in CurrentOpendTerrains)
                {
                    if (t.name.Equals(terrainName))
                    {
                        CurrentOpendTerrains.Remove(t);
                        break;
                    }
                }
            }
        }


    }

    private static void OnConnected(bool state)
    {
        if (TerrainEditorConfig.IsLocalServer)
        {
            return;
        }

        TerrainEditorConfig.SynchronizeTerrainsInfoToServer();
    }

    private static void Update()
    {
        OneThreadSynchronizationContext.Instance.Update();
    }

}

