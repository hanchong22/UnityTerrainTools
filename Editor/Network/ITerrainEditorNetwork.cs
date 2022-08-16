using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityEditor.TerrainTools
{
    public enum NetworkStateDefine
    {
        None = 0,
        Connecting = 1,
        Connected = 2,
        Error = 3,
    }

    public interface ITerrainEditorNetwork
    {
        long Id { get; }
        NetworkStateDefine NetworkState { get; }
        void Start(Action<MemoryStream, ushort, Channel> onRead, Action<bool> onConnected);
        void SendBinary(ushort opcode, Channel c, Action<BinByteBufWriter> write_handler);
        void RemoveChannel(Channel c);
        void Dispose();
    }
}
