using System;
using System.Runtime.InteropServices;
using OpenTabletDriver.Plugin;
using PressureMapper.Linux.Interop;
using static PressureMapper.Linux.Interop.LibC;
using static PressureMapper.Linux.Interop.UinputConstants;

namespace PressureMapper.Linux;

public class UinputController : IVirtController
{
    private readonly int fd;
    private bool deviceCreated;

    public UinputController()
    {
        fd = open(DevicePath, O_WRONLY | O_NONBLOCK);
        if (fd < 0)
        {
            Log.WriteNotify("PressureMapper", "Failed to open /dev/uinput. Check the uinput kernel module is loaded and udev permissions are set up.", LogLevel.Error);
            return;
        }

        ioctl_int(fd, UI_SET_EVBIT, EV_ABS);
        ioctl_int(fd, UI_SET_ABSBIT, ABS_Z);
        ioctl_int(fd, UI_SET_ABSBIT, ABS_RZ);
        ioctl_int(fd, UI_SET_EVBIT, EV_KEY);
        ioctl_int(fd, UI_SET_KEYBIT, KEY_F13);

        var absZ = new UinputAbsSetup { Code = ABS_Z, AbsInfo = new InputAbsInfo { Minimum = 0, Maximum = 255 } };
        ioctl_abs_setup(fd, UI_ABS_SETUP, ref absZ);

        var absRz = new UinputAbsSetup { Code = ABS_RZ, AbsInfo = new InputAbsInfo { Minimum = 0, Maximum = 255 } };
        ioctl_abs_setup(fd, UI_ABS_SETUP, ref absRz);

        var setup = new UinputSetup
        {
            Id = new InputId { BusType = BUS_USB, Vendor = 0x045E, Product = 0x028E, Version = 1 },
            Name = ToFixedName("PressureMapper Virtual Controller"),
            FfEffectsMax = 0
        };
        ioctl_setup(fd, UI_DEV_SETUP, ref setup);

        if (ioctl_void(fd, UI_DEV_CREATE) < 0)
        {
            Log.WriteNotify("PressureMapper", "Failed to create the uinput device.", LogLevel.Error);
            return;
        }

        deviceCreated = true;
    }

    public override void SetTrigger(ControllerSide side, float value, float max)
    {
        if (!deviceCreated) return;

        ushort axis = side == ControllerSide.Left ? ABS_Z : ABS_RZ;
        int scaled = (int)(value / max * 255f);
        WriteEvent(EV_ABS, axis, scaled);
    }

    public override void SendKey()
    {
        if (!deviceCreated) return;

        WriteEvent(EV_KEY, KEY_F13, 1);
        WriteEvent(EV_KEY, KEY_F13, 0);
    }

    public override void Report()
    {
        if (!deviceCreated) return;
        WriteEvent(EV_SYN, SYN_REPORT, 0);
    }

    private void WriteEvent(ushort type, ushort code, int value)
    {
        var ev = new InputEvent { Type = type, Code = code, Value = value };
        write(fd, ref ev, (IntPtr)Marshal.SizeOf<InputEvent>());
    }

    private static byte[] ToFixedName(string name)
    {
        var buffer = new byte[80];
        var bytes = System.Text.Encoding.ASCII.GetBytes(name);
        Array.Copy(bytes, buffer, Math.Min(bytes.Length, buffer.Length - 1));
        return buffer;
    }

    ~UinputController()
    {
        if (deviceCreated)
            ioctl_void(fd, UI_DEV_DESTROY);
        if (fd >= 0)
            close(fd);
    }
}
