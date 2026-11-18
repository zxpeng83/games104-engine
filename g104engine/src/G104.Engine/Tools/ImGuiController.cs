using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using NVector2 = System.Numerics.Vector2;

namespace G104.Engine.Tools;

/// <summary>
/// 本控制器连接窗口(_window)、ImGui与OpenGL，负责转交输入和绘制UI。
/// _window是窗口对象引用，提供尺寸/输入；ImGui静态API处理控件并生成绘制数据；GL静态API提交GPU绘制。
/// UI状态保存在_context=ImGui.CreateContext()指向的原生上下文中；ImGui.SetCurrentContext()选中当前操作的一份，也可切换到另一份。
/// tips：这里绘制编辑器界面的GL和绘制游侠场景的GL是同一个。
/// </summary>
/// <remarks>
/// 窗口事件只上报原始操作；Button/InputText等控件每帧结合输入判断交互，并准备外观数据，业务动作由项目代码处理。
/// ImGui按控件ID保留交互状态，所以每帧调用控件不会忘记之前按住了哪个按钮。
/// </remarks>
/// <example>
/// 以下按流程摘录，分别属于构造函数、输入回调、窗口更新和绘制方法，并非同一个方法内的连续代码：
/// <code>
/// // 1. 初始化一次：绑定窗口、创建UI状态，并注册输入回调。
/// _window = window;                   // TrainingWindow传入的this。
/// _context = ImGui.CreateContext();
/// ImGui.SetCurrentContext(_context);
/// _window.MouseDown += OnMouseDown;
/// _window.MouseUp += OnMouseUp;
///
/// // 2. 鼠标回调：告诉ImGui在哪个坐标按下/松开，此时还未判断点中了哪个按钮。
/// ImGui.SetCurrentContext(_context);
/// var io = ImGui.GetIO();             // 当前上下文的输入/输出及配置接口。
/// io.AddMousePosEvent(_window.MousePosition.X, _window.MousePosition.Y);
/// io.AddMouseButtonEvent(index, down);// true表示按下，false表示松开。
///
/// // 3. TrainingWindow每帧更新：准备输入，再处理本帧控件；注意这里是处理界面、控件的状态，还不是渲染。
/// _ui.BeginFrame(this, dt);           // 同步尺寸、时间和键盘等状态，内部调用ImGui.NewFrame()。
/// // DrawToolbar中的代码：Button判断点击并生成按钮外观数据，返回true才安排保存。
/// if (ImGui.Button("Save design")) Enqueue(Save);
/// ProcessCommands();                 // 窗口随后执行队列中的Save等操作；按钮文字本身不会保存。
/// // 保存按钮的实际调用链（可按类名、方法名定位）：
/// // TrainingWindow.OnUpdateFrame()
/// //   ├─ ImGuiController.BeginFrame()                  准备尺寸、时间和输入
/// //   │    └─ ImGui.NewFrame()                         开始本帧UI处理
/// //   ├─ TrainingWindow.DrawToolbar()                  每帧处理工具栏控件
/// //   │    └─ ImGui.Button("Save design")              判断按钮是否被点击并生成按钮外观数据
/// //   │         └─ 返回true：TrainingWindow.Enqueue(Save)  只入队，尚未调用Save()
/// //   └─ TrainingWindow.ProcessCommands()              逐个取出队列中的操作
/// //        └─ TrainingWindow.ExecuteEditorCommand(...) 执行操作并处理异常
/// //             └─ action()                           调用取出的委托
/// //                  └─ TrainingWindow.Save()         保存设计并更新已保存状态
/// //                       └─ SceneSerializer.Save(...) 将设计写入文件
/// // Save方法作为Action委托入队；本按钮请求在同次OnUpdateFrame稍后执行，先收集控件请求再操作场景。
///
/// // 4. 窗口先用_renderer.Render绘制3D场景，再调用_ui.Render叠加UI；后者内部执行：
/// ImGui.Render();                    // ImGui库的方法：整理UI绘制数据，不执行GL绘制。
/// var data = ImGui.GetDrawData();     // 取得当前上下文的绘制数据。
/// // 本控制器用GL.BufferData上传、GL.DrawElementsBaseVertex提交GPU绘制，并处理裁剪和GL状态恢复。
/// // 因此_ui.Render()包含数据整理与实际绘制；完成后由窗口SwapBuffers提交场景和UI的完整画面。
///
/// // 对应的实际调用流程（省略专项检查及部分GL设置）：
/// // TrainingWindow.OnRenderFrame()
/// //   ├─ TrainingRenderer.Render(...)                 即_renderer.Render(...)，绘制3D场景
/// //   ├─ ImGuiController.Render()                     即_ui.Render()，叠加UI
/// //   │    ├─ ImGui.Render()                          整理UI绘制数据，尚未执行GL绘制
/// //   │    ├─ ImGui.GetDrawData()                     取得顶点、索引及绘制指令
/// //   │    ├─ 保存当前GL状态                          供绘制结束后恢复
/// //   │    ├─ GL.UseProgram / GL.BindVertexArray      选择UI的shader和顶点格式
/// //   │    ├─ GL.BufferData(...)                     上传顶点、索引数据
/// //   │    ├─ GL.Scissor / GL.BindTexture             设置当前指令的裁剪范围和纹理
/// //   │    ├─ GL.DrawElementsBaseVertex(...)          提交GPU绘制命令，使用前述shader画三角形
/// //   │    └─ finally恢复GL状态                       避免UI绘制影响后续渲染
/// //   └─ SwapBuffers()                               提交包含场景和UI的完整画面
/// // 上传/绘制可能执行多次：每个列表分别上传顶点和索引，再遍历其中的指令设置裁剪、绑定纹理并绘制。
/// </code>
/// </example>
public sealed class ImGuiController : IDisposable
{
    /// <summary>本后端创建并销毁的ImGui原生上下文；输入回调先切换到此上下文。</summary>
    private readonly nint _context;
    /// <summary>输入事件所属窗口，由外层窗口生命周期拥有。</summary>
    private readonly GameWindow _window;
    /// <summary>_vao保存UI顶点布局；_vertices/_indices为每帧流式顶点/索引缓冲。</summary>
    /// <remarks>_program为UI着色程序；_font为字体纹理，全部GL句柄由本后端在当前上下文释放。</remarks>
    private int _vao, _vertices, _indices, _program, _font;
    /// <summary>_frame标记已开帧待绘制；_disposed防止重复释放；_focused保存按事件顺序收到的焦点状态。</summary>
    private bool _frame, _disposed, _focused;
    /// <summary>ImGui当前是否捕获键盘，用于窗口决定游戏快捷键的输入归属。</summary>
    public bool WantsKeyboard => ImGui.GetIO().WantCaptureKeyboard;
    /// <summary>ImGui当前是否捕获鼠标，包括活动控件与模态窗口的捕获。</summary>
    public bool WantsMouse => ImGui.GetIO().WantCaptureMouse;

    /// <summary>绑定一个窗口，创建UI上下文及GPU资源并订阅输入；失败时解除订阅并清理已创建资源。</summary>
    public ImGuiController(GameWindow window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _focused = window.IsFocused;
        _context = ImGui.CreateContext(); ImGui.SetCurrentContext(_context);
        try
        {
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            // 禁止默认写imgui.ini到任意当前目录；面板布局由程序给出。
            unsafe { io.NativePtr->IniFilename = null; }
            ImGui.StyleColorsDark();
            var style = ImGui.GetStyle(); style.WindowRounding = 5; style.FrameRounding = 3;
            // 整段原始字符串为UI顶点shader：逻辑像素坐标经正交投影进入裁剪空间。
            int vertex = Shader(ShaderType.VertexShader, """
                #version 430 core
                layout(location=0) in vec2 position;
                layout(location=1) in vec2 uv;
                layout(location=2) in vec4 color;
                uniform mat4 projection;
                out vec2 texCoord; out vec4 tint;
                void main(){ texCoord=uv; tint=color; gl_Position=projection*vec4(position,0,1); }
                """);
            int fragment = 0;
            try
            {
                // 整段原始字符串为UI片元shader：字体/控件纹理与顶点颜色相乘。
                fragment = Shader(ShaderType.FragmentShader, """
                    #version 430 core
                    in vec2 texCoord; in vec4 tint;
                    uniform sampler2D fontTexture; out vec4 outputColor;
                    void main(){ outputColor=tint*texture(fontTexture,texCoord); }
                    """);
                _program = GL.CreateProgram(); GL.AttachShader(_program, vertex); GL.AttachShader(_program, fragment);
                GL.LinkProgram(_program); GL.GetProgram(_program, GetProgramParameterName.LinkStatus, out int linked);
                if (linked == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(_program));
            }
            finally { GL.DeleteShader(vertex); if (fragment != 0) GL.DeleteShader(fragment); }
            _vao = GL.GenVertexArray(); _vertices = GL.GenBuffer(); _indices = GL.GenBuffer();
            GL.BindVertexArray(_vao); GL.BindBuffer(BufferTarget.ArrayBuffer, _vertices);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indices);
            // ImDrawVert共20字节：float2位置、float2 UV、归一化RGBA8颜色；索引在本绑定中为16位。
            GL.EnableVertexAttribArray(0); GL.VertexAttribPointer(0,2,VertexAttribPointerType.Float,false,20,0);
            GL.EnableVertexAttribArray(1); GL.VertexAttribPointer(1,2,VertexAttribPointerType.Float,false,20,8);
            GL.EnableVertexAttribArray(2); GL.VertexAttribPointer(2,4,VertexAttribPointerType.UnsignedByte,true,20,16);
            GL.BindVertexArray(0); GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            io.Fonts.AddFontDefault();
            io.Fonts.GetTexDataAsRGBA32(out nint pixels, out int width, out int height, out _);
            _font = GL.GenTexture(); GL.BindTexture(TextureTarget.Texture2D, _font);
            GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgba8,width,height,0,
                PixelFormat.Rgba,PixelType.UnsignedByte,pixels);
            GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Linear);
            io.Fonts.SetTexID((nint)_font); io.Fonts.ClearTexData(); GL.BindTexture(TextureTarget.Texture2D,0);
            // 鼠标移动/按键/滚轮、焦点和文字输入按事件顺序提交给ImGui。
            // 鼠标事件这里是采用初始时注册的方式，而不是在 BeginFrame() 每帧判断，是因为这样的话如果在帧间快速按了又松开会检测不到。
            // 键盘也支持KeyDown/KeyUp事件，但当前项目对键盘的事件采用在 BeginFrame() 每帧判断，这种方式也会出现快速按下又松开检测不到的情况，但目前没发现有严重负面影响，所以不改。
            _window.MouseMove += OnMouseMove;
            _window.MouseDown += OnMouseDown;
            _window.MouseUp += OnMouseUp;
            _window.MouseWheel += OnMouseWheel;
            _window.FocusedChanged += OnFocusedChanged;
            _window.TextInput += OnTextInput;
            io.AddFocusEvent(_focused);
            AddMousePosition(io);
            for (int button=0; button<5; button++)
                io.AddMouseButtonEvent(button,_focused && _window.MouseState.IsButtonDown((MouseButton)button));
        }
        catch { Dispose(); throw; }
    }

    /// <summary>
    /// 处理界面的文本块的文本输入。
    /// 窗口的输入框接收输入一个字符时调用；
    /// 这里将字符信息交给ImGui处理，这里没有出现对应的输入框，是因为封装好了ImGui的输入处理机制，ImGui会根据当前活动控件的ID来决定哪个输入框接收这个字符。
    /// </summary>
    public void AddCharacter(uint codePoint) 
    { 
        ImGui.SetCurrentContext(_context); 
        ImGui.GetIO().AddInputCharacter(codePoint); 
    }

    /// <summary>
    /// 告诉 ImGui，鼠标当前在窗口的什么位置；窗口失去焦点时，告诉它这个位置已经无效，防止旧位置继续参与点击。
    /// 注意这里只是报告ImGui信息，没有修改信息。
    /// </summary>
    private void AddMousePosition(ImGuiIOPtr io)
    {
        io.AddMousePosEvent(_focused ? _window.MousePosition.X : -float.MaxValue,
                            _focused ? _window.MousePosition.Y : -float.MaxValue);
    }

    /// <summary>
    /// 和 AddMousePosition 类似，告诉 ImGui 鼠标当前在窗口的什么位置；窗口失去焦点时，告诉它这个位置已经无效，防止旧位置继续参与点击。
    /// 这里是监听并实时更新，而 AddMousePosition 是为了主动调用刷新的辅助方法。
    /// </summary>
    private void OnMouseMove(MouseMoveEventArgs e)
    {
        if (!_focused) return;
        ImGui.SetCurrentContext(_context); 
        ImGui.GetIO().AddMousePosEvent(e.X,e.Y);
    }

    /// <summary>把窗口按下事件转交到统一按钮入口。</summary>
    private void OnMouseDown(MouseButtonEventArgs e) => AddMouseButton(e.Button,true);
    /// <summary>把窗口松开事件转交到统一按钮入口，避免帧间短点击丢失。</summary>
    private void OnMouseUp(MouseButtonEventArgs e) => AddMouseButton(e.Button,false);

    /// <summary>只接收前五个鼠标按钮，并在按钮事件前提交当前位置以保留点击发生地点。</summary>
    private void AddMouseButton(MouseButton button, bool down)
    {
        int index=(int)button;
        if (index<0 || index>=5 || (down && !_focused)) return;
        ImGui.SetCurrentContext(_context);
        var io=ImGui.GetIO();
        AddMousePosition(io);
        io.AddMouseButtonEvent(index,down);
    }

    /// <summary>
    /// 鼠标滚轮滚动事件。
    /// 将有焦点时的滚轮相对增量提交一次，避免后续帧重复采样。
    /// </summary>
    private void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (!_focused) return;
        ImGui.SetCurrentContext(_context);
        var io=ImGui.GetIO(); 
        AddMousePosition(io); 
        io.AddMouseWheelEvent(e.OffsetX,e.OffsetY);
    }

    /// <summary>
    /// 窗口获得/失去焦点时触发（如Alt+Tab切出/切回），注意在当前运行窗口里从一个输入框点击到当前窗口的另一个输入框类似的情况不算；防止在别处松开鼠标后，UI仍停留在按下状态或误点击。
    /// 依事件值切换焦点；失焦先使位置无效再释放全部按钮，取消未完成点击。
    /// </summary>
    private void OnFocusedChanged(FocusedChangedEventArgs e)
    {
        // OpenTK先派发事件后更新IsFocused，回调必须读取事件值。
        _focused=e.IsFocused;
        ImGui.SetCurrentContext(_context);
        var io=ImGui.GetIO(); 
        io.AddFocusEvent(_focused);
        // 先使位置无效，再释放按钮；同一事件批次恢复焦点也不能完成旧点击。
        AddMousePosition(io);
        if (!_focused)
            for (int button=0; button<5; button++) io.AddMouseButtonEvent(button,false);
    }

    /// <summary>仅在窗口有焦点时接收OpenTK产生的文本输入。</summary>
    private void OnTextInput(TextInputEventArgs e)
    {
        if (_focused) AddCharacter((uint)e.Unicode);
    }

    /// <summary>更新逻辑尺寸、帧缓冲缩放、秒制时间步及键盘按住态，然后开UI帧。</summary>
    /// <param name="window">必须是构造时绑定的同一窗口。</param>
    /// <param name="deltaTime">显示帧间隔秒数，限制在可用范围内供ImGui消费。</param>
    public void BeginFrame(GameWindow window, float deltaTime)
    {
        if (!ReferenceEquals(window,_window)) 
            throw new ArgumentException("ImGui input belongs to its bound window.",nameof(window));
            
        ImGui.SetCurrentContext(_context);
        if (_frame) { ImGui.EndFrame(); _frame = false; }
        var io = ImGui.GetIO();
        io.DisplaySize = new NVector2(Math.Max(1,window.ClientSize.X),Math.Max(1,window.ClientSize.Y));
        io.DisplayFramebufferScale = new NVector2(window.FramebufferSize.X/io.DisplaySize.X,window.FramebufferSize.Y/io.DisplaySize.Y);
        io.DeltaTime = Math.Clamp(deltaTime, 1e-5f, 0.25f);
        bool focused = _focused;
        // 位置可同步；按钮/滚轮/焦点只收事件，避免把排队的历史覆写成当前状态。
        AddMousePosition(io);
        // 这里逐帧查询键盘当前按住状态；若两次采样间已按下又松开，仍可能漏掉短按。
        // 文字字符由OnTextInput/AddCharacter事件链提交；此处同步按键状态，供导航、快捷键等使用。
        foreach (Keys key in Enum.GetValues<Keys>())
        {
            ImGuiKey mapped = MapKey(key);
            if (mapped != ImGuiKey.None) io.AddKeyEvent(mapped,focused && window.KeyboardState.IsKeyDown(key));
        }
        io.AddKeyEvent(ImGuiKey.ModCtrl,focused && (window.KeyboardState.IsKeyDown(Keys.LeftControl)||window.KeyboardState.IsKeyDown(Keys.RightControl)));
        io.AddKeyEvent(ImGuiKey.ModShift,focused && (window.KeyboardState.IsKeyDown(Keys.LeftShift)||window.KeyboardState.IsKeyDown(Keys.RightShift)));
        io.AddKeyEvent(ImGuiKey.ModAlt,focused && (window.KeyboardState.IsKeyDown(Keys.LeftAlt)||window.KeyboardState.IsKeyDown(Keys.RightAlt)));
        ImGui.NewFrame(); _frame = true;
    }

    /// <summary>将支持的OpenTK键映射到ImGui键；未支持项返回None。</summary>
    private static ImGuiKey MapKey(Keys key)
    {
        if (key is >= Keys.A and <= Keys.Z) return ImGuiKey.A + (key-Keys.A);
        if (key is >= Keys.D0 and <= Keys.D9) return ImGuiKey._0 + (key-Keys.D0);
        if (key is >= Keys.F1 and <= Keys.F12) return ImGuiKey.F1 + (key-Keys.F1);
        return key switch
        {
            Keys.Tab=>ImGuiKey.Tab, Keys.Left=>ImGuiKey.LeftArrow, Keys.Right=>ImGuiKey.RightArrow,
            Keys.Up=>ImGuiKey.UpArrow, Keys.Down=>ImGuiKey.DownArrow, Keys.PageUp=>ImGuiKey.PageUp,
            Keys.PageDown=>ImGuiKey.PageDown, Keys.Home=>ImGuiKey.Home, Keys.End=>ImGuiKey.End,
            Keys.Insert=>ImGuiKey.Insert, Keys.Delete=>ImGuiKey.Delete, Keys.Backspace=>ImGuiKey.Backspace,
            Keys.Space=>ImGuiKey.Space, Keys.Enter=>ImGuiKey.Enter, Keys.Escape=>ImGuiKey.Escape,
            Keys.LeftControl=>ImGuiKey.LeftCtrl, Keys.RightControl=>ImGuiKey.RightCtrl,
            Keys.LeftShift=>ImGuiKey.LeftShift, Keys.RightShift=>ImGuiKey.RightShift,
            Keys.LeftAlt=>ImGuiKey.LeftAlt, Keys.RightAlt=>ImGuiKey.RightAlt,
            _=>ImGuiKey.None
        };
    }

    /// <summary>消费本帧UI绘制列表，在当前GL上下文上传/裁剪/绘制（这里是绘制当前控制器的_window，不是场景），并在finally恢复所改GL状态。</summary>
    public unsafe void Render()
    {
        ImGui.SetCurrentContext(_context);
        if (!_frame) return;
        ImGui.Render(); _frame = false;
        var data = ImGui.GetDrawData();
        int width=(int)(data.DisplaySize.X*data.FramebufferScale.X), height=(int)(data.DisplaySize.Y*data.FramebufferScale.Y);
        if (width<=0 || height<=0 || data.CmdListsCount==0) return;
        // 保存所有本后端会改的GL状态，避免UI隐式破坏下一帧的渲染管线。
        int oldProgram=GL.GetInteger(GetPName.CurrentProgram), oldVao=GL.GetInteger(GetPName.VertexArrayBinding);
        int oldArray=GL.GetInteger(GetPName.ArrayBufferBinding), oldActive=GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int oldTexture=GL.GetInteger(GetPName.TextureBinding2D), oldSampler=GL.GetInteger(GetPName.SamplerBinding);
        bool blend=GL.IsEnabled(EnableCap.Blend), depth=GL.IsEnabled(EnableCap.DepthTest), cull=GL.IsEnabled(EnableCap.CullFace), scissor=GL.IsEnabled(EnableCap.ScissorTest);
        int srcRgb=GL.GetInteger(GetPName.BlendSrcRgb), dstRgb=GL.GetInteger(GetPName.BlendDstRgb);
        int srcAlpha=GL.GetInteger(GetPName.BlendSrcAlpha), dstAlpha=GL.GetInteger(GetPName.BlendDstAlpha);
        int eqRgb=GL.GetInteger(GetPName.BlendEquationRgb), eqAlpha=GL.GetInteger(GetPName.BlendEquationAlpha);
        int[] viewport=new int[4], box=new int[4]; GL.GetInteger(GetPName.Viewport,viewport); GL.GetInteger(GetPName.ScissorBox,box);
        try
        {
            GL.Enable(EnableCap.Blend); GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha,BlendingFactorDest.OneMinusSrcAlpha,BlendingFactorSrc.One,BlendingFactorDest.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace); GL.Disable(EnableCap.DepthTest); GL.Enable(EnableCap.ScissorTest);
            GL.Viewport(0,0,width,height); GL.UseProgram(_program); GL.BindVertexArray(_vao); GL.BindSampler(0,0);
            // UI坐标原点在左上，正交投影反转Y；CPU行矩阵按transpose=false上传适配GLSL列向量。
            Matrix4 projection=Matrix4.CreateOrthographicOffCenter(data.DisplayPos.X,data.DisplayPos.X+data.DisplaySize.X,
                data.DisplayPos.Y+data.DisplaySize.Y,data.DisplayPos.Y,-1,1);
            GL.UniformMatrix4(GL.GetUniformLocation(_program,"projection"),false,ref projection);
            GL.Uniform1(GL.GetUniformLocation(_program,"fontTexture"),0);
            for(int listIndex=0;listIndex<data.CmdListsCount;listIndex++)
            {
                var list=data.CmdLists[listIndex];
                GL.BindBuffer(BufferTarget.ArrayBuffer,_vertices);
                GL.BufferData(BufferTarget.ArrayBuffer,list.VtxBuffer.Size*20,list.VtxBuffer.Data,BufferUsageHint.StreamDraw);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer,_indices);
                GL.BufferData(BufferTarget.ElementArrayBuffer,list.IdxBuffer.Size*2,list.IdxBuffer.Data,BufferUsageHint.StreamDraw);
                for(int i=0;i<list.CmdBuffer.Size;i++)
                {
                    var command=list.CmdBuffer[i];
                    if(command.UserCallback!=IntPtr.Zero) throw new NotSupportedException("Custom ImGui draw callbacks are outside V1.");
                    var clip=command.ClipRect;
                    int x=(int)((clip.X-data.DisplayPos.X)*data.FramebufferScale.X);
                    int y=(int)((clip.Y-data.DisplayPos.Y)*data.FramebufferScale.Y);
                    int right=(int)((clip.Z-data.DisplayPos.X)*data.FramebufferScale.X);
                    int bottom=(int)((clip.W-data.DisplayPos.Y)*data.FramebufferScale.Y);
                    x=Math.Clamp(x,0,width); y=Math.Clamp(y,0,height); right=Math.Clamp(right,0,width); bottom=Math.Clamp(bottom,0,height);
                    if(right<=x||bottom<=y) continue;
                    // 控件裁剪先乘DPI缩放，再由左上逻辑坐标改为GL左下像素坐标。
                    GL.Scissor(x,height-bottom,right-x,bottom-y);
                    GL.BindTexture(TextureTarget.Texture2D,(int)command.TextureId);
                    GL.DrawElementsBaseVertex(PrimitiveType.Triangles,(int)command.ElemCount,DrawElementsType.UnsignedShort,
                        (nint)(command.IdxOffset*2),(int)command.VtxOffset);
                }
            }
        }
        finally
        {
            GL.UseProgram(oldProgram); GL.BindVertexArray(oldVao); GL.BindBuffer(BufferTarget.ArrayBuffer,oldArray);
            GL.BindTexture(TextureTarget.Texture2D,oldTexture); GL.BindSampler(0,oldSampler); GL.ActiveTexture((TextureUnit)oldActive);
            GL.BlendEquationSeparate((BlendEquationMode)eqRgb,(BlendEquationMode)eqAlpha);
            GL.BlendFuncSeparate((BlendingFactorSrc)srcRgb,(BlendingFactorDest)dstRgb,(BlendingFactorSrc)srcAlpha,(BlendingFactorDest)dstAlpha);
            Set(EnableCap.Blend,blend); Set(EnableCap.DepthTest,depth); Set(EnableCap.CullFace,cull); Set(EnableCap.ScissorTest,scissor);
            GL.Viewport(viewport[0],viewport[1],viewport[2],viewport[3]); GL.Scissor(box[0],box[1],box[2],box[3]);
        }
    }

    /// <summary>按保存的启用状态恢复一个GL能力开关。</summary>
    private static void Set(EnableCap capability,bool enabled) 
    { 
        if(enabled) GL.Enable(capability); 
        else GL.Disable(capability); 
    }

    /// <summary>编译内嵌ASCII shader；成功转交句柄，失败先删除再抛出编译日志。</summary>
    private static int Shader(ShaderType type,string source)
    {
        int shader=GL.CreateShader(type); GL.ShaderSource(shader,source); GL.CompileShader(shader);
        GL.GetShader(shader,ShaderParameter.CompileStatus,out int compiled);
        if(compiled!=0) return shader;
        string error=GL.GetShaderInfoLog(shader); GL.DeleteShader(shader); throw new InvalidOperationException(error);
    }

    /// <summary>解除窗口事件、结束未完成UI帧，随后释放GL句柄和ImGui上下文；由窗口在有效GL上下文调用。</summary>
    public void Dispose()
    {
        if(_disposed) return;
        _window.MouseMove -= OnMouseMove;
        _window.MouseDown -= OnMouseDown;
        _window.MouseUp -= OnMouseUp;
        _window.MouseWheel -= OnMouseWheel;
        _window.FocusedChanged -= OnFocusedChanged;
        _window.TextInput -= OnTextInput;
        ImGui.SetCurrentContext(_context); if(_frame) { ImGui.EndFrame(); _frame=false; }
        if(_font!=0) GL.DeleteTexture(_font); if(_program!=0) GL.DeleteProgram(_program);
        if(_vertices!=0) GL.DeleteBuffer(_vertices); if(_indices!=0) GL.DeleteBuffer(_indices); if(_vao!=0) GL.DeleteVertexArray(_vao);
        ImGui.DestroyContext(_context); _disposed=true;
    }
}
