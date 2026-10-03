using G104.Engine.Rendering;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace G104.Sandbox.Tools;

internal sealed class RenderingVerificationWindow : GameWindow
{
    private readonly LaunchOptions _options;
    public bool Failed { get; private set; }
    public RenderingVerificationWindow(LaunchOptions options) : base(GameWindowSettings.Default, new NativeWindowSettings
    {
        ClientSize = new Vector2i(256, 256), API = ContextAPI.OpenGL, APIVersion = new Version(4, 3),
        Profile = ContextProfile.Core, Flags = ContextFlags.ForwardCompatible, StartVisible = false,
        Title = "G104 V1 targeted renderer verification"
    }) { _options = options; }
    protected override void OnLoad()
    {
        base.OnLoad();
        foreach (string result in RenderingVerification.Run(_options.AssetRoot, Path.Combine(_options.UserDataRoot, "Rendering")))
        {
            Console.WriteLine(result);
            Failed |= result.StartsWith("FAIL", StringComparison.Ordinal);
        }
        Close();
    }
}
