using System;
using System.IO;
using System.Net;
using UnityEngine;

namespace UnityEditor.TerrainTools
{
    public class Client : ITerrainEditorNetwork
    {
        private Channel channel;
        private IPEndPoint ipEndPoint;

        private Action<MemoryStream, ushort, Channel> onReadCallback;
        private Action<bool> onConnectedCallback;

        public long Id
        {
            get
            {
                if (channel == null)
                {
                    return 0;
                }

                return this.channel.Id;
            }
        }
        public Client(string ip, int port)
        {
            IPAddress[] address = Dns.GetHostAddresses(ip);
            this.ipEndPoint = new IPEndPoint(address[0], port);

            this.channel = new Channel(this.ipEndPoint, this);
        }

        public void Start(Action<MemoryStream, ushort, Channel> onRead, Action<bool> onConnected)
        {
            this.onReadCallback = onRead;
            this.onConnectedCallback = onConnected;

            this.channel.OnRead = this.onReadCallback;
            this.channel.onConnected = this.onConnectedCallback;
            this.channel.Start();
        }

        public void SendBinary(ushort opcode, Channel c, Action<BinByteBufWriter> write_handler)
        {
            if (c == null)
            {
                c = this.channel;
            }

            using (BinByteBufWriter bw = new BinByteBufWriter(this.channel.Stream))
            {
                bw.WriteInt32(0);               //length
                bw.WriteUInt16((UInt16)opcode);

                write_handler(bw);

                uint iLen = (uint)bw.GetLength();
                bw.SetUInt32(0, iLen - 4);
                bw.GetStream().Position = 0;
                this.channel.Send(bw.GetStream(), iLen);
            }
        }


        public void Dispose()
        {
            if (this.channel != null)
            {
                this.channel.Dispose();
            }

            this.onReadCallback = null;
            this.onConnectedCallback = null;
            this.channel = null;
        }

        public void RemoveChannel(Channel c)
        {
            if (this.channel == c)
            {
                this.channel = null;
            }
        }

        public NetworkStateDefine NetworkState
        {
            get
            {
                if (this.channel == null)
                {
                    return NetworkStateDefine.None;
                }

                if (this.channel.HasError)
                {
                    return NetworkStateDefine.Error;
                }

                if (this.channel.IsConnected)
                {
                    return NetworkStateDefine.Connected;
                }
                else
                {
                    return NetworkStateDefine.Connecting;
                }
            }

        }
    }
}
