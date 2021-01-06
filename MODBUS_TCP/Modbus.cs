using System;
using System.Collections.Generic;
using System.Text;

namespace MODBUS_TCP
{
    public enum FunctionCode : byte
    {
        ReadCoil,
        ReadDiscreteInputs,
        ReadHoldingRegister,
        ReadInputRegister,
        WriteSingleCoil,
        WriteSingleRegister,
        WriteMultipleCoils = 15,
        WriteMultipleRegister = 16,
    };

    public static class Modbus
    {
        private static byte[] Create_MBAPHeader(ushort PDU_Length, ref bool[] isTransactionID)
        {
            byte[] MBAPHeader_Return = new byte[6];
            ushort TransactionID;

            for (TransactionID = 0; TransactionID < isTransactionID.Length; TransactionID++)
            { if (isTransactionID[TransactionID] != true) break; }
            isTransactionID[TransactionID] = true;

            Buffer.BlockCopy(BitConverter.GetBytes(TransactionID), 0, MBAPHeader_Return, 0, 2);
            Buffer.BlockCopy(new byte[2] { 0, 0 }, 0, MBAPHeader_Return, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(PDU_Length), 0, MBAPHeader_Return, 4, 2);

            return (byte[])MBAPHeader_Return.Clone();
        }

        private static byte[] Create_PDU(byte slaveID, FunctionCode functionCode, ushort startADDr, ushort count)
        {
            byte[] PDU_Return = new byte[6];
            PDU_Return[0] = slaveID;
            PDU_Return[1] = (byte)functionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(startADDr), 0, PDU_Return, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(count), 0, PDU_Return, 4, 2);

            return (byte[])PDU_Return.Clone();
        }

        private static byte[] Create_PDU(byte slaveID, FunctionCode functionCode, ushort startADDr, ushort count, ushort dataCount, byte[] data)
        {
            byte[] PDU_Return = new byte[8 + data.Length];
            PDU_Return[0] = slaveID;
            PDU_Return[1] = (byte)functionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(startADDr), 0, PDU_Return, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(count), 0, PDU_Return, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(dataCount), 0, PDU_Return, 6, 2);
            Buffer.BlockCopy(data, 0, PDU_Return, 8, data.Length);

            return (byte[])PDU_Return.Clone();
        }

        public static byte[] Protocol(ref bool[] isTransactionID, byte slaveID, FunctionCode functionCode, ushort startADDr, ushort count)
        {
            byte[] PDU = Create_PDU(slaveID, functionCode, startADDr, count);
            byte[] MBAPHeader = Create_MBAPHeader((ushort)PDU.Length, ref isTransactionID);

            byte[] Protocol_Return = new byte[MBAPHeader.Length + PDU.Length];
            Buffer.BlockCopy(MBAPHeader, 0, Protocol_Return, 0, MBAPHeader.Length);
            Buffer.BlockCopy(PDU, 0, Protocol_Return, MBAPHeader.Length, PDU.Length);

            return (byte[])Protocol_Return.Clone();
        }

        public static byte[] Protocol(ref bool[] isTransactionID, byte slaveID, FunctionCode functionCode, ushort startADDr, ushort count, ushort dataCount, byte[] data)
        {
            byte[] PDU = Create_PDU(slaveID, functionCode, startADDr, count, dataCount, data);
            byte[] MBAPHeader = Create_MBAPHeader((ushort)PDU.Length, ref isTransactionID);

            byte[] Protocol_Return = new byte[MBAPHeader.Length + PDU.Length];
            Buffer.BlockCopy(MBAPHeader, 0, Protocol_Return, 0, MBAPHeader.Length);
            Buffer.BlockCopy(PDU, 0, Protocol_Return, MBAPHeader.Length, PDU.Length);

            return (byte[])Protocol_Return.Clone();
        }
    }
}
