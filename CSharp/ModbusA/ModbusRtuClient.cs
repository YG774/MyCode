using System.IO.Ports;

namespace ModbusA
{
    /*
     从机地址0用于广播，不需要回应。
     从机地址248-255,用户自定义功能。
     */
    public class ModbusRtuClient
    {
        public SerialPort SerialPort { get; }

        public bool DiscardInBufferEachTime { get; set; } = false;

        public int ReadTimeOut { get => SerialPort.ReadTimeout; set => SerialPort.ReadTimeout = value; }
        public int WriteTimeout { get => SerialPort.WriteTimeout; set => SerialPort.WriteTimeout = value; }

        byte[] _readBuffer = new byte[512];
        byte[] _writeBuffer = new byte[512];

        public ModbusRtuClient(SerialPort serialPort, int readTimeout, int writeTimeout)
        {
            ArgumentNullException.ThrowIfNull(serialPort);
            SerialPort = serialPort;
            SerialPort.ReadTimeout = readTimeout;
            SerialPort.WriteTimeout = writeTimeout;
        }

        private void EnsureReadBufferLength(int length)
        {
            if (_readBuffer.Length < length)
            {
                byte[] buffer = new byte[_readBuffer.Length * 2];
                Array.Copy(_readBuffer, 0, buffer, 0, _readBuffer.Length);
                _readBuffer = buffer;
            }
        }

        private void EnsureWriteBufferLength(int length)
        {
            if (_writeBuffer.Length < length)
            {
                byte[] buffer = new byte[_writeBuffer.Length * 2];
                Array.Copy(_writeBuffer, 0, buffer, 0, _writeBuffer.Length);
                _writeBuffer = buffer;
            }
        }

        public async Task<Memory<byte>> ReadColisBytesAsync(byte slaveAddr, int start, int count)
        {
            if (start < 0 || start > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0 || count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count));
            EnsureWriteBufferLength(8);
            byte[] sendBytes = _writeBuffer;
            sendBytes[0] = slaveAddr;
            ModbusHelper.ReadColisPdu(sendBytes.AsSpan(1, 5), start, count);
            ushort crc = ModbusHelper.CalculateCRC(sendBytes.AsSpan(0, 6));
            sendBytes[6] = (byte)(crc & 0xFF);
            sendBytes[7] = (byte)(crc >> 8);
            if (DiscardInBufferEachTime)
                SerialPort.DiscardInBuffer();
            using (CancellationTokenSource writeCts = new(WriteTimeout))
            {
                await SerialPort.BaseStream.WriteAsync(sendBytes, writeCts.Token);
            }
            int recvBufferLen = (count + 7) / 8 + 5;
            EnsureReadBufferLength(recvBufferLen);
            byte[] recvBuffer = _readBuffer;
            int recvLen = 0, l;
            while (true)
            {
                if (recvLen >= recvBufferLen)
                    throw new Exception($"received bytes length encough,but unexpected:{BitConverter.ToString(recvBuffer)}");
                using CancellationTokenSource readCts = new(ReadTimeOut);
                l = await SerialPort.BaseStream.ReadAsync(recvBuffer.AsMemory(recvLen), readCts.Token);
                recvLen += l;
                if (recvLen < 5)
                    continue;
                byte recvSlaveAddr = recvBuffer[0];
                byte recvFc = recvBuffer[1];
                if (recvSlaveAddr != slaveAddr && recvSlaveAddr != 255)
                    continue;
                if (recvFc == (byte)ModbusFunctionCode.ReadCoils)
                {
                    byte dataLen = recvBuffer[2];
                    if (recvLen < dataLen + 5)
                        continue;
                    ushort expectedCrc = ModbusHelper.CalculateCRC(recvBuffer.AsSpan(0, recvLen - 2));
                    ushort actualCrc = (ushort)(recvBuffer[recvLen - 1] << 8 | recvBuffer[recvLen - 2]);
                    if (expectedCrc == actualCrc)
                    {
                        if (dataLen != (count + 7) / 8)
                            throw new Exception("Unexpected reception length.");
                        return recvBuffer.AsMemory(3, dataLen);
                    }
                    else
                        continue;
                }
                else if (recvFc == (byte)ModbusFunctionCode.ReadCoils + 0x80)
                {
                    if (recvLen != 5)
                        continue;
                    ushort expectedCrc = ModbusHelper.CalculateCRC(recvBuffer.AsSpan(0, recvLen - 2));
                    ushort actualCrc = (ushort)(recvBuffer[recvLen - 1] << 8 | recvBuffer[recvLen - 2]);
                    if (expectedCrc == actualCrc)
                    {
                        byte exceptionCode = recvBuffer[2];
                        ModbusExceptionCode modbusExceptionCode = (ModbusExceptionCode)exceptionCode;
                        throw new Exception($"ModbusExceptionCode is {exceptionCode},{modbusExceptionCode}");
                    }
                    else
                        continue;
                }
                else
                {
                    ushort expectedCrc = ModbusHelper.CalculateCRC(recvBuffer.AsSpan(0, recvLen - 2));
                    ushort actualCrc = (ushort)(recvBuffer[recvLen - 1] << 8 | recvBuffer[recvLen - 2]);
                    if (expectedCrc == actualCrc)
                        throw new Exception("Unexcepted received function code");
                    else
                        continue;
                }
            }
        }
        public async Task<bool[]> ReadColisAsync(byte slaveAddr, int start, int count)
        {
            Memory<byte> memory = await ReadColisBytesAsync(slaveAddr, start, count);
            return ModbusHelper.CreateBoolArray(memory, count);
        }

        public async Task<Memory<byte>> ReadInputColisBytesAsync(byte slaveAddr, int start, int count)
        {
            if (start < 0 || start > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0 || count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count));
        }
    }
}
