using System;
using System.Net;
using System.Net.Sockets;

namespace MODBUS_TCP
{
    class Master
    {
        private static ushort _usTimeout = 500;
        private static ushort _usRefresh = 10;
        private static bool _bConnected = false;

        private Socket mSocket;
        private byte[] mBuffer = new byte[2048];

        public delegate void ResponseData(byte[] data);
        public event ResponseData OnResponseData;
        public delegate void ExceptionData(ushort id, byte unit, byte function, byte exception);
        public event ExceptionData OnException;

        public ushort usTimeout
        {
            get { return _usTimeout; }
            set { _usTimeout = value; }
        }

        public ushort usRefresh
        {
            get { return _usRefresh; }
            set { _usRefresh = value; }
        }

        public bool bConnected
        {
            get { return _bConnected; }
        }

        public Master()
        {
        }

        public Master(string ip, ushort port)
        {
            connect(ip, port);
        }

        public void Connect(string ip, ushort port)
        {
            try
            {
                IPAddress _ip;
                if (IPAddress.TryParse(ip, out _ip) == false)
                {
                    IPHostEntry hst = Dns.GetHostEntry(ip);
                    ip = hst.AddressList[0].ToString();
                }
                mSocket = new Socket(IPAddress.Parse(ip).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                mSocket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
                mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _usTimeout);
                mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _usTimeout);
                mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
                _bConnected = true;
            }
            catch (System.IO.IOException error)
            {
                _bConnected = false;
                throw (error);
            }
        }

        ~Master()
        {
            Dispose();
        }

        public void disconnect()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (mSocket != null)
            {
                if (mSocket.Connected)
                {
                    try { mSocket.Shutdown(SocketShutdown.Both); }
                    catch { }
                    mSocket.Close();
                }
                mSocket = null;
            }
        }

        internal void CallException(ushort id, byte unit, byte function, byte exception)
        {
            if (mSocket == null) return;
            if (exception == 0/* TODO : Need Connection Lost Exception Id */) mSocket == null;
            if (OnException != null) OnException(id, unit, function, exception);
        }

        private void WriteAsyncData(byte[] write_data, ushort id)
        {
            if (mSocket != null)
            {
                try
                {
                    mSocket.BeginSend(write_data, 0, write_data.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
                }
                catch (SystemException)
                {
                    CallException(id, write_data[6], write_data[7], 0/* TODO : Need Connection Lost Exception Id */);
                }
            }
            else CallException(id, write_data[6], write_data[7], 0/* TODO : Need Connection Lost Exception Id */);
        }

        private void OnSend(System.IAsyncResult result)
        {
            Int32 size = mSocket.EndSend(result);
            if (result.IsCompleted == false) CallException(0xFFFF, 0xFF, 0xFF, 0/* TODO : Need Send Fault */);
            else mSocket.BeginReceive(mBuffer, 0, mBuffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), mSocket);
        }

        private void OnReceive(System.IAsyncResult result)
        {
            if (mSocket == null) return;

            try
            {
                mSocket.EndReceive(result);
                if (result.IsCompleted == false) CallException(0xFF, 0xFF, 0xFF, 0/* TODO : Need Connection Lost Exception Id */);
            }
            catch (Exception) { }

            OnResponseData(mBuffer);
        }

        private void WriteSyncData(byte[] write_data, ushort id)
        {
            if (mSocket.Connected)
            {
                try
                {
                    mSocket.Send(write_data, 0, write_data.Length, SocketFlags.None);
                }
                catch (SystemException)
                {
                    CallException(id, write_data[6], write_data[7], 0/* TODO : Need Connection Lost Exception Id */);
                }
            }
            else CallException(id, write_data[6], write_data[7], 0/* TODO : Need Connection Lost Exception Id */);
        }
    }
}
