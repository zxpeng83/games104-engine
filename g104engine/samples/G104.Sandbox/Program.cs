using System.Reflection;
using System.Runtime.InteropServices;
using G104.Engine.Animation;
using G104.Engine.Audio;
using G104.Engine.Core;
using G104.Engine.Navigation;
using G104.Engine.Physics;
using G104.Engine.Scene;
using G104.Sandbox;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

try
{
    if(args.Contains("--smoke",StringComparer.OrdinalIgnoreCase))
    {
        Console.WriteLine("Mode:             smoke");
        Console.WriteLine($"Framework:        {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"OS:               {RuntimeInformation.OSDescription}");
        Console.WriteLine($"Process arch:     {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"OpenTK.Graphics:  {VersionOf(typeof(GL).Assembly)}");
        Console.WriteLine($"OpenTK.Windowing: {VersionOf(typeof(GameWindow).Assembly)}");
        Console.WriteLine($"OpenTK.Math:      {VersionOf(typeof(Vector2i).Assembly)}");
        Console.WriteLine("Smoke result:     PASS");return 0;
    }
    var options=LaunchOptions.Parse(args);
    if(args.Contains("--verify-contacts",StringComparer.OrdinalIgnoreCase))
    {
        G104.Sandbox.Gameplay.ContactApproachVerification.Run(options.AssetRoot);
        return 0;
    }
    if(args.Contains("--verify-ui",StringComparer.OrdinalIgnoreCase))
    {
        foreach(string result in G104.Sandbox.Tools.EditorUiVerification.Run(Path.Combine(options.UserDataRoot,"ChecksUI")))
            Console.WriteLine(result);
        return 0;
    }
    if(args.Contains("--verify-render",StringComparer.OrdinalIgnoreCase))
    {
        using var verificationWindow=new G104.Sandbox.Tools.RenderingVerificationWindow(options);
        verificationWindow.Run();return verificationWindow.Failed ? 1 : 0;
    }
    if(args.Contains("--review-baseline",StringComparer.OrdinalIgnoreCase))
    {
        var checks=new (string Name,Action Check)[]
        {
            ("nonintegral navigation bounds",NavigationVerification.VerifyNonIntegralBounds),
            ("rounded navigation tail sampling",NavigationVerification.VerifyRoundedTailSampling),
            ("interior navigation maximum rounding",NavigationVerification.VerifyInteriorMaximumRounding),
            ("single-ULP navigation tail allocation",NavigationVerification.VerifySingleUlpTailAllocation),
            ("NPC initial orientation",G104.Sandbox.Gameplay.GameplayVerification.VerifyNpcFacing),
            ("NPC turning orientation",G104.Sandbox.Gameplay.GameplayVerification.VerifyNpcFacingTransition),
            ("character radius contract",G104.Sandbox.Gameplay.GameplayVerification.VerifyCharacterRadiusContract)
        };
        int failures=0;
        foreach(var check in checks)
        {
            try { check.Check();Console.WriteLine("PASS "+check.Name); }
            catch(Exception error) { failures++;Console.WriteLine("FAIL "+check.Name+": "+error.Message); }
        }
        return failures==0 ? 0 : 1;
    }
    if(args.Contains("--verify",StringComparer.OrdinalIgnoreCase))
    {
        foreach(string check in CoreSelfChecks.Run(Path.Combine(options.UserDataRoot,"Checks"))) Console.WriteLine("PASS core: "+check);
        NavigationVerification.Run();Console.WriteLine("PASS navigation: A* behavioral checks");
        PhysicsVerification.Run();Console.WriteLine("PASS physics: Jolt queries/controller/rigid-body lifecycle");
        G104.Sandbox.Gameplay.GameplayVerification.Run();Console.WriteLine("PASS gameplay: door/collision/navigation/goal events");
        foreach(string check in AnimationVerification.Run(options.AssetRoot)) Console.WriteLine("PASS animation: "+check);
        var scene=SceneSerializer.Load(Path.Combine(options.AssetRoot,"scenes","training-ground.json"),options.AssetRoot);
        using(var simulation=new G104.Sandbox.Gameplay.TrainingSimulation(scene,new SceneGraph(scene)))
        {
            for(int i=0;i<180;i++) simulation.Tick(1f/60,new G104.Sandbox.Gameplay.GameInput(Vector2.Zero,false,false,false,0));
            if(!simulation.PlayerState.Grounded) throw new InvalidOperationException("Default player did not become grounded.");
            Console.WriteLine("PASS integration: default scene prepares, NPC ticks, player grounds, resources dispose");
        }
        foreach(string path in Directory.EnumerateFiles(Path.Combine(options.AssetRoot,"audio"),"*.wav"))
        {
            var wave=PcmWave.Load(path);if(wave.Channels!=1) throw new InvalidDataException("Prepared spatial WAV should be mono.");
        }
        Console.WriteLine("PASS audio: prepared WAV chunk parsing/PCM16/mono");
        Console.WriteLine("Verification complete; GPU/audio-device/hand-feel checks remain separate.");return 0;
    }
    if(args.Contains("--verify-audio",StringComparer.OrdinalIgnoreCase))
    {
        using var audio=new AudioSystem(options.AssetRoot);
        audio.SetListener(Vector3.Zero,-Vector3.UnitZ);
        int source=audio.Play("loop-test",new Vector3(2,0,-2),true,true,.01f);
        audio.SetPaused(true);audio.SetPaused(false);audio.Stop(source);
        audio.Play("click",Vector3.Zero,false,gain:.01f);audio.StopAll();
        Console.WriteLine("PASS OpenAL actual context/buffers/2D+3D sources/pause/resume/cleanup: "+audio.Backend);
        return 0;
    }
    using var window=new TrainingWindow(options);
    try { window.Run(); }
    finally { window.DisposeResources(); }
    return 0;
}
catch(Exception error) { Console.Error.WriteLine(error);return 1; }

static string VersionOf(Assembly assembly)=>assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ??assembly.GetName().Version?.ToString()??"unknown";
