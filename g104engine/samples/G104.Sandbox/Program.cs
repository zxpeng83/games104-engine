using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

// 这是环境探针，只验证窗口、驱动和 OpenGL 上下文，不代表正式引擎架构。
if (Array.Exists(
        args,
        argument => string.Equals(
            argument,
            "--smoke",
            StringComparison.OrdinalIgnoreCase)))
{
    // 非交互检查只加载托管程序集，不创建窗口或 OpenGL 上下文。
    Console.WriteLine("Mode:             smoke");
    Console.WriteLine($"Framework:        {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"OS:               {RuntimeInformation.OSDescription}");
    Console.WriteLine($"Process arch:     {RuntimeInformation.ProcessArchitecture}");
    Console.WriteLine($"OpenTK.Graphics:  {GetAssemblyVersion(typeof(GL).Assembly)}");
    Console.WriteLine($"OpenTK.Windowing: {GetAssemblyVersion(typeof(GameWindow).Assembly)}");
    Console.WriteLine($"OpenTK.Math:      {GetAssemblyVersion(typeof(Vector2i).Assembly)}");
    Console.WriteLine("Smoke result:     PASS");
    return;
}
using var window = new OpenGlProbeWindow();
window.Run();

static string GetAssemblyVersion(Assembly assembly)
{
    return assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
}

internal sealed class OpenGlProbeWindow : GameWindow
{
    public OpenGlProbeWindow()
        : base(
            GameWindowSettings.Default,
            new NativeWindowSettings
            {
                ClientSize = new Vector2i(960, 540),
                Title = "G104Engine - OpenGL Environment Probe",
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 3),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.ForwardCompatible
            })
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        // OpenGL 查询必须在上下文创建并绑定到当前线程后执行。
        int major = GL.GetInteger(GetPName.MajorVersion);
        int minor = GL.GetInteger(GetPName.MinorVersion);
        int profileMask = GL.GetInteger(GetPName.ContextProfileMask);
        bool isCoreProfile =
            (profileMask & (int)ContextProfileMask.ContextCoreProfileBit) != 0;

        Console.WriteLine($"OpenGL vendor:   {GL.GetString(StringName.Vendor)}");
        Console.WriteLine($"OpenGL renderer: {GL.GetString(StringName.Renderer)}");
        Console.WriteLine($"OpenGL version:  {GL.GetString(StringName.Version)}");
        Console.WriteLine($"GLSL version:    {GL.GetString(StringName.ShadingLanguageVersion)}");
        Console.WriteLine($"Numeric version: {major}.{minor}");
        Console.WriteLine($"Core profile:    {isCoreProfile}");
        Console.WriteLine("Resize the window; press Escape or close the window to exit.");

        if (major < 4 || (major == 4 && minor < 3) || !isCoreProfile)
        {
            throw new NotSupportedException(
                $"Expected OpenGL 4.3 Core or newer, got {major}.{minor}, Core={isCoreProfile}.");
        }

        VSync = VSyncMode.On;
        GL.ClearColor(0.06f, 0.10f, 0.16f, 1.0f);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        SwapBuffers();
    }
}