using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace PressureMapper.Windows;

public class VigEmController : IVirtController
{
    readonly private static ViGEmClient client;
    private static IXbox360Controller controller;

    static VigEmController()
    {
        client = new();
        controller = client.CreateXbox360Controller();
        controller.AutoSubmitReport = false;
        controller.Connect();
    }

    public override void SetTrigger(ControllerSide side, float value, float max)
    {
        Xbox360Slider slider = side switch
        {
            ControllerSide.Left => Xbox360Slider.LeftTrigger,
            _ => Xbox360Slider.RightTrigger
        };

        controller.SetSliderValue(slider, (byte)(value / max * byte.MaxValue));
    }

    public override void Report()
    {
        controller.SubmitReport();
    }
}
