namespace G104.Sandbox;

public sealed record LaunchOptions(string AssetRoot, string UserDataRoot, int Frames, bool Exercise, string? CaptureRoot, bool ContactExercise=false)
{
    public static LaunchOptions Parse(string[] args)
    {
        string assets=Path.Combine(AppContext.BaseDirectory,"assets");
        string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"G104Engine","TrainingGroundV1");
        int frames=0; bool exercise=false, isolated=false, contactExercise=false; string? captures=null;
        for(int i=0;i<args.Length;i++)
        {
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
