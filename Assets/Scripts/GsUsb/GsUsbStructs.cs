using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GsHostFrame
{
    public uint echo_id;
    public uint can_id;

    public byte can_dlc;
    public byte channel;
    public byte flags;
    public byte reserved;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] data;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GsDeviceBitTiming
{
    public uint prop_seg;
    public uint phase_seg1;
    public uint phase_seg2;
    public uint sjw;
    public uint brp;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GsDeviceMode
{
    public uint mode;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GsDeviceBtConst
{
    public uint feature;
    public uint fclk_can;

    public uint tseg1_min;
    public uint tseg1_max;

    public uint tseg2_min;
    public uint tseg2_max;

    public uint sjw_max;

    public uint brp_min;
    public uint brp_max;
    public uint brp_inc;
}
