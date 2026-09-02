using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

#if WINDOWS
using PressureMapper.Windows;
#endif

namespace PressureMapper;

public enum ControllerSide
{
    Left,
    Right,
}

public class IVirtController
{
    public virtual void SetTrigger(ControllerSide side, float value, float max) { }
    public virtual void SendKey() { }
    public virtual void Report() { }
}

[PluginName("Pen Pressure to Roblox")]
public class ControllerMapper : IPositionedPipelineElement<IDeviceReport>
{
    private uint maxPressure = 4096;
    private readonly static IVirtController controller;

    static ControllerMapper()
    {
        #if WINDOWS
            controller = new VigEmController();
        #else
            controller = new IVirtController();
            Log.WriteNotify("PressureMapper", "No virtual controller implementation is available for the current device.", LogLevel.Warning);
        #endif
    }

    [TabletReference]
    public TabletReference TabletReference
    {
        set => maxPressure = value.Properties.Specifications.Pen.MaxPressure;
    }

    public PipelinePosition Position => PipelinePosition.PostTransform;
    public event Action<IDeviceReport>? Emit;

    public void Consume(IDeviceReport device_report)
    {
        if (device_report is ITabletReport tablet)
        {
            controller.SetTrigger(ControllerSide.Right, tablet.Pressure, maxPressure);
        }

        if (device_report is OutOfRangeReport)
        {
            controller.SetTrigger(ControllerSide.Right, 0, maxPressure);
        }

        controller.Report();
        controller.SendKey();
        Emit?.Invoke(device_report);
    }
}
