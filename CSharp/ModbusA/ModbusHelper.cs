namespace ModbusA
{
    public class ModbusHelper
    {
        public static ushort CalculateCRC(Span<byte> span)
        {
            ushort crc = 0xFFFF;

            foreach (var value in span)
            {
                crc ^= value;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        //crc多项式 0xA001,低位先行 0x8005，高位先行
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return crc;
        }
        
        public static bool[] CreateBoolArray(Memory<byte> memory,int boolArrayLength)
        {
            Span<byte> span = memory.Span;
            bool[] result = new bool[boolArrayLength];
            for (int i = 0; i < span.Length - 1; i++)
            {
                byte b = span[i];
                int index = i * 8;
                result[index] = (b & 1) != 0;
                result[index + 1] = (b & 2) != 0;
                result[index + 2] = (b & 4) != 0;
                result[index + 3] = (b & 8) != 0;
                result[index + 4] = (b & 16) != 0;
                result[index + 5] = (b & 32) != 0;
                result[index + 6] = (b & 64) != 0;
                result[index + 7] = (b & 128) != 0;
            }
            int remainder = boolArrayLength % 8;
            if (remainder == 0)
                remainder = 8;
            int lastIndex = span.Length - 1;
            byte last = span[lastIndex];
            lastIndex *= 8;
            for (int i = 0; i < remainder; i++)
            {
                result[lastIndex + i] = (last & 1 << i) != 0;
            }
            return result;
        }

        public static void ReadDataPdu(Span<byte> span, ModbusFunctionCode modbusFunctionCode, int start, int count)
        {
            if (start < 0 || start > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0 || count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (modbusFunctionCode is not (ModbusFunctionCode.ReadCoils or ModbusFunctionCode.ReadDiscreteInputs or ModbusFunctionCode.ReadHoldingRegisters or ModbusFunctionCode.ReadInputRegisters))
                throw new NotSupportedException($"not correct readData modbusFunctionCode {modbusFunctionCode}");
            if (span.Length < 5)
                throw new ArgumentException("length not enough");
            span[0] = (byte)modbusFunctionCode;
            span[1] = (byte)(start >> 8);
            span[2] = (byte)(start & 0xFF);
            span[3] = (byte)(count >> 8);
            span[4] = (byte)(count & 0xFF);
        }

        public static void ReadColisPdu(Span<byte> span, int start, int count)
            => ReadDataPdu(span, ModbusFunctionCode.ReadCoils, start, count);

        public static void ReadDiscreteInputsPdu(Span<byte> span, int start, int count)
            => ReadDataPdu(span, ModbusFunctionCode.ReadDiscreteInputs, start, count);

        public static void ReadHoldingRegistersPdu(Span<byte> span, int start, int count)
            => ReadDataPdu(span, ModbusFunctionCode.ReadHoldingRegisters, start, count);

        public static void ReadInputRegistersPdu(Span<byte> span, int start, int count)
            => ReadDataPdu(span, ModbusFunctionCode.ReadInputRegisters, start, count);

        public static bool DetectRecv(byte slaveAddr, Span<byte> bytes)
        {
            int length = bytes.Length;
            if (length < 5)
                return false;
            byte recvSlaveAddr = bytes[0];
            byte recvFc = bytes[1];
            if (recvSlaveAddr != slaveAddr && recvSlaveAddr != 255) // 255 mean skip addr check
                return false;
            if (recvFc < 0x80)
            {
                ModbusFunctionCode modbusFunctionCode = (ModbusFunctionCode)recvFc;
                switch (modbusFunctionCode)
                {
                    // Read methods
                    case ModbusFunctionCode.ReadCoils:
                    case ModbusFunctionCode.ReadDiscreteInputs:
                    case ModbusFunctionCode.ReadHoldingRegisters:
                    case ModbusFunctionCode.ReadInputRegisters:
                    case ModbusFunctionCode.ReadWriteMultipleRegisters:
                        if (length < bytes[2] + 5)
                            return false;
                        break;

                    // Write methods
                    case ModbusFunctionCode.WriteSingleCoil:
                    case ModbusFunctionCode.WriteSingleRegister:
                    case ModbusFunctionCode.WriteMultipleCoils:
                    case ModbusFunctionCode.WriteMultipleRegisters:
                        if (length < 8)
                            return false;
                        break;
                }
            }
            ushort expectedCrc = CalculateCRC(bytes[..^2]);
            ushort actualCrc = (ushort)(bytes[^1] << 8 | bytes[^2]);
            if (expectedCrc != actualCrc)
                return false;
            else
                return true;
        }
    }
}
