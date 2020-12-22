using System;
using System.Net;
using System.Net.Sockets;

namespace MODBUS_TCP
{

    enum ExceptionCode
    {
        IllegalFunction,
        IllegalDataAdr,
        IllegalDataVal,
        SlaveDeviceFailure,
        Ack,
        SlaveIsBusy,
        GatePathUnavailable = 10,
        SendFailt = 100,
        Offset = 128,
        NotConnected = 253,
        ConnectionLost,
        Timeout
    };

    public class Master
    {
        private static ushort _usTimeout = 500;
        private static ushort _usRefresh = 10;
        private static bool _bConnected = false;

        private Socket mSocket;
        private byte[] mBuffer = new byte[2048];

        private bool[] isTransactionID = new bool[65535];

        public delegate void ReceivedData(byte[] data);
        public event ReceivedData OnReceivedData;

        public delegate void SendData(byte[] data);
        public event SendData OnSendData;

        public delegate void ExceptionData(ushort id, byte unit, byte function, ExceptionCode exception);
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
                mSocket.BeginReceive(mBuffer, 0, mBuffer.Length, SocketFlags.None, new AsyncCallback(OnReceived), mSocket);
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

        public void Disconnect()
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
                _bConnected = false;
            }
        }

        internal void CallException(ushort id, byte unit, byte function, byte exception)
        {
            if (mSocket == null) return;
            if (exception == ExceptionCode.ConnectionLost) mSocket == null;
            if (OnException != null) OnException(id, unit, function, exception);
        }

        private void WriteAsyncData(byte[] write_data)
        {
            if (mSocket != null)
            {
                try
                {
                    mSocket.BeginSend(write_data, 0, write_data.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
                }
                catch (SystemException)
                {
                    CallException(id, write_data[6], write_data[7], ExceptionCode.ConnectionLost);
                }
            }
            else CallException(id, write_data[6], write_data[7], ExceptionCode.ConnectionLost);
        }

        private void OnSend(IAsyncResult result)
        {
            if (mSocket == null) return;

            try
            {
                int size = mSocket.EndSend(result);
                if (result.IsCompleted == false) CallException(0xFFFF, 0xFF, 0xFF, ExceptionCode.SendFailt);
            }
            catch (Exception) { }

            OnSendData(mBuffer);
        }

        private void OnReceived(IAsyncResult result)
        {
            if (mSocket == null) return;

            try
            {
                mSocket.EndReceive(result);
                if (result.IsCompleted == false) CallException(0xFF, 0xFF, 0xFF, ExceptionCode.ConnectionLost);
            }
            catch (Exception) { }

            OnResponseData(mBuffer);
        }

        private void WriteData(byte[] write_data)
        {
            if (mSocket.Connected)
            {
                try
                {
                    mSocket.Send(write_data, 0, write_data.Length, SocketFlags.None);
                }
                catch (SystemException)
                {
                    CallException(id, write_data[6], write_data[7], ExceptionCode.ConnectionLost);
                }
            }
            else CallException(id, write_data[6], write_data[7], ExceptionCode.ConnectionLost);
        }
    }
}
