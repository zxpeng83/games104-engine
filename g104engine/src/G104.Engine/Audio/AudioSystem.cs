using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace G104.Engine.Audio;

/// <summary>主线程音频上下文；共享Buffer归此作用域，Voice先解绑/删除，最后释放Buffer和Context。</summary>
public sealed class AudioSystem : IDisposable
{
    /// <summary>创建音频作用域的托管线程标识，用于约束上下文调用。</summary>
    private readonly int _thread = Environment.CurrentManagedThreadId;
    /// <summary>按音效文件名共享PCM缓冲及声道数，直到音频系统释放。</summary>
    private readonly Dictionary<string, (int Buffer, int Channels)> _clips = new(StringComparer.Ordinal);
    /// <summary>按登记顺序保存独立声源；满额时淘汰最早登记项。</summary>
    private readonly List<int> _voices = [];
    /// <summary>本系统打开并最终关闭的OpenAL设备。</summary>
    private ALDevice _device;
    /// <summary>绑定创建线程的音频上下文；所有声源和缓冲在其内创建。</summary>
    private ALContext _context;
    /// <summary>显式加载的OpenAL32.dll句柄，设备/上下文结束后才卸载。</summary>
    private nint _native;
    /// <summary>正常释放完成后的幂等标记。</summary>
    private bool _disposed;
    /// <summary>初始化成功后报告实际OpenAL版本和渲染后端。</summary>
    public string Backend { get; private set; } = "not initialized";
    /// <summary>当前登记的声源数量，停止项在收集时移除。</summary>
    public int ActiveVoices => _voices.Count;
    /// <summary>已载入并共享的PCM缓冲数量。</summary>
    public int LoadedClips => _clips.Count;

    /// <summary>在当前线程加载原生库、设备/上下文和audio目录WAV；初始化失败清理已取得资源。</summary>
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

    /// <summary>创建独立Voice并借用共享Clip；3D源仅接受单声道，成功返回由本系统管理的声源句柄。</summary>
    /// <param name="position">3D声源世界坐标，单位米；非空间音效使用监听者相对原点。</param>
    public int Play(string clip, Vector3 position, bool spatial = true, bool loop = false, float gain = 0.7f)
    {
        Guard();
        if (!_clips.TryGetValue(clip, out var data)) throw new KeyNotFoundException("Unknown sound: " + clip);
        if (spatial && data.Channels != 1) throw new InvalidOperationException("3D point sources require mono PCM.");
        Collect();
        // 容量策略只按登记先后淘汰，不实现分类优先级或距离排序。
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

    /// <summary>用显示相机的世界位置和前向更新监听者，固定+Y向上，并回收已结束Voice。</summary>
    public void SetListener(Vector3 position, Vector3 forward)
    {
        Guard();
        if (forward.LengthSquared < 1e-8f) forward = -Vector3.UnitZ;
        forward.Normalize(); var up = Vector3.UnitY;
        AL.Listener(ALListener3f.Position, ref position);
        AL.Listener(ALListenerfv.Orientation, ref forward, ref up);
        Collect(); Check("listener");
    }

    /// <summary>暂停正在播放的Voice，或恢复当前暂停的Voice，不重建共享缓冲。</summary>
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

    /// <summary>移除登记声源并先停播/解绑缓冲再删除；未知句柄不作处理。</summary>
    public void Stop(int source)
    {
        Guard();
        if (!_voices.Remove(source)) return;
        AL.SourceStop(source); AL.Source(source, ALSourcei.Buffer, 0); AL.DeleteSource(source);
    }

    /// <summary>停止全部Voice；Clip缓冲仍保留供后续播放复用。</summary>
    public void StopAll()
    {
        Guard();
        foreach (int source in _voices.ToArray()) Stop(source);
    }

    /// <summary>从副本遍历登记项，回收OpenAL已经报告停止的声源。</summary>
    private void Collect()
    {
        foreach (int source in _voices.ToArray())
            if (AL.GetSource(source, ALGetSourcei.SourceState) == (int)ALSourceState.Stopped) Stop(source);
    }

    /// <summary>消费OpenAL错误状态，并附上正在执行的操作以便定位失败。</summary>
    private static void Check(string operation)
    {
        var error = AL.GetError();
        if (error != ALError.NoError) throw new InvalidOperationException($"OpenAL {operation}: {error}");
    }

    /// <summary>拒绝已释放对象或非创建线程的音频调用；调用者仍须保持正确上下文。</summary>
    private void Guard()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Environment.CurrentManagedThreadId != _thread) throw new InvalidOperationException("Audio context thread mismatch.");
    }

    /// <summary>在创建线程依次清Voice、Buffer、Context、Device和库句柄，也用于构造失败清理。</summary>
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
