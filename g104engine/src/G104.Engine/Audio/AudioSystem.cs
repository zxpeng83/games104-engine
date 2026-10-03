using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace G104.Engine.Audio;

/// <summary>主线程音频上下文；共享Buffer归此作用域，Voice先解绑/删除，最后释放Buffer和Context。</summary>
public sealed class AudioSystem : IDisposable
{
    private readonly int _thread = Environment.CurrentManagedThreadId;
    private readonly Dictionary<string, (int Buffer, int Channels)> _clips = new(StringComparer.Ordinal);
    private readonly List<int> _voices = [];
    private ALDevice _device;
    private ALContext _context;
    private nint _native;
    private bool _disposed;
    public string Backend { get; private set; } = "not initialized";
    public int ActiveVoices => _voices.Count;
    public int LoadedClips => _clips.Count;

    public AudioSystem(string assetRoot)
    {
        try
        {
            string library = Path.Combine(AppContext.BaseDirectory, "OpenAL32.dll");
            _native = NativeLibrary.Load(library);
            _device = ALC.OpenDevice(null);
            if (_device.Handle == IntPtr.Zero) throw new InvalidOperationException("OpenAL device unavailable.");
            _context = ALC.CreateContext(_device, new ALContextAttributes());
            if (_context.Handle == IntPtr.Zero || !ALC.MakeContextCurrent(_context))
                throw new InvalidOperationException("OpenAL context unavailable.");
            Backend = AL.Get(ALGetString.Version) + " / " + AL.Get(ALGetString.Renderer);
            AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);
            foreach (string path in Directory.EnumerateFiles(Path.Combine(assetRoot, "audio"), "*.wav"))
            {
                PcmWave clip = PcmWave.Load(path);
                int buffer = AL.GenBuffer();
                try
                {
                    AL.BufferData(buffer, clip.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16,
                        clip.Samples, clip.SampleRate);
                    Check("upload " + path);
                    _clips.Add(Path.GetFileNameWithoutExtension(path), (buffer, clip.Channels));
                }
                catch { AL.DeleteBuffer(buffer); throw; }
            }
            Check("initialize audio");
        }
        catch { Dispose(); throw; }
    }

    public int Play(string clip, Vector3 position, bool spatial = true, bool loop = false, float gain = 0.7f)
    {
        Guard();
        if (!_clips.TryGetValue(clip, out var data)) throw new KeyNotFoundException("Unknown sound: " + clip);
        if (spatial && data.Channels != 1) throw new InvalidOperationException("3D point sources require mono PCM.");
        Collect();
        if (_voices.Count >= 32) Stop(_voices[0]);
        int source = AL.GenSource();
        try
        {
            AL.Source(source, ALSourcei.Buffer, data.Buffer);
            AL.Source(source, ALSourceb.SourceRelative, !spatial);
            AL.Source(source, ALSourceb.Looping, loop);
            AL.Source(source, ALSourcef.Gain, Math.Clamp(gain, 0, 1));
            AL.Source(source, ALSourcef.ReferenceDistance, 2);
            AL.Source(source, ALSourcef.MaxDistance, 35);
            AL.Source(source, ALSourcef.RolloffFactor, spatial ? 1 : 0);
            Vector3 location = spatial ? position : Vector3.Zero;
            AL.Source(source, ALSource3f.Position, ref location);
            AL.SourcePlay(source); Check("start voice");
            _voices.Add(source);
            return source;
        }
        catch { AL.SourceStop(source); AL.Source(source, ALSourcei.Buffer, 0); AL.DeleteSource(source); throw; }
    }

    public void SetListener(Vector3 position, Vector3 forward)
    {
        Guard();
        if (forward.LengthSquared < 1e-8f) forward = -Vector3.UnitZ;
        forward.Normalize(); var up = Vector3.UnitY;
        AL.Listener(ALListener3f.Position, ref position);
        AL.Listener(ALListenerfv.Orientation, ref forward, ref up);
        Collect(); Check("listener");
    }

    public void SetPaused(bool paused)
    {
        Guard();
        foreach (int source in _voices)
        {
            int state = AL.GetSource(source, ALGetSourcei.SourceState);
            if (paused && state == (int)ALSourceState.Playing) AL.SourcePause(source);
            if (!paused && state == (int)ALSourceState.Paused) AL.SourcePlay(source);
        }
    }

    public void Stop(int source)
    {
        Guard();
        if (!_voices.Remove(source)) return;
        AL.SourceStop(source); AL.Source(source, ALSourcei.Buffer, 0); AL.DeleteSource(source);
    }

    public void StopAll()
    {
        Guard();
        foreach (int source in _voices.ToArray()) Stop(source);
    }

    private void Collect()
    {
        foreach (int source in _voices.ToArray())
            if (AL.GetSource(source, ALGetSourcei.SourceState) == (int)ALSourceState.Stopped) Stop(source);
    }

    private static void Check(string operation)
    {
        var error = AL.GetError();
        if (error != ALError.NoError) throw new InvalidOperationException($"OpenAL {operation}: {error}");
    }

    private void Guard()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Environment.CurrentManagedThreadId != _thread) throw new InvalidOperationException("Audio context thread mismatch.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_context.Handle != IntPtr.Zero)
        {
            ALC.MakeContextCurrent(_context);
            StopAll();
            foreach (var clip in _clips.Values) AL.DeleteBuffer(clip.Buffer);
            _clips.Clear();
            ALC.MakeContextCurrent(ALContext.Null); ALC.DestroyContext(_context); _context = ALContext.Null;
        }
        if (_device.Handle != IntPtr.Zero) { ALC.CloseDevice(_device); _device = ALDevice.Null; }
        if (_native != IntPtr.Zero) { NativeLibrary.Free(_native); _native = 0; }
        _disposed = true;
    }
}
