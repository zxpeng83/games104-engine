using G104.Engine.Assets;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;
using System.Text;

namespace G104.Engine.Rendering;

internal sealed class ShaderProgram : IDisposable
{
    private readonly Dictionary<string, int> locations = new(StringComparer.Ordinal);
    public int Handle { get; private set; }
    public ShaderProgram(AssetRoot assets, string vertex, string fragment)
    {
        int vs = 0, fs = 0;
        try
        {
            vs = Compile(ShaderType.VertexShader, Read(assets, vertex), vertex);
            fs = Compile(ShaderType.FragmentShader, Read(assets, fragment), fragment);
            Handle = GL.CreateProgram(); GL.AttachShader(Handle, vs); GL.AttachShader(Handle, fs); GL.LinkProgram(Handle);
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linked);
            if (linked == 0) throw new InvalidOperationException($"着色器链接失败({vertex}, {fragment})：{GL.GetProgramInfoLog(Handle)}");
        }
        catch { Dispose(); throw; }
        finally { if (vs != 0) GL.DeleteShader(vs); if (fs != 0) GL.DeleteShader(fs); }
    }
    private static string Read(AssetRoot assets, string file)
    {
        string source = File.ReadAllText(assets.Resolve("shaders/" + file));
        foreach (string include in new[] { "lighting.glsl", "material.glsl" })
            if (source.Contains($"#include \"{include}\"", StringComparison.Ordinal))
                source = source.Replace($"#include \"{include}\"", File.ReadAllText(assets.Resolve("shaders/" + include)), StringComparison.Ordinal);
        return source;
    }
    private static int Compile(ShaderType type, string source, string filename)
    {
        int shader = GL.CreateShader(type);
        try
        {
            // OpenTK 4.9.4单字符串helper传UTF16 Length，底层却UTF8编码；中文注释会截断尾部。
            // 使用显式字节长度重载，保留源码里的中文学习注释。
            int utf8Bytes = Encoding.UTF8.GetByteCount(source);
            GL.ShaderSource(shader, 1, [source], [utf8Bytes]); GL.CompileShader(shader); GL.GetShader(shader, ShaderParameter.CompileStatus, out int compiled);
            if (compiled == 0) throw new InvalidOperationException($"{type}编译失败({filename}, {source.Length} chars/{utf8Bytes} UTF8 bytes)：{GL.GetShaderInfoLog(shader)}");
            return shader;
        }
        catch { GL.DeleteShader(shader); throw; }
    }
    public void Use() => GL.UseProgram(Handle);
    private int Location(string name)
    {
        if (!locations.TryGetValue(name, out int location)) locations[name] = location = GL.GetUniformLocation(Handle, name);
        return location;
    }
    public void Set(string name, int value) => GL.Uniform1(Location(name), value);
    public void Set(string name, float value) => GL.Uniform1(Location(name), value);
    public void Set(string name, Vector2 value) => GL.Uniform2(Location(name), value);
    public void Set(string name, Vector3 value) => GL.Uniform3(Location(name), value);
    public void Set(string name, Vector4 value) => GL.Uniform4(Location(name), value);
    public void Set(string name, Matrix4 value)
    {
        // CPU为行向量矩阵；行字节按GLSL列主序读取即为所需转置，不能再transpose=true。
        GL.UniformMatrix4(Location(name), false, ref value);
    }
    public void Dispose() { if (Handle != 0) GL.DeleteProgram(Handle); Handle = 0; }
}

internal sealed class GpuMesh : IDisposable
{
    public int Vao { get; private set; }
    private int vbo, ebo;
    public int IndexCount { get; }
    public GpuMesh(float[] vertices, uint[] indices)
    {
        IndexCount = indices.Length;
        try
        {
            Vao = GL.GenVertexArray(); vbo = GL.GenBuffer(); ebo = GL.GenBuffer();
            GL.BindVertexArray(Vao); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            int[] sizes = [3, 3, 2, 4, 4, 4]; int[] offsets = [0, 3, 6, 8, 12, 16];
            for (int i = 0; i < sizes.Length; i++) { GL.EnableVertexAttribArray(i); GL.VertexAttribPointer(i, sizes[i], VertexAttribPointerType.Float, false, 20 * sizeof(float), offsets[i] * sizeof(float)); }
            GL.BindVertexArray(0);
        }
        catch { Dispose(); throw; }
    }
    public void Draw() { GL.BindVertexArray(Vao); GL.DrawElements(PrimitiveType.Triangles, IndexCount, DrawElementsType.UnsignedInt, 0); }
    public void Dispose()
    {
        if (ebo != 0) GL.DeleteBuffer(ebo); if (vbo != 0) GL.DeleteBuffer(vbo); if (Vao != 0) GL.DeleteVertexArray(Vao);
        ebo = vbo = Vao = 0;
    }
}

internal sealed class TextureCache(AssetRoot assets) : IDisposable
{
    private readonly Dictionary<string, int> textures = new(StringComparer.Ordinal);
    public HashSet<string> Snapshot() => textures.Keys.ToHashSet(StringComparer.Ordinal);
    public void Retain(ISet<string> keys)
    {
        foreach (string key in textures.Keys.Where(k => !keys.Contains(k)).ToArray())
        { GL.DeleteTexture(textures[key]); textures.Remove(key); }
    }
    public int GetFile(string relativePath, bool srgb)
    {
        string key = relativePath + (srgb ? ":s" : ":l");
        return textures.TryGetValue(key, out int handle) ? handle : Get(key, File.ReadAllBytes(assets.Resolve(relativePath)), srgb);
    }
    public int Get(string key, ModelTexture image, bool srgb) => Get(key, image.Encoded, srgb, image);
    public int Get(string key, byte[] encoded, bool srgb, ModelTexture? sampler = null)
    {
        if (textures.TryGetValue(key, out int handle)) return handle;
        var image = ImageResult.FromMemory(encoded, ColorComponents.RedGreenBlueAlpha);
        handle = GL.GenTexture();
        try
        {
            GL.BindTexture(TextureTarget.Texture2D, handle);
            // glTF图像第一个像素对应UV(0,0)，不额外翻转Y；颜色贴图由硬件sRGB解码。
            GL.TexImage2D(TextureTarget.Texture2D, 0, srgb ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, sampler?.MinFilter ?? (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, sampler?.MagFilter ?? (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, sampler?.WrapS ?? (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, sampler?.WrapT ?? (int)TextureWrapMode.Repeat);
            textures.Add(key, handle); return handle;
        }
        catch { GL.DeleteTexture(handle); throw; }
    }
    public void Dispose() { foreach (int handle in textures.Values) GL.DeleteTexture(handle); textures.Clear(); }
}

internal sealed class RenderTargets : IDisposable
{
    public int Width { get; }
    public int Height { get; }
    public int GBuffer, BaseMetal, NormalRough, Depth, HdrBuffer, HdrColor, HdrDepth, LdrBuffer, LdrColor;
    public RenderTargets(int width, int height)
    {
        Width = width; Height = height;
        try
        {
            GBuffer = GL.GenFramebuffer(); GL.BindFramebuffer(FramebufferTarget.Framebuffer, GBuffer);
            BaseMetal = Texture(width, height, PixelInternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte);
            NormalRough = Texture(width, height, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat);
            Depth = Texture(width, height, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent, PixelType.UnsignedInt);
            Attach(BaseMetal, FramebufferAttachment.ColorAttachment0); Attach(NormalRough, FramebufferAttachment.ColorAttachment1); Attach(Depth, FramebufferAttachment.DepthAttachment);
            GL.DrawBuffers(2, [DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1]); Check("G-buffer");
            HdrBuffer = GL.GenFramebuffer(); GL.BindFramebuffer(FramebufferTarget.Framebuffer, HdrBuffer);
            HdrColor = Texture(width, height, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat); Attach(HdrColor, FramebufferAttachment.ColorAttachment0);
            // HDR使用独立深度附件，Deferred光照读取G-buffer深度时不会形成纹理反馈回路。
            HdrDepth = GL.GenRenderbuffer(); GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, HdrDepth);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, HdrDepth);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0); Check("HDR");
            LdrBuffer = GL.GenFramebuffer(); GL.BindFramebuffer(FramebufferTarget.Framebuffer, LdrBuffer);
            LdrColor = Texture(width, height, PixelInternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte); Attach(LdrColor, FramebufferAttachment.ColorAttachment0);
            // FXAA沿边缘作亚像素采样，输入必须双线性；G-buffer/深度仍保留Nearest。
            GL.BindTexture(TextureTarget.Texture2D, LdrColor);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0); Check("LDR");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        catch { Dispose(); throw; }
    }
    internal static int Texture(int width, int height, PixelInternalFormat format, PixelFormat pixels, PixelType type)
    {
        int texture = GL.GenTexture();
        try
        {
            GL.BindTexture(TextureTarget.Texture2D, texture); GL.TexImage2D(TextureTarget.Texture2D, 0, format, width, height, 0, pixels, type, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            return texture;
        }
        catch { GL.DeleteTexture(texture); throw; }
    }
    private static void Attach(int texture, FramebufferAttachment attachment) => GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, texture, 0);
    internal static void Check(string name)
    {
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete) throw new InvalidOperationException($"{name}帧缓冲不完整：{status}");
    }
    public void Dispose()
    {
        foreach (int texture in new[] { BaseMetal, NormalRough, Depth, HdrColor, LdrColor }) if (texture != 0) GL.DeleteTexture(texture);
        foreach (int fbo in new[] { GBuffer, HdrBuffer, LdrBuffer }) if (fbo != 0) GL.DeleteFramebuffer(fbo);
        if (HdrDepth != 0) GL.DeleteRenderbuffer(HdrDepth);
        GBuffer = BaseMetal = NormalRough = Depth = HdrBuffer = HdrColor = HdrDepth = LdrBuffer = LdrColor = 0;
    }
}
