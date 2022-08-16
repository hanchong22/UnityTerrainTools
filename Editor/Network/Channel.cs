using Microsoft.IO;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace UnityEditor.TerrainTools
{
    public class Channel
    {
        private Socket socket;
        private ITerrainEditorNetwork network;

        private readonly MemoryStream memoryStream;
        private readonly PacketParser parser;
        private readonly CircularBuffer recvBuffer = new CircularBuffer();
        private readonly CircularBuffer sendBuffer = new CircularBuffer();
        private SocketAsyncEventArgs outArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs innArgs = new SocketAsyncEventArgs();
        public IPEndPoint RemoteAddress { get; private set; }
        public Action<MemoryStream, ushort, Channel> OnRead { get; set; }
        public Action<bool> onConnected { get; set; }

        public long Id { get; private set; }

        public bool IsDisposed { get; private set; }

        private bool isConnected;
        private bool isSending;
        private bool hasError;
        private bool isClient;

        public Channel(SocketAsyncEventArgs e, Server server)
        {
            this.socket = e.AcceptSocket;
            this.network = server;


            this.socket.NoDelay = true;

            this.Id = IdGenerater.GenerateId();

            this.memoryStream = RecyclableMemoryStreamManager.Instance.GetStream("message", ushort.MaxValue);
            this.parser = new PacketParser(this.recvBuffer, this.memoryStream);
            this.outArgs.Completed += this.OnComplete;
            this.innArgs.Completed += this.OnComplete;
            this.RemoteAddress = (IPEndPoint)this.socket.RemoteEndPoint;

            this.isConnected = true;
            this.isSending = false;
            this.isClient = false;
        }

        public Channel(IPEndPoint ipEndPoint, Client c)
        {
            this.network = c;
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.NoDelay = true;

            this.memoryStream = RecyclableMemoryStreamManager.Instance.GetStream("message", ushort.MaxValue);
            this.parser = new PacketParser(this.recvBuffer, this.memoryStream);

            this.innArgs.Completed += this.OnComplete;
            this.outArgs.Completed += this.OnComplete;

            this.RemoteAddress = ipEndPoint;
            this.isConnected = false;
            this.isSending = false;
            this.isClient = true;
        }

        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
        }

        public bool HasError
        {
            get
            {
                return this.hasError;
            }
        }


        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;
            this.network?.RemoveChannel(this);
            this.socket.Close();
            this.outArgs.Dispose();
            this.innArgs.Dispose();
            this.memoryStream.Dispose();
            this.outArgs = null;
            this.innArgs = null;
            this.socket = null;
            this.OnRead = null;
            this.onConnected = null;

            this.isConnected = false;
        }



        public MemoryStream Stream
        {
            get => this.memoryStream;
        }

        public void Send(MemoryStream stream, uint iLen)
        {
            if (this.IsDisposed)
            {
                throw new Exception("this channel was distroyed, cannt send data");
            }

            this.sendBuffer.ReadFrom(stream, iLen);

            if (!this.isSending)
            {
                this.StartSend();
            }
        }

        public void Start()
        {
            if (!this.isConnected)
            {
                this.ConnectAsync(this.RemoteAddress);
                return;
            }

            this.StartRecv();
            this.StartSend();
        }

        private void OnComplete(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    OneThreadSynchronizationContext.Instance.Post(this.OnConnectComplete, e);
                    break;
                case SocketAsyncOperation.Receive:
                    OneThreadSynchronizationContext.Instance.Post(this.OnRecvComplete, e);
                    break;
                case SocketAsyncOperation.Send:
                    OneThreadSynchronizationContext.Instance.Post(this.OnSendComplete, e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    OneThreadSynchronizationContext.Instance.Post(this.OnDisconnectComplete, e);
                    break;
                default:
                    throw new Exception($"socket error: {e.LastOperation}");
            }
        }

        public void ConnectAsync(IPEndPoint ipEndPoint)
        {
            this.outArgs.RemoteEndPoint = ipEndPoint;
            if (this.socket.ConnectAsync(this.outArgs))
            {
                return;
            }

            this.OnConnectComplete(this.outArgs);
        }

        /// <summary>
        /// 连接后事件，仅客户端有
        /// </summary>
        /// <param name="o"></param>
        private void OnConnectComplete(object o)
        {
            if (this.socket == null)
            {
                return;
            }

            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;

            if (e.SocketError != SocketError.Success)
            {
                this.hasError = true;
                UnityEngine.Debug.LogError($"error : Channel.OnConnectComplete,{this.RemoteAddress}");
                this.onConnected?.Invoke(false);
                return;
            }

            e.RemoteEndPoint = null;
            this.isConnected = true;
            this.hasError = false;

            this.StartRecv();
            this.StartSend();
        }

        private void OnDisconnectComplete(object o)
        {
            this.isConnected = false;
            this.isSending = false;

            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;
            UnityEngine.Debug.Log($": Channel.OnDisconnectComplete,{this.RemoteAddress}");
        }

        private void StartRecv()
        {
            int size = this.recvBuffer.ChunkSize - this.recvBuffer.LastIndex;
            this.RecvAsync(this.recvBuffer.Last, this.recvBuffer.LastIndex, size);
        }

        public void RecvAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                this.innArgs.SetBuffer(buffer, offset, count);
            }
            catch (Exception e)
            {
                this.hasError = true;
                throw new Exception($"RecvAsync : socket set buffer error: {buffer.Length}, {offset}, {count}", e);
            }

            if (this.socket.ReceiveAsync(this.innArgs))
            {
                return;
            }

            OnRecvComplete(this.innArgs);
        }

        private void OnRecvComplete(object o)
        {
            if (this.socket == null)
            {
                return;
            }

            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;

            if (e.SocketError != SocketError.Success)
            {
                this.hasError = true;
                UnityEngine.Debug.LogError($"OnRecvComplete,{this.RemoteAddress},error id {(int)e.SocketError}");
                return;
            }

            if (e.BytesTransferred == 0)
            {
                this.hasError = true;
                UnityEngine.Debug.LogError($"OnRecvComplete, BytesTransferred == 0, error id:{(int)e.SocketError}");
                return;
            }

            this.recvBuffer.LastIndex += e.BytesTransferred;
            if (this.recvBuffer.LastIndex == this.recvBuffer.ChunkSize)
            {
                this.recvBuffer.AddLast();
                this.recvBuffer.LastIndex = 0;
            }

            while (true)
            {
                if (!this.parser.Parse())
                {
                    break;
                }

                MemoryStream stream = this.parser.GetPacket();
                stream.Seek(0, SeekOrigin.Begin);
                var code = BitConverter.ToUInt16(stream.GetBuffer(), Packet.OPCODE_OFFSET);

                if (code == 0 && this.isClient)
                {
                    this.Id = BitConverter.ToInt64(stream.GetBuffer(), Packet.PACK_CONTENT_OFFSET);
                    this.onConnected?.Invoke(true);
                }
                else
                {
                    try
                    {
                        this.OnRead?.Invoke(stream, code, this);
                    }
                    catch (Exception exception)
                    {
                        this.hasError = true;
                        Debug.LogError(exception.ToString());
                    }
                }
            }

            this.hasError = false;
            this.StartRecv();
        }

        private void StartSend()
        {
            if (!this.isConnected)
            {
                return;
            }

            // 没有数据需要发送
            if (this.sendBuffer.Length == 0)
            {
                this.isSending = false;
                return;
            }

            this.isSending = true;

            int sendSize = this.sendBuffer.ChunkSize - this.sendBuffer.FirstIndex;
            if (sendSize > this.sendBuffer.Length)
            {
                sendSize = (int)this.sendBuffer.Length;
            }

            this.SendAsync(this.sendBuffer.First, this.sendBuffer.FirstIndex, sendSize);
        }

        public void SendAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                this.outArgs.SetBuffer(buffer, offset, count);
            }
            catch (Exception e)
            {
                this.hasError = true;
                throw new Exception($"socket set buffer error: {buffer.Length}, {offset}, {count}", e);
            }

            if (this.socket.SendAsync(this.outArgs))
            {
                return;
            }

            this.OnSendComplete(this.outArgs);
        }

        private void OnSendComplete(object o)
        {
            if (this.socket == null)
            {
                return;
            }

            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;

            if (e.SocketError != SocketError.Success)
            {
                this.hasError = true;
                Debug.LogError($"OnSendComplete,{this.RemoteAddress},error id :{(int)e.SocketError}");
                return;
            }

            if (e.BytesTransferred == 0)
            {
                this.hasError = true;
                Debug.LogError($"OnSendComplete, BytesTransferred == 0,error id :{(int)e.SocketError}");
                return;
            }

            this.sendBuffer.FirstIndex += e.BytesTransferred;
            if (this.sendBuffer.FirstIndex == this.sendBuffer.ChunkSize)
            {
                this.sendBuffer.FirstIndex = 0;
                this.sendBuffer.RemoveFirst();
            }

            this.hasError = false;
            this.StartSend();
        }

    }
}
