using System.Text;

namespace G104.Engine.Audio;

/// <summary>保存整数PCM16交错声道样本；通过Read/Load读取时验证格式与声道。</summary>
/// <param name="Channels">声道数；Read/Load仅支持单声道或双声道。</param>
/// <param name="SampleRate">每声道每秒采样帧数，单位Hz。</param>
/// <param name="Samples">不含容器头的PCM16小端样本数据。</param>
public sealed record PcmWave(int Channels, int SampleRate, byte[] Samples)
{
    /// <summary>同步读取WAV文件，再按RIFF分块解析和验证。</summary>
    public static PcmWave Load(string path) => Read(File.ReadAllBytes(path));

    /// <summary>校验RIFF边界、PCM格式、分块对齐和完整采样帧，拒绝不支持的编码。</summary>
    public static PcmWave Read(byte[] file)
    {
        using var stream = new MemoryStream(file, writable: false);
        using var reader = new BinaryReader(stream, Encoding.ASCII);
        // 读取四字节ASCII块标识；后续块长度决定如何跳过未知内容。
        string Tag() => Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (file.Length < 12 || Tag() != "RIFF") throw new InvalidDataException("Expected RIFF/WAVE.");
        long end = checked(8L + reader.ReadUInt32());
        if (end > file.Length || end < 12 || Tag() != "WAVE") throw new InvalidDataException("Truncated WAVE.");
        int channels = 0, sampleRate = 0, blockAlign = 0;
        byte[]? samples = null;
        while (stream.Position + 8 <= end)
        {
            string tag = Tag();
            uint size = reader.ReadUInt32();
            long next = checked(stream.Position + size);
            if (next > end || size > int.MaxValue) throw new InvalidDataException("WAVE chunk exceeds RIFF.");
            if (tag == "fmt ")
            {
                if (size < 16 || channels != 0) throw new InvalidDataException("Invalid/duplicate fmt chunk.");
                int encoding = reader.ReadUInt16();
                channels = reader.ReadUInt16(); sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32(); blockAlign = reader.ReadUInt16(); int bits = reader.ReadUInt16();
                if (encoding != 1 || bits != 16 || channels is < 1 or > 2 || sampleRate is < 8000 or > 192000 ||
                    blockAlign != channels * 2 || byteRate != sampleRate * blockAlign)
                    throw new NotSupportedException("V1 requires integer PCM16, mono/stereo WAVE.");
            }
            else if (tag == "data")
            {
                if (samples is not null) throw new InvalidDataException("Duplicate data chunk.");
                samples = reader.ReadBytes((int)size);
            }
            // RIFF每个chunk按偶数字节对齐；LIST/JUNK等可跳过，不能假设固定44字节头。
            stream.Position = next + (size & 1);
            if (stream.Position > end) throw new InvalidDataException("Missing chunk alignment padding.");
        }
        if (channels == 0 || samples is null || samples.Length == 0 || samples.Length % blockAlign != 0)
            throw new InvalidDataException("Missing format/data or incomplete PCM frame.");
        return new PcmWave(channels, sampleRate, samples);
    }
}
