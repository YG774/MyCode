namespace ModbusA
{
    public enum ModbusFunctionCode : byte
    {
        ReadCoils = 0x01,
        ReadDiscreteInputs = 0x02,
        ReadHoldingRegisters = 0x03,
        ReadInputRegisters = 0x04,
        WriteSingleCoil = 0x05,
        WriteSingleRegister = 0x06,
        WriteMultipleCoils = 0x0F,
        WriteMultipleRegisters = 0x10,
        ReadWriteMultipleRegisters = 0x17,
        ReadExceptionStatus = 0x07,
        Diagnostics = 0x08,
        GetComEventCounter = 0x0B,
        GetComEventLog = 0x0C,
        ReportSlaveID = 0x11,
        ReadFileRecord = 0x14,
        WriteFileRecord = 0x15,
        MaskWriteRegister = 0x16,
        ReadFIFOQueue = 0x18,
        EncapsulatedInterfaceTransport = 0x2B,
    }
}
