using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace UnityEditor.TerrainTools
{
    public class Server : ITerrainEditorNetwork
    {
        public long Id { get; private set; }

        public string LocalIpAddress { get; private set; }

        private Socket acceptor;
        private readonly SocketAsyncEventArgs networkArges = new SocketAsyncEventArgs();

        private bool hasError;

        private List<Channel> clientList = new List<Channel>();

        private Action<MemoryStream, ushort, Channel> onReadCallback;


        public Server(int port)
        {
            string host = Dns.GetHostName();
            var addressList = Dns.GetHostAddresses(host);

            this.LocalIpAddress = "";
            for (int i = 0; i < addressList.Length; ++i)
            {
                if (addressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    this.LocalIpAddress +=  addressList[i].ToString() + " ; ";
                }
            }

            this.Id = IdGenerater.GenerateId();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            this.acceptor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.acceptor.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.networkArges.Completed += this.OnComplete;
            this.acceptor.Bind(ipEndPoint);
            this.acceptor.Listen(1000);
        }

        public void Start(Action<MemoryStream, ushort, Channel> onRead, Action<bool> onConnected)
        {
            this.onReadCallback = onRead;
            this.AcceptAsync();
        }

        public void SendBinary(ushort opcode, Channel c, Action<BinByteBufWriter> write_handler)
        {
            if (c == null)
            {
                Debug.LogError($"必须指定接收的客户端");
                return;
            }

            using (BinByteBufWriter bw = new BinByteBufWriter(c.Stream))
            {
                bw.WriteInt32(0);               //length
                bw.WriteUInt16((UInt16)opcode);

                write_handler(bw);

                uint iLen = (uint)bw.GetLength();
                bw.SetUInt32(0, iLen - 4);
                bw.GetStream().Position = 0;
                c.Send(bw.GetStream(), iLen);
            }
        }

        public void AcceptAsync()
        {
            this.hasError = false;
            this.networkArges.AcceptSocket = null;
            if (this.acceptor.AcceptAsync(this.networkArges))
            {
                return;
            }

            this.OnAcceptComplete(this.networkArges);
        }

        public void Dispose()
        {
            var clients = clientList.ToArray();
            foreach (var c in clients)
            {
                c.Dispose();
            }

            this.hasError = false;
            this.clientList.Clear();
            this.acceptor?.Close();
            this.acceptor = null;
            this.networkArges.Dispose();
            this.onReadCallback = null;
        }

        private void OnComplete(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    OneThreadSynchronizationContext.Instance.Post(this.OnAcceptComplete, e);
                    break;
                default:
                    throw new Exception($"socket error: {e.LastOperation}");
            }
        }

        private void OnAcceptComplete(object o)
        {
            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;

            if (e.SocketError != SocketError.Success)
            {
                this.hasError = true;
                Debug.LogError($"accept error {e.SocketError}");
                return;
            }

            Channel c = new Channel(e, this);
            if (!this.clientList.Contains(c))
            {
                this.clientList.Add(c);
            }

            this.hasError = false;
            c.OnRead = this.onReadCallback;
            c.Start();
            this.AcceptAsync();
        }

        public void RemoveChannel(Channel c)
        {
            if (this.clientList.Contains(c))
            {
                this.clientList.Remove(c);
            }
        }


        public NetworkStateDefine NetworkState
        {
            get
            {
                if (this.acceptor == null)
                {
                    return NetworkStateDefine.None;
                }

                if (this.hasError)
                {
                    return NetworkStateDefine.Error;
                }
                else
                {
                    return NetworkStateDefine.Connected;
                }
            }
        }
    }
}
