public static class GsUsbConstants
{
    // CandleLight VID/PID
    public const int Vid = 0x1D50;
    public const int Pid = 0x606F;

    // USB control transfer request types
    public const int RequestTypeHostToDevice = 0x41; // Host -> Device | Vendor | Interface
    public const int RequestTypeDeviceToHost = 0xC1; // Device -> Host | Vendor | Interface

    // gs_usb bRequest codes
    public const byte BreqBitTiming = 1;
    public const byte BreqMode = 2;
    public const byte BreqBtConst = 4;

    // gs_can_mode.mode
    public const uint CanModeReset = 0;
    public const uint CanModeStart = 1;

    // gs_can_mode.flags
    public const uint CanModeNormal = 0;
    public const uint CanModeLoopBack = 1u << 1;
    public const uint CanModeOneShot = 1u << 3;

    // Bulk endpoints
    public const int EndpointBulkIn = 0x81;
    public const int EndpointBulkOut = 0x02;

    public const uint EchoIdNone = 0xFFFFFFFF;
}
