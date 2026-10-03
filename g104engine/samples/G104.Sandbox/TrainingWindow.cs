using G104.Engine.Audio;
using G104.Engine.Assets;
using G104.Engine.Core;
using G104.Engine.Editor;
using G104.Engine.Effects;
using G104.Engine.Physics;
using G104.Engine.Rendering;
using G104.Engine.Scene;
using G104.Engine.Tools;
using G104.Sandbox.Gameplay;
using G104.Sandbox.Tools;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using NVector2=System.Numerics.Vector2;

namespace G104.Sandbox;

/// <summary>组装训练场；通用功能在Engine，专属场景/交互及演示流程在Sandbox。</summary>
public sealed class TrainingWindow : GameWindow
{
    private readonly LaunchOptions _options;
    private readonly FixedStepClock _clock=new();
    private readonly InputBuffer _input=new();
    private readonly ParticleSystem _particles=new();
    private readonly Queue<Action> _commands=[];
    private readonly SceneEditorPanel _editorPanel=new();
    private SceneGraph _design=null!;
    private SceneEditor _editor=null!;
    private SceneGraph? _runtime;
    private TrainingSimulation? _simulation;
    private TrainingRenderer? _renderer;
    private ImGuiController? _ui;
    private AudioSystem? _audio;
    private IReadOnlyList<RenderObject> _objects=[];
    private CameraState _camera;
    private Guid? _selected;
    private float _yaw,_pitch=.33f,_distance=7,_alpha=1,_fps,_frameMilliseconds;
    private Vector3 _editTarget=new(0,1,0);
    private bool _paused,_suspended,_disposed;
    private string _message="Ready. Select Play to explore the training ground.";
    private string _audioStatus="not initialized";
    private int _frame,_footstep;
    private int _loopVoice;
    private bool _showPath=true,_showSkeleton;
    private string _animationEvent="none";
    private RenderPipelineMode _activePipeline;
    private bool _discardNextDelta=true,_rejectedSavedDesign;
    private string? _rejectedBackup;
    private int _exerciseJumpEvents,_exerciseMovementFrames;
    private string? _exerciseUnsavedDesign;
    private int _exerciseUnsavedHistory;
    private string? _pendingLoad;
    private bool _loadModalOpened;
    private RenderDebugView _debug;
    private string SavePath=>Path.Combine(_options.UserDataRoot,"Scenes","training-ground.json");
    private bool Playing=>_simulation is not null;

    public TrainingWindow(LaunchOptions options):base(GameWindowSettings.Default,new NativeWindowSettings
    {
        ClientSize=new Vector2i(1440,900), Title="G104Engine | Training Ground V1", API=ContextAPI.OpenGL,
        APIVersion=new Version(4,3),Profile=ContextProfile.Core,Flags=ContextFlags.ForwardCompatible,StartVisible=!options.Exercise
    }) { _options=options; }

    protected override void OnLoad()
    {
        base.OnLoad();
        Console.WriteLine($"GPU: {GL.GetString(StringName.Renderer)}; GL: {GL.GetString(StringName.Version)}");
        int major=GL.GetInteger(GetPName.MajorVersion),minor=GL.GetInteger(GetPName.MinorVersion);
        if(major<4||(major==4&&minor<3)) throw new NotSupportedException("OpenGL4.3Core required.");
        VSync=_options.Exercise ? VSyncMode.Off : VSyncMode.On;
        _ui=new ImGuiController();
        TextInput += e=>_ui?.AddCharacter((uint)e.Unicode);
        SceneDocument initial;
        string seed=Path.Combine(_options.AssetRoot,"scenes","training-ground.json");
        bool hadUserSave=File.Exists(SavePath);
        if(hadUserSave)
        {
            try { initial=SceneSerializer.Load(SavePath,_options.AssetRoot); }
            catch(Exception error) { _rejectedSavedDesign=true;_message="Saved scene rejected and preserved: "+error.Message;Console.WriteLine(_message); initial=SceneSerializer.Load(seed,_options.AssetRoot); }
        }
        else initial=SceneSerializer.Load(seed,_options.AssetRoot);
        _design=new SceneGraph(initial);_editor=new SceneEditor(_design);
        _activePipeline=initial.Rendering.Pipeline;
        _renderer=new TrainingRenderer(_options.AssetRoot,FramebufferSize.X,FramebufferSize.Y);
        _objects=BuildObjects(_design,1);
        try { _renderer.Prepare(_objects); }
        catch(Exception error) when(hadUserSave&&!_rejectedSavedDesign)
        {
            _rejectedSavedDesign=true;_message="Saved scene resource preparation failed; original preserved: "+error.Message;
            Console.WriteLine(_message);
            initial=SceneSerializer.Load(seed,_options.AssetRoot);_design=new SceneGraph(initial);_editor=new SceneEditor(_design);
            _activePipeline=initial.Rendering.Pipeline;_objects=BuildObjects(_design,1);_renderer.Prepare(_objects);
        }
        ConnectEditorPreparation();
        try { _audio=new AudioSystem(_options.AssetRoot);_audioStatus=_audio.Backend; }
        catch(Exception error) { _audioStatus="Unavailable: "+error.Message;Console.WriteLine(_audioStatus); }
        _selected=initial.Objects.FirstOrDefault(o=>o.Kind==ObjectKind.Player)?.Id;
        Console.WriteLine("Assets: "+_options.AssetRoot);Console.WriteLine("Design save: "+SavePath);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        if(FramebufferSize.X>0&&FramebufferSize.Y>0) _renderer?.Resize(FramebufferSize.X,FramebufferSize.Y);
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        if(e.Width>0&&e.Height>0) _renderer?.Resize(e.Width,e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        if(_ui is null||_renderer is null) return;
        _frame++;bool discardedThisFrame=_discardNextDelta;
        float dt=discardedThisFrame ? 0 : (float)Math.Clamp(e.Time,0,0.25);_discardNextDelta=false;
        _frameMilliseconds=_frameMilliseconds==0 ? dt*1000 : MathHelper.Lerp(_frameMilliseconds,dt*1000,.08f);
        _fps=_frameMilliseconds>0 ? 1000/_frameMilliseconds : 0;
        _ui.BeginFrame(this,Math.Max(dt,1e-4f));
        DrawToolbar();_selected=_editorPanel.Draw(_editor,Playing,_selected,Enqueue);DrawStatus();
        if(MouseState.IsButtonPressed(MouseButton.Left)&&!ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow)) ImGui.SetWindowFocus(null!);
        ProcessCommands();
        if(_options.Exercise) Exercise();
        bool suspended=(!_options.Exercise&&(!IsFocused||WindowState==OpenTK.Windowing.Common.WindowState.Minimized))||_paused;
        if(suspended!=_suspended) { _suspended=suspended;_discardNextDelta=true;_clock.Reset();_input.Clear();_runtime?.ResetInterpolation();_renderer.ResetDisplayHistory();_audio?.SetPaused(suspended); }
        if(KeyboardState.IsKeyPressed(Keys.Escape)&&!_ui.WantsKeyboard) { if(Playing) Enqueue(Stop); else Close(); }
        if(KeyboardState.IsKeyPressed(Keys.F5)&&!_ui.WantsKeyboard) Enqueue(Playing ? Stop : Play);
        if(!Playing && !_ui.WantsKeyboard && KeyboardState.IsKeyDown(Keys.LeftControl))
        {
            if(KeyboardState.IsKeyPressed(Keys.Z)) Enqueue(()=>_editor.History.Undo());
            if(KeyboardState.IsKeyPressed(Keys.Y)) Enqueue(()=>_editor.History.Redo());
            if(KeyboardState.IsKeyPressed(Keys.S)) Enqueue(Save);
        }
        Vector2 move=new((KeyboardState.IsKeyDown(Keys.D)?1:0)-(KeyboardState.IsKeyDown(Keys.A)?1:0),
                         (KeyboardState.IsKeyDown(Keys.W)?1:0)-(KeyboardState.IsKeyDown(Keys.S)?1:0));
        Vector2 look=MouseState.IsButtonDown(MouseButton.Right) ? MouseState.Delta : Vector2.Zero;
        bool jump=KeyboardState.IsKeyPressed(Keys.Space),interact=KeyboardState.IsKeyPressed(Keys.E),sprint=KeyboardState.IsKeyDown(Keys.LeftShift);
        if(_options.Exercise)
        {
            move=(_frame is >=6 and <=65 || _frame is >=80 and <=130) ? Vector2.UnitY : Vector2.Zero;
            look=Vector2.Zero;jump=_frame is 55 or 95;interact=false;sprint=_frame is >=35 and <=65 || _frame is >=80 and <=130;
        }
        _input.Push(new InputState(move,look,jump,interact,!suspended,
            !_options.Exercise&&_ui.WantsKeyboard,!_options.Exercise&&_ui.WantsMouse,sprint));
        Vector2 frameLook=_input.ConsumeLook();_yaw-=frameLook.X*.004f;_pitch=Math.Clamp(_pitch+frameLook.Y*.004f,.08f,1.25f);
        if(!_ui.WantsMouse) _distance=Math.Clamp(_distance-MouseState.ScrollDelta.Y*.5f,2.5f,15);
        if(_options.Exercise) { _yaw=_frame>=225 ? .4f : 0;_pitch=.33f;_distance=_frame>=225 ? 4 : 7; }
        if(Playing&&!suspended)
        {
            // 模拟、事实、动画/粒子按固定步推进；绘制只读取插值，不重发事件。
            var result=_clock.Advance(_discardNextDelta||discardedThisFrame ? 0 : (_options.Exercise ? 1.0/60 : dt),step=>
            {
                _runtime!.CapturePrevious();var input=_input.Consume();
                _simulation!.Tick(step,new GameInput(input.Move,input.Sprint,input.JumpPressed,input.InteractPressed,_yaw));
                if(_options.Exercise&&_simulation.PlayerState.Velocity.Xz.Length>.2f) _exerciseMovementFrames++;
                foreach(var fact in _simulation.Events) Feedback(fact);
                _particles.Tick(step);_renderer.UpdateAnimations(step,BuildObjects(_runtime,1));
                foreach(var actor in _runtime.Document.Objects.Where(o=>o.Kind is ObjectKind.Player or ObjectKind.Npc))
                    foreach(var animationEvent in _renderer.DrainAnimationEvents(actor.Id))
                        _animationEvent=$"{actor.Name}: {animationEvent.Name} #{animationEvent.Sequence}";
            });
            _alpha=result.Alpha;
            if(_options.Exercise&&_frame==206)
            {
                if(!discardedThisFrame||result.Steps!=0) throw new InvalidOperationException("Synchronous preparation time entered simulation.");
                Console.WriteLine($"Slow prepare 120ms: next raw frame {e.Time*1000:F2}ms, discarded; simulated steps {result.Steps}.");
            }
            if(result.DroppedSteps>0) Console.WriteLine($"Fixed-step debt dropped: {result.DroppedSteps}");
        }
        else
        {
            _alpha=1;
            if(!Playing&&!suspended)
            {
                _renderer.UpdateAnimations(dt,BuildObjects(_design,1));
                foreach(var actor in _design.Document.Objects.Where(o=>o.Kind is ObjectKind.Player or ObjectKind.Npc)) _renderer.DrainAnimationEvents(actor.Id);
            }
        }
        _objects=BuildObjects(_runtime??_design,_alpha);UpdateCamera();
        DrawWorldDebug();
        _audio?.SetListener(_camera.Position,(_camera.Target-_camera.Position).Normalized());
    }

    private void Enqueue(Action action)=>_commands.Enqueue(action);
    private void ProcessCommands()
    {
        while(_commands.TryDequeue(out var action))
        {
            try { action(); }
            catch(Exception error) { _editor?.History.CancelTransaction();_message=error.Message;Console.WriteLine("Command rejected: "+error); }
        }
    }

    private void Play()
    {
        if(Playing) return;
        if(_options.Exercise&&_frame==205) Thread.Sleep(120);
        if(_editor.History.InTransaction) _editor.History.CommitTransaction();
        var candidate=new SceneGraph(SceneSerializer.Clone(_design.Document));
        TrainingSimulation? simulation=null;
        try
        {
            simulation=new TrainingSimulation(candidate.Document,candidate);
            _renderer!.Prepare(BuildObjects(candidate,1));
        }
        catch { simulation?.Dispose();throw; }
        _runtime=candidate;_simulation=simulation;
        _activePipeline=candidate.Document.Rendering.Pipeline;
        _renderer!.ResetAnimations();
        _clock.Reset();_input.Clear();_particles.Clear();_audio?.StopAll();_loopVoice=0;_paused=false;_alpha=1;
        _discardNextDelta=true;
        _message="Play: WASD move, Shift run, Space jump, E interact, RMB orbit; Esc stops.";
        ImGui.SetWindowFocus(null!);
        Console.WriteLine("Play prepared: "+candidate.Document.Rendering.Pipeline);
    }

    private void Stop()
    {
        if(!Playing) return;
        // 保留原设计和Undo历史；运行变换/机关状态只存在独立Clone中。
        _renderer!.Prepare(BuildObjects(_design,1));
        _audio?.StopAll();_loopVoice=0;_particles.Clear();_simulation!.Dispose();_simulation=null;_runtime=null;
        _renderer.ResetAnimations();
        _clock.Reset();_input.Clear();_design.ResetInterpolation();_paused=false;_alpha=1;
        _discardNextDelta=true;
        _message="Stopped; unsaved design edits and undo history retained.";Console.WriteLine("Stop restored design");
    }

    private void Save()
    {
        if(_editor.History.InTransaction) _editor.History.CommitTransaction();
        if(_rejectedSavedDesign&&File.Exists(SavePath))
        {
            string backup=SavePath+".rejected-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json";
            File.Copy(SavePath,backup,overwrite:false);_rejectedBackup=backup;Console.WriteLine("Rejected original backed up: "+backup);
        }
        SceneSerializer.Save(_design.Document,SavePath,_options.AssetRoot);_rejectedSavedDesign=false;
        _editor.History.MarkSaved();_message="Saved design: "+SavePath;
    }

    private void LoadDesign(string path)
    {
        if(Playing) throw new InvalidOperationException("Stop before loading another design.");
        var candidate=new SceneGraph(SceneSerializer.Load(path,_options.AssetRoot));
        _renderer!.Prepare(BuildObjects(candidate,1));
        _design=candidate;_editor=new SceneEditor(candidate);_selected=null;_clock.Reset();_input.Clear();_message="Loaded "+path;
        _activePipeline=candidate.Document.Rendering.Pipeline;
        _renderer.ResetAnimations();ConnectEditorPreparation();
        _audio?.StopAll();_loopVoice=0;_particles.Clear();
        _discardNextDelta=true;
    }

    private void RequestLoad(string path)
    {
        if(_editor.History.IsDirty) { _pendingLoad=path;_loadModalOpened=false; }
        else LoadDesign(path);
    }

    private void ConnectEditorPreparation() => _editor.History.PrepareChange = candidate =>
    {
        SceneSerializer.ValidateAssets(candidate.Document,_options.AssetRoot);
        _renderer!.Prepare(BuildObjects(candidate,1));
    };

    private IReadOnlyList<RenderObject> BuildObjects(SceneGraph graph,float alpha)=>graph.Document.Objects
        .Where(o=>o.Visible&&o.Kind!=ObjectKind.Group).Select(o=>new RenderObject(o.Id,graph.InterpolatedWorldMatrix(o.Id,alpha),o,
            _simulation is not null && ReferenceEquals(graph,_runtime) ? _simulation.AnimationFor(o.Id) : new AnimationInputs(0,true,0),alpha)).ToArray();

    private void UpdateCamera()
    {
        Vector3 target=_editTarget;
        if(Playing)
        {
            var player=_runtime!.Document.Objects.First(o=>o.Kind==ObjectKind.Player);
            target=_runtime.InterpolatedWorldMatrix(player.Id,_alpha).ExtractTranslation()+new Vector3(0,1.15f,0);
        }
        Vector3 offset=new(MathF.Sin(_yaw)*MathF.Cos(_pitch),MathF.Sin(_pitch),MathF.Cos(_yaw)*MathF.Cos(_pitch));
        float distance=_distance;
        if(Playing)
        {
            var hit=_simulation!.World.Raycast(target,offset,_distance);
            if(hit is not null) distance=Math.Clamp(hit.Value.Distance-.25f,.6f,_distance);
        }
        Vector3 position=target+offset*distance;
        _camera=new CameraState(position,target,Matrix4.LookAt(position,target,Vector3.UnitY),
            Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(55),Math.Max(1,FramebufferSize.X)/(float)Math.Max(1,FramebufferSize.Y),.08f,120));
    }

    private void Feedback(SimulationEvent fact)
    {
        if(_options.Exercise&&fact.Sound=="jump") _exerciseJumpEvents++;
        string clip=fact.Sound switch { "footstep"=>"step"+((_footstep++)%2),"land"=>"step0","button"=>"switch","denied"=>"click",_=>fact.Sound };
        if(_audio is not null)
        {
            bool spatial=fact.Sound is not "complete" and not "denied";
            _audio.Play(clip,fact.Position,spatial,gain:_options.Exercise ? .02f : (fact.Sound=="footstep" ? .22f : .6f));
        }
        if(fact.Particles) _particles.Burst(fact.Position+Vector3.UnitY*.5f,fact.Color);
    }

    private void DrawToolbar()
    {
        ImGui.SetNextWindowPos(new NVector2(12,12),ImGuiCond.Always);ImGui.SetNextWindowSize(new NVector2(680,112),ImGuiCond.Always);
        ImGui.Begin("Training Ground V1",ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoCollapse);
        if(ImGui.Button(Playing ? "Stop (F5)" : "Play (F5)")) Enqueue(Playing ? Stop : Play);
        ImGui.SameLine();if(ImGui.Button("Save design")) Enqueue(Save);
        ImGui.SameLine();ImGui.BeginDisabled(Playing);if(ImGui.Button("Load saved")) Enqueue(()=>RequestLoad(SavePath));
        ImGui.SameLine();if(ImGui.Button("Reset seed")) Enqueue(()=>RequestLoad(Path.Combine(_options.AssetRoot,"scenes","training-ground.json")));ImGui.EndDisabled();
        if(Playing) { ImGui.SameLine();if(ImGui.Button(_paused ? "Resume" : "Pause")) Enqueue(()=>_paused=!_paused); }
        ImGui.TextUnformatted($"{(Playing ? "PLAY" : "EDIT")} | {(_editor.History.IsDirty ? "Unsaved" : "Saved")} | {_fps:F0} FPS / {_frameMilliseconds:F2} ms");
        ImGui.BeginDisabled(Playing);int pipeline=(int)_design.Document.Rendering.Pipeline;
        if(ImGui.Combo("Pipeline (next Play)",ref pipeline,"Forward\0Deferred\0")) { var mode=(RenderPipelineMode)pipeline;Enqueue(()=>_editor.SetRenderPipeline(mode)); }
        ImGui.EndDisabled();
        if(_pendingLoad is not null&&!_loadModalOpened) { ImGui.OpenPopup("Unsaved design");_loadModalOpened=true; }
        bool modalOpen=true;
        if(ImGui.BeginPopupModal("Unsaved design",ref modalOpen,ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted("This design has unsaved edits. Choose how to load another design.");
            if(ImGui.Button("Save and load"))
            {
                string path=_pendingLoad!;_pendingLoad=null;_loadModalOpened=false;ImGui.CloseCurrentPopup();
                Enqueue(()=>{ Save();LoadDesign(path); });
            }
            ImGui.SameLine();if(ImGui.Button("Discard and load"))
            {
                string path=_pendingLoad!;_pendingLoad=null;_loadModalOpened=false;ImGui.CloseCurrentPopup();Enqueue(()=>LoadDesign(path));
            }
            ImGui.SameLine();if(ImGui.Button("Cancel")) { _pendingLoad=null;_loadModalOpened=false;ImGui.CloseCurrentPopup(); }
            ImGui.EndPopup();
        }
        if(!modalOpen) { _pendingLoad=null;_loadModalOpened=false; }
        ImGui.End();
    }

    private void DrawStatus()
    {
        ImGui.SetNextWindowPos(new NVector2(Math.Max(710,ClientSize.X-350),12),ImGuiCond.Always);
        ImGui.SetNextWindowSize(new NVector2(338,0),ImGuiCond.Always);ImGui.Begin("Runtime / Render debug",ImGuiWindowFlags.NoResize);
        int view=(int)_debug;if(ImGui.Combo("View",ref view,"Final\0Base color\0Normals\0Roughness\0Depth\0Shadow\0")) _debug=(RenderDebugView)view;
        ImGui.TextWrapped(_message);
        ImGui.TextUnformatted("RMB drag: orbit | Wheel: distance");
        if(!Playing&&_selected is Guid selected && ImGui.Button("Focus selected")) _editTarget=_design.WorldPosition(selected)+Vector3.UnitY;
        if(_simulation is not null)
        {
            ImGui.Separator();ImGui.TextUnformatted($"Player grounded: {_simulation.PlayerState.Grounded}");
            ImGui.TextUnformatted($"Speed: {_simulation.PlayerState.Velocity.Xz.Length:F2} m/s");
            ImGui.TextUnformatted($"Door: {(_simulation.DoorOpen ? "open" : "closed")} | Goal: {_simulation.Completed}");
            ImGui.TextUnformatted($"NPC: {_simulation.NpcMode} | Path: {_simulation.NpcPath.Count} nodes");
            ImGui.TextWrapped(_simulation.Feedback);
            if(_selected is Guid actor) ImGui.TextWrapped(_renderer!.AnimationDebug(actor));
            ImGui.TextWrapped("Animation event: "+_animationEvent);
        }
        ImGui.Separator();ImGui.TextWrapped("Audio: "+_audioStatus);ImGui.TextUnformatted($"Voices {_audio?.ActiveVoices??0} | Particles {_particles.Count}");
        ImGui.Checkbox("NPC path overlay",ref _showPath);ImGui.Checkbox("Selected skeleton overlay",ref _showSkeleton);
        if(_audio is not null)
        {
            if(ImGui.Button("2D test")) Enqueue(()=>_audio.Play("click",Vector3.Zero,false));ImGui.SameLine();
            if(ImGui.Button("3D test")) Enqueue(()=>_audio.Play("door",new Vector3(3,1,0)));
            if(ImGui.Button(_loopVoice==0 ? "Loop test" : "Stop loop")) Enqueue(()=>
            {
                if(_loopVoice==0) _loopVoice=_audio.Play("loop-test",Vector3.Zero,false,true,.2f);
                else { _audio.Stop(_loopVoice);_loopVoice=0; }
            });
        }
        if(_rejectedSavedDesign) ImGui.TextWrapped("Original save rejected. Save will preserve a backup before replacement.");
        if(_rejectedBackup is not null) ImGui.TextWrapped("Preserved original: "+_rejectedBackup);
        ImGui.TextWrapped("Save: "+SavePath);ImGui.End();
    }

    private void Exercise()
    {
        if(_frame==3)
        {
            string original=SceneSerializer.Serialize(_design.Document);
            _editor.Create(new SceneObjectData { Name="Preparation failure fixture",Transform=new TransformData { Scale=new Float3(1,1,2) },
                Collider=new ColliderData { Kind=ColliderKind.Capsule,Size=new Float3(1,3,1) } });
            string before=SceneSerializer.Serialize(_design.Document);bool rejected=false;
            try { Play(); } catch(ArgumentException) { rejected=true; }
            if(!rejected||Playing||SceneSerializer.Serialize(_design.Document)!=before||PhysicsWorld.ActiveWorlds!=0)
                throw new InvalidOperationException("Failed Play lost preview/design or leaked physics resources.");
            _editor.History.Undo();
            if(SceneSerializer.Serialize(_design.Document)!=original) throw new InvalidOperationException("Preparation failure fixture did not restore original preview.");
            Console.WriteLine("Exercise failed Play preserved preview/design and disposed partial physics world.");
        }
        if(_frame==5) Play();
        if(_frame==75) { Stop();_editor.SetRenderPipeline(RenderPipelineMode.Deferred);Play(); }
        if(_frame==135) ClientSize=new Vector2i(1280,800);
        if(_frame==150) _debug=RenderDebugView.Normals;
        if(_frame==165) _debug=RenderDebugView.Shadow;
        if(_frame==180)
        {
            Stop();_debug=RenderDebugView.Final;
            int count=_design.Document.Objects.Count;int history=_editor.History.UndoCount;
            var model=_design.Document.Objects.First(o=>o.Kind==ObjectKind.Player);
            string before=model.ModelPath!;
            try { _editor.SetAsset(model.Id,"models/missing-bad-edit.glb");throw new InvalidOperationException("Invalid model edit unexpectedly committed."); }
            catch(SceneValidationException) { }
            if(model.ModelPath!=before||_design.Document.Objects.Count!=count||_editor.History.UndoCount!=history)
                throw new InvalidOperationException("Failed asset edit changed design/history.");
            Guid cube=_editor.Create("cube");_editor.SetTransform(cube,new TransformData { Position=new Float3(3,.5f,7) });
            _editor.History.Undo();_editor.History.Redo();Save();LoadDesign(SavePath);
            Console.WriteLine("Exercise save/load/undo/redo passed");
        }
        if(_frame==185)
        {
            Guid unsaved=_editor.Create("cube");_editor.SetTransform(unsaved,new TransformData { Position=new Float3(7,.5f,9) });
            _exerciseUnsavedDesign=SceneSerializer.Serialize(_design.Document);_exerciseUnsavedHistory=_editor.History.UndoCount;
        }
        if(_frame==190) { Play();_paused=true; }
        if(_frame==200) _paused=false;
        if(_frame==205)
        {
            Stop();
            if(!_editor.History.IsDirty||!_editor.History.CanUndo||_editor.History.UndoCount!=_exerciseUnsavedHistory||
                SceneSerializer.Serialize(_design.Document)!=_exerciseUnsavedDesign)
                throw new InvalidOperationException("Play/Stop lost unsaved edits or undo history.");
            Console.WriteLine("Exercise Play/Stop retained unsaved design and undo history.");Play();
        }
        if(_frame==215) Stop();
        if(_frame==218) VerifyStaticSkeletonSpace();
        if(_frame==225)
        {
            var probe=_design.Document.Objects.First(o=>o.ModelPath=="models/material-probe.glb");
            _editTarget=_design.WorldPosition(probe.Id);_selected=probe.Id;
        }
        if(_frame==233) _editor.History.Execute("Shadow isolation",()=>_design.Document.Rendering.Shadows=false);
        if(_frame==238) _editor.History.Undo();
    }

    private void DrawWorldDebug()
    {
        if(_debug!=RenderDebugView.Final) return;
        var draw=ImGui.GetBackgroundDrawList();
        NVector2? Project(Vector3 point)
        {
            Vector4 clip=Vector4.TransformRow(new Vector4(point,1),_camera.View*_camera.Projection);
            if(clip.W<=.01f) return null;
            return new NVector2((clip.X/clip.W*.5f+.5f)*ClientSize.X,(-clip.Y/clip.W*.5f+.5f)*ClientSize.Y);
        }
        void Line(Vector3 a,Vector3 b,uint color)
        {
            var first=Project(a);var second=Project(b);if(first.HasValue&&second.HasValue) draw.AddLine(first.Value,second.Value,color,2);
        }
        if(_showPath&&_simulation is not null)
            for(int i=1;i<_simulation.NpcPath.Count;i++) Line(_simulation.NpcPath[i-1]+Vector3.UnitY*.08f,_simulation.NpcPath[i]+Vector3.UnitY*.08f,0xfff0b540);
        if(_showSkeleton&&_selected is Guid id)
        {
            var item=_objects.FirstOrDefault(o=>o.Id==id);
            if(item is not null) foreach(var segment in _renderer!.SkeletonDebug(id,item.ModelMatrix,item.InterpolationAlpha)) Line(segment.Start,segment.End,0xff60ff90);
        }
    }

    private void VerifyStaticSkeletonSpace()
    {
        var original=_objects.First(o=>o.Design.Kind==ObjectKind.Player);
        var design=new SceneObjectData { Kind=ObjectKind.StaticMesh,Primitive=PrimitiveKind.Model,ModelPath=original.Design.ModelPath };
        var item=original with { Design=design,ModelMatrix=Matrix4.Identity,InterpolationAlpha=1 };
        var model=new GltfModelLoader(new AssetRoot(_options.AssetRoot)).Load(design.ModelPath!);
        var world=new Matrix4[model.Nodes.Length];model.EvaluateWorld(model.CreateDefaultPose(),world);
        var joints=model.Skins.SelectMany(s=>s.Joints).ToHashSet();
        var expected=joints.Where(j=>model.Nodes[j].Parent>=0&&joints.Contains(model.Nodes[j].Parent)).ToArray();
        _renderer!.UpdateAnimations(0,[item]);
        var actual=_renderer.SkeletonDebug(item.Id,Matrix4.Identity,1);
        if(actual.Count!=expected.Length) throw new InvalidOperationException("Static skeleton debug segment count changed.");
        for(int i=0;i<expected.Length;i++)
        {
            int joint=expected[i],parent=model.Nodes[joint].Parent;
            if((actual[i].Start-world[parent].ExtractTranslation()).Length>1e-4f||
               (actual[i].End-world[joint].ExtractTranslation()).Length>1e-4f)
                throw new InvalidOperationException("Static skeleton overlay applied character correction or wrong space.");
        }
        _renderer.ResetAnimations();_renderer.Prepare(_objects);
        Console.WriteLine("Exercise same model GUID switched character/static: bind skeleton space matches raw model nodes.");
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        if(_renderer is null||_ui is null||FramebufferSize.X<=0||FramebufferSize.Y<=0) return;
        RenderSettings settings=(_runtime??_design).Document.Rendering;
        if(!Playing) settings=new RenderSettings { Pipeline=_activePipeline,SunDirection=settings.SunDirection,SunColor=settings.SunColor,
            SunIntensity=settings.SunIntensity,Exposure=settings.Exposure,Shadows=settings.Shadows,Fxaa=settings.Fxaa };
        _renderer.Render(_camera,_objects,settings,_particles.Visuals(),_debug);
        if(_options.Exercise&&_frame==125) ComparePipelines(settings);
        _ui.Render();
        var error=GL.GetError();if(error!=OpenTK.Graphics.OpenGL4.ErrorCode.NoError) throw new InvalidOperationException("GL frame error: "+error);
        if(_options.CaptureRoot is not null && (_frame==60||_frame==105||_frame==125||_frame==144||_frame==155||_frame==170||_frame==230||_frame==235))
        {
            string label=_frame switch {60=>"forward",105=>"jump",125=>"deferred",144=>"landed",155=>"normals",170=>"shadow",230=>"material-probe",_=>"material-probe-no-shadow"};
            FramebufferCapture.Save(Path.Combine(_options.CaptureRoot,label+".png"),FramebufferSize.X,FramebufferSize.Y);
        }
        SwapBuffers();
        if(_options.Frames>0&&_frame>=_options.Frames)
        {
            if(_options.Exercise&&(_exerciseJumpEvents<2||_exerciseMovementFrames<30))
                throw new InvalidOperationException($"Exercise movement/animation chain failed: jump events {_exerciseJumpEvents}, moving steps {_exerciseMovementFrames}.");
            Console.WriteLine($"Graphics exercise reached {_frame} frames; GL errors: none; jump events {_exerciseJumpEvents}; moving steps {_exerciseMovementFrames}.");Close();
        }
    }

    protected override void OnUnload() { DisposeResources();base.OnUnload(); }

    private void ComparePipelines(RenderSettings settings)
    {
        RenderPipelineMode original=settings.Pipeline;
        byte[] ReadPixels()
        {
            byte[] data=new byte[checked(FramebufferSize.X*FramebufferSize.Y*4)];
            GL.ReadPixels(0,0,FramebufferSize.X,FramebufferSize.Y,PixelFormat.Rgba,PixelType.UnsignedByte,data);
            return data;
        }
        try
        {
            settings.Pipeline=RenderPipelineMode.Forward;_renderer!.Render(_camera,_objects,settings,_particles.Visuals(),RenderDebugView.Final);
            byte[] forward=ReadPixels();
            settings.Pipeline=RenderPipelineMode.Deferred;_renderer.Render(_camera,_objects,settings,_particles.Visuals(),RenderDebugView.Final);
            byte[] deferred=ReadPixels();long difference=0;int maximum=0;
            for(int i=0;i<forward.Length;i++) if((i&3)!=3) { int value=Math.Abs(forward[i]-deferred[i]);difference+=value;maximum=Math.Max(maximum,value); }
            double mean=difference/(forward.Length/4.0*3);
            Console.WriteLine($"Same-frame Forward/Deferred RGB difference: mean {mean:F4}/255; max {maximum}/255.");
            if(mean>3) throw new InvalidOperationException("Same-scene pipeline comparison exceeds the documented quantization tolerance.");
        }
        finally { settings.Pipeline=original; }
    }

    public void DisposeResources()
    {
        if(_disposed) return;
        // 外层finally也调用本方法；GL释放必须发生在窗口Context仍有效时。
        Exception? failure=null;
        void Release(Action action) { try { action(); } catch(Exception error) { failure??=error; } }
        if(_audio is not null) Release(_audio.Dispose);
        if(_simulation is not null) Release(_simulation.Dispose);
        if(_renderer is not null) Release(_renderer.Dispose);
        if(_ui is not null) Release(_ui.Dispose);
        _disposed=true;
        if(failure is not null) Console.Error.WriteLine("Cleanup failure: "+failure);
    }
}
