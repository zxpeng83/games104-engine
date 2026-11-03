namespace G104.Sandbox;

/// <summary>启动配置；自动演练必须显式指定隔离用户数据目录。</summary>
/// <param name="AssetRoot">只读运行资产根目录的完整路径。</param>
/// <param name="UserDataRoot">用户设计和隔离检查文件的根目录。</param>
/// <param name="Frames">达到此显示更新次数后退出；0表示不限制。</param>
/// <param name="Exercise">是否使用自动输入与预设演练流程。</param>
/// <param name="CaptureRoot">演练截图目录；null表示不保存截图。</param>
/// <param name="ContactExercise">是否运行窗口中的接触路线专项演练。</param>
public sealed record LaunchOptions(string AssetRoot, string UserDataRoot, int Frames, bool Exercise, string? CaptureRoot, bool ContactExercise=false)
{
    /// <summary>解析路径和演练参数并拒绝未知选项；验证模式由Program分派。</summary>
    public static LaunchOptions Parse(string[] args)
    {
        string assets=Path.Combine(AppContext.BaseDirectory,"assets");
        string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"G104Engine","TrainingGroundV1");
        int frames=0; bool exercise=false, isolated=false, contactExercise=false; string? captures=null;
        for(int i=0;i<args.Length;i++)
        {
            // 消费当前选项紧随的一个参数，并推进外层循环索引。
            string Value() => ++i<args.Length ? args[i] : throw new ArgumentException("Missing option value.");
            switch(args[i])
            {
                case "--assets": assets=Path.GetFullPath(Value()); break;
                case "--user-data-root": data=Path.GetFullPath(Value()); isolated=true; break;
                case "--frames": frames=int.Parse(Value()); if(frames<1) throw new ArgumentOutOfRangeException("frames"); break;
                case "--exercise": exercise=true; break;
                case "--exercise-contacts": exercise=true; contactExercise=true; break;
                case "--capture-root": captures=Path.GetFullPath(Value()); break;
                case "--verify": case "--smoke": case "--verify-audio": case "--verify-ui": case "--verify-ui-input": case "--verify-render": case "--review-baseline": case "--verify-contacts": break;
                default: throw new ArgumentException("Unknown option: "+args[i]);
            }
        }
        // 自动演练会操作设计和保存流程，要求显式隔离目录以保护真实存档。
        if(exercise && !isolated) throw new ArgumentException("--exercise requires explicit --user-data-root to protect saved designs.");
        if(contactExercise)
        {
            if(frames==0) frames=Tools.ContactWindowExercise.RequiredFrames;
            if(frames<Tools.ContactWindowExercise.RequiredFrames)
                throw new ArgumentException($"--exercise-contacts requires at least {Tools.ContactWindowExercise.RequiredFrames} frames.");
        }
        return new LaunchOptions(assets,data,frames,exercise,captures,contactExercise);
    }
}
