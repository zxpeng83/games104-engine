using G104.Engine.Assets;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;
using System.Text;

namespace G104.Engine.Rendering;

/// <summary>独占一个GL程序并缓存uniform位置；创建、使用和释放均要求调用线程持有有效GL上下文。</summary>
internal sealed class ShaderProgram : IDisposable
{
    /// <summary>按uniform名缓存位置，包括被优化掉而返回的-1。</summary>
    private readonly Dictionary<string, int> locations = new(StringComparer.Ordinal);
    /// <summary>本对象拥有的GL程序句柄，释放后为零。</summary>
    public int Handle { get; private set; }
    /// <summary>编译并链接资产中的顶点/片元阶段；失败释放程序，临时shader始终删除。</summary>
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
    /// <summary>读取shader并展开当前支持的两个公共include，不实现通用预处理器。</summary>
    private static string Read(AssetRoot assets, string file)
    {
        string source = File.ReadAllText(assets.Resolve("shaders/" + file));
        foreach (string include in new[] { "lighting.glsl", "material.glsl" })
            if (source.Contains($"#include \"{include}\"", StringComparison.Ordinal))
                source = source.Replace($"#include \"{include}\"", File.ReadAllText(assets.Resolve("shaders/" + include)), StringComparison.Ordinal);
        return source;
    }
    /// <summary>按UTF-8字节数提交源码；成功移交shader句柄，编译失败立即删除。</summary>
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
    /// <summary>将此程序设为当前上下文的绘制程序。</summary>
    public void Use() => GL.UseProgram(Handle);
    /// <summary>首次查询uniform位置，后续设置复用缓存。</summary>
    private int Location(string name)
    {
        if (!locations.TryGetValue(name, out int location)) locations[name] = location = GL.GetUniformLocation(Handle, name);
        return location;
    }
    /// <summary>设置整数uniform，亦用于纹理单元索引和开关。</summary>
    public void Set(string name, int value) => GL.Uniform1(Location(name), value);
    /// <summary>设置单精度标量uniform。</summary>
    public void Set(string name, float value) => GL.Uniform1(Location(name), value);
    /// <summary>设置二维向量uniform。</summary>
    public void Set(string name, Vector2 value) => GL.Uniform2(Location(name), value);
    /// <summary>设置三维向量uniform。</summary>
    public void Set(string name, Vector3 value) => GL.Uniform3(Location(name), value);
    /// <summary>设置四维向量uniform。</summary>
    public void Set(string name, Vector4 value) => GL.Uniform4(Location(name), value);
    /// <summary>将CPU行向量矩阵的原字节交给GLSL列向量约定，上传时不再转置。</summary>
    public void Set(string name, Matrix4 value)
    {
        // CPU为行向量矩阵；行字节按GLSL列主序读取即为所需转置，不能再transpose=true。
        GL.UniformMatrix4(Location(name), false, ref value);
    }
    /// <summary>在当前GL上下文删除程序；重复调用由零句柄保护。</summary>
    public void Dispose() { if (Handle != 0) GL.DeleteProgram(Handle); Handle = 0; }
}

/// <summary>独占网格VAO、顶点缓冲和索引缓冲；不拥有材质或纹理。</summary>
internal sealed class GpuMesh : IDisposable
{
    /// <summary>保存顶点布局及索引缓冲绑定的顶点数组句柄。</summary>
    public int Vao { get; private set; }
    /// <summary>vbo保存交错顶点；ebo保存32位三角形索引，均由本网格释放。</summary>
    private int vbo, ebo;
    /// <summary>一次绘制提交的索引数量，三角形数为该值除以三。</summary>
    public int IndexCount { get; }
    /// <summary>上传每顶点20个float的统一布局；部分创建失败时清理已分配句柄。</summary>
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
            // 布局依次为位置、法线、UV、切线及手性、关节索引、权重；偏移均以float计。
            int[] sizes = [3, 3, 2, 4, 4, 4]; int[] offsets = [0, 3, 6, 8, 12, 16];
            for (int i = 0; i < sizes.Length; i++) { GL.EnableVertexAttribArray(i); GL.VertexAttribPointer(i, sizes[i], VertexAttribPointerType.Float, false, 20 * sizeof(float), offsets[i] * sizeof(float)); }
            GL.BindVertexArray(0);
        }
        catch { Dispose(); throw; }
    }
    /// <summary>绑定本网格并以无符号32位索引绘制三角形。</summary>
    public void Draw() { GL.BindVertexArray(Vao); GL.DrawElements(PrimitiveType.Triangles, IndexCount, DrawElementsType.UnsignedInt, 0); }
    /// <summary>删除网格索引、顶点缓冲和VAO并清零；须在当前GL上下文调用。</summary>
    public void Dispose()
    {
        if (ebo != 0) GL.DeleteBuffer(ebo); if (vbo != 0) GL.DeleteBuffer(vbo); if (Vao != 0) GL.DeleteVertexArray(Vao);
        ebo = vbo = Vao = 0;
    }
}

/// <summary>共享解码后的GL纹理；缓存拥有句柄，调用者仅借用并在上下文线程收束引用。</summary>
/// <param name="assets">解析贴图路径的资产根，不由缓存释放。</param>
internal sealed class TextureCache(AssetRoot assets) : IDisposable
{
    /// <summary>以包含用途/色彩空间的键保存共享纹理句柄。</summary>
    private readonly Dictionary<string, int> textures = new(StringComparer.Ordinal);
    /// <summary>复制当前键集合，为候选场景准备失败后的资源回滚划定边界。</summary>
    public HashSet<string> Snapshot() => textures.Keys.ToHashSet(StringComparer.Ordinal);
    /// <summary>仅保留指定键；删除其余GL纹理，供场景回滚或活动引用收集后调用。</summary>
    public void Retain(ISet<string> keys)
    {
        foreach (string key in textures.Keys.Where(k => !keys.Contains(k)).ToArray())
        { GL.DeleteTexture(textures[key]); textures.Remove(key); }
    }
    /// <summary>按路径及sRGB用途复用文件贴图，首次请求时同步读取和上传。</summary>
    public int GetFile(string relativePath, bool srgb)
    {
        string key = relativePath + (srgb ? ":s" : ":l");
        return textures.TryGetValue(key, out int handle) ? handle : Get(key, File.ReadAllBytes(assets.Resolve(relativePath)), srgb);
    }
    /// <summary>上传或复用模型内嵌贴图，并沿用导入的采样参数。</summary>
    public int Get(string key, ModelTexture image, bool srgb) => Get(key, image.Encoded, srgb, image);
    /// <summary>解码RGBA并创建带mipmap的纹理；仅颜色用途启用sRGB，失败删除临时句柄。</summary>
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
    /// <summary>删除全部缓存纹理并清空键；须在有效当前GL上下文调用。</summary>
    public void Dispose() { foreach (int handle in textures.Values) GL.DeleteTexture(handle); textures.Clear(); }
}

/// <summary>独占同尺寸G-buffer、HDR和LDR附件；构造成功后才可替换旧目标。</summary>
internal sealed class RenderTargets : IDisposable
{
    /// <summary>渲染附件宽度，单位像素。</summary>
    public int Width { get; }
    /// <summary>渲染附件高度，单位像素。</summary>
    public int Height { get; }
    /// <summary>GBuffer为几何FBO；BaseMetal存基础色/金属度；NormalRough存世界法线/粗糙度；Depth为几何深度。</summary>
    /// <remarks>HdrBuffer/HdrColor为FP32光照目标；HdrDepth为独立深度renderbuffer；LdrBuffer/LdrColor为后处理目标。</remarks>
    public int GBuffer, BaseMetal, NormalRough, Depth, HdrBuffer, HdrColor, HdrDepth, LdrBuffer, LdrColor;
    /// <summary>创建并检查三组FBO；任一阶段失败时释放此前全部附件。</summary>
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
            // .045粗糙度GGX金属峰值配合法强光可超过半精度65504；保留能量，不提高粗糙度或截断D。
            HdrColor = Texture(width, height, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float); Attach(HdrColor, FramebufferAttachment.ColorAttachment0);
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
    /// <summary>分配最近邻、边缘夹取的空2D附件纹理；成功后由调用者接管句柄。</summary>
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
    /// <summary>把2D纹理的第零级挂到当前FBO的指定附件点。</summary>
    private static void Attach(int texture, FramebufferAttachment attachment) => GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, texture, 0);
    /// <summary>检查当前绑定FBO的完整性，并在失败消息中标明所属Pass。</summary>
    internal static void Check(string name)
    {
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete) throw new InvalidOperationException($"{name}帧缓冲不完整：{status}");
    }
    /// <summary>删除全部纹理、FBO及HDR深度renderbuffer并清零，覆盖部分构造失败。</summary>
    public void Dispose()
    {
        foreach (int texture in new[] { BaseMetal, NormalRough, Depth, HdrColor, LdrColor }) if (texture != 0) GL.DeleteTexture(texture);
        foreach (int fbo in new[] { GBuffer, HdrBuffer, LdrBuffer }) if (fbo != 0) GL.DeleteFramebuffer(fbo);
        if (HdrDepth != 0) GL.DeleteRenderbuffer(HdrDepth);
        GBuffer = BaseMetal = NormalRough = Depth = HdrBuffer = HdrColor = HdrDepth = LdrBuffer = LdrColor = 0;
    }
}
