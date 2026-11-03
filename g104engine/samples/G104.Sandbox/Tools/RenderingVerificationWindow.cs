using G104.Engine.Rendering;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace G104.Sandbox.Tools;

/// <summary>创建隐藏OpenGL 4.3窗口，为定向渲染验证提供真实当前上下文。</summary>
internal sealed class RenderingVerificationWindow : GameWindow
{
    /// <summary>提供资产路径和隔离用户数据根的已解析启动选项。</summary>
    private readonly LaunchOptions _options;
    /// <summary>记录返回结果中的显式FAIL行；验证抛出的异常仍由外层启动路径处理。</summary>
    public bool Failed { get; private set; }
    /// <summary>创建隐藏核心上下文窗口并保存启动选项，具体渲染检查在OnLoad执行。</summary>
    public RenderingVerificationWindow(LaunchOptions options) : base(GameWindowSettings.Default, new NativeWindowSettings
    {
        ClientSize = new Vector2i(256, 256), API = ContextAPI.OpenGL, APIVersion = new Version(4, 3),
        Profile = ContextProfile.Core, Flags = ContextFlags.ForwardCompatible, StartVisible = false,
        Title = "G104 V1 targeted renderer verification"
    }) { _options = options; }
    /// <summary>在窗口上下文就绪后运行定向渲染检查、输出结果并关闭窗口。</summary>
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
