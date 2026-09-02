using System;
using System.Runtime.InteropServices;

namespace PressureMapper.Linux.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct InputId
{
    public ushort BusType;
    public ushort Vendor;
    public ushort Product;
    public ushort Version;
}

[StructLayout(LayoutKind.Sequential)]
internal struct UinputSetup
{
    public InputId Id;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
    public byte[] Name;

    public uint FfEffectsMax;
}

[StructLayout(LayoutKind.Sequential)]
internal struct InputAbsInfo
{
    public int Value;
    public int Minimum;
    public int Maximum;
    public int Fuzz;
    public int Flat;
    public int Resolution;
}

[StructLayout(LayoutKind.Sequential)]
internal struct UinputAbsSetup
{
    public ushort Code;
    public InputAbsInfo AbsInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct InputEvent
{
    public long TvSec;
    public long TvUsec;
    public ushort Type;
    public ushort Code;
    public int Value;
}

internal static class LibC
{
    public const int O_WRONLY = 0x0001;
    public const int O_NONBLOCK = 0x0800;

    [DllImport("libc", SetLastError = true)]
    internal static extern int open(string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    internal static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    internal static extern IntPtr write(int fd, ref InputEvent buf, IntPtr count);

    [DllImport("libc", SetLastError = true, EntryPoint = "ioctl")]
    internal static extern int ioctl_int(int fd, uint request, int arg);

    [DllImport("libc", SetLastError = true, EntryPoint = "ioctl")]
    internal static extern int ioctl_setup(int fd, uint request, ref UinputSetup arg);

    [DllImport("libc", SetLastError = true, EntryPoint = "ioctl")]
    internal static extern int ioctl_abs_setup(int fd, uint request, ref UinputAbsSetup arg);

    [DllImport("libc", SetLastError = true, EntryPoint = "ioctl")]
    internal static extern int ioctl_void(int fd, uint request);
}

internal static class UinputConstants
{
    public const string DevicePath = "/dev/uinput";

    private const int IocNrBits = 8;
    private const int IocTypeBits = 8;
    private const int IocSizeBits = 14;

    private const int IocNrShift = 0;
    private const int IocTypeShift = IocNrShift + IocNrBits;
    private const int IocSizeShift = IocTypeShift + IocTypeBits;
    private const int IocDirShift = IocSizeShift + IocSizeBits;

    private const int IocNone = 0;
    private const int IocWrite = 1;

    private static uint Ioc(uint dir, uint type, uint nr, uint size) =>
        (dir << IocDirShift) | (type << IocTypeShift) | (nr << IocNrShift) | (size << IocSizeShift);

    private static uint Io(uint type, uint nr) => Ioc(IocNone, type, nr, 0);
    private static uint Iow(uint type, uint nr, uint size) => Ioc(IocWrite, type, nr, size);

    private const uint UinputIoctlBase = (uint)'U';

    public static readonly uint UI_DEV_CREATE = Io(UinputIoctlBase, 1);
    public static readonly uint UI_DEV_DESTROY = Io(UinputIoctlBase, 2);
    public static readonly uint UI_DEV_SETUP = Iow(UinputIoctlBase, 3, (uint)Marshal.SizeOf<UinputSetup>());
    public static readonly uint UI_ABS_SETUP = Iow(UinputIoctlBase, 4, (uint)Marshal.SizeOf<UinputAbsSetup>());

    public static readonly uint UI_SET_EVBIT = Iow(UinputIoctlBase, 100, sizeof(int));
    public static readonly uint UI_SET_KEYBIT = Iow(UinputIoctlBase, 101, sizeof(int));
    public static readonly uint UI_SET_ABSBIT = Iow(UinputIoctlBase, 103, sizeof(int));

    public const ushort EV_SYN = 0x00;
    public const ushort EV_KEY = 0x01;
    public const ushort EV_ABS = 0x03;
    public const ushort SYN_REPORT = 0;

    public const ushort ABS_Z = 0x02;
    public const ushort ABS_RZ = 0x05;
    public const ushort KEY_F13 = 183;
    public const ushort BUS_USB = 0x03;
}
