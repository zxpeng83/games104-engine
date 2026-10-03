using G104.Engine.Physics;
using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Sandbox.Gameplay;

// 从正式种子复制独立内存场景；不写用户设计、不创建图形或音频上下文。
public static class ContactApproachVerification
{
    private const float Dt = 1f / 60;

    public static void Run(string assetRoot)
    {
        var seed = SceneSerializer.Load(Path.Combine(assetRoot, "scenes", "training-ground.json"), assetRoot);
        var seedJson = SceneSerializer.Serialize(seed);
        var failures = new List<string>();
        int cases = 0;
        void Check(string name, Action<Context> action)
        {
            cases++;
            var context = new Context(name);
            int owners = PhysicsWorld.ActiveWorlds;
            try
            {
                action(context);
                Require(PhysicsWorld.ActiveWorlds == owners, "The case must release its native world.");
                Console.WriteLine($"PASS contacts: {name}; {context.Describe()}");
            }
            catch (Exception error)
            {
                failures.Add(name);
                Console.Error.WriteLine($"FAIL contacts: {name}; {context.Describe()}; nativeWorlds={PhysicsWorld.ActiveWorlds}/{owners}");
                Console.Error.WriteLine(error.ToString());
            }
        }

        foreach (var kind in new[] { ObjectKind.Player, ObjectKind.Npc })
        foreach (var heading in new[] { "north", "south", "east", "diagonal", "turns" })
            Check($"TRS {kind} {heading} 600 steps", context => VerifyTransformLoop(seed, context, kind, heading));

        var spheres = seed.Objects.Where(item => item.Name.StartsWith("PBR sample ", StringComparison.Ordinal)).ToArray();
        Require(spheres.Length == 6 && spheres.All(item => item.Collider is null),
            "This regression uses the six original render-only PBR spheres; it must not invent sphere colliders.");
        for (int index = 0; index < spheres.Length; index++)
        {
            var sphere = spheres[index];
            var direction = (index % 3) switch
            {
                0 => Vector3.UnitZ,
                1 => -Vector3.UnitZ,
                _ => new Vector3(1, 0, 1).Normalized()
            };
            foreach (bool sprint in new[] { false, true })
                Check($"{sphere.Name} {(sprint ? "run" : "walk")} through visual sphere", context =>
                {
                    var center = sphere.Transform.Position.ToVector();
                    var start = center - direction * 2.2f; start.Y = 0.05f;
                    var metrics = Exercise(seed, context, sphere.Name, start, _ => Input(direction, sprint), Frames(seed, sprint, 4.4f));
                    Require(metrics.MinimumTargetDistance < 0.1f && Vector3.Dot(metrics.End - center, direction) > 1.8f,
                        "The actor must actually cross the sphere's displayed position; no sphere collision is expected.");
                    Require(!metrics.TargetContact && metrics.GroundedFrames > 10,
                        "A render-only sphere must not appear in native capsule hits, and the actor must stay on the floor.");
                });
        }

        var ramp = seed.Objects.Single(item => item.Name == "Ramp");
        foreach (bool sprint in new[] { false, true })
        {
            string pace = sprint ? "run" : "walk";
            Check($"Ramp {pace} uphill", context =>
            {
                var metrics = Exercise(seed, context, ramp.Name, new Vector3(9, 0.05f, -3.6f),
                    _ => Input(Vector3.UnitZ, sprint), Frames(seed, sprint, 5.8f));
                Require(metrics.TargetContact && metrics.TargetSupportFrames > 10 && metrics.End.Z > 1.8f && metrics.End.Y > 0.8f,
                    "The original ramp must be contacted and climbed on its sloped top.");
            });
            Check($"Ramp {pace} downhill", context =>
            {
                var start = new Vector3(9, RampTop(ramp, 9, 3) + 0.05f, 3);
                var metrics = Exercise(seed, context, ramp.Name, start, _ => Input(-Vector3.UnitZ, sprint), Frames(seed, sprint, 6));
                Require(metrics.TargetSupportFrames > 10 && metrics.End.Z < -2.8f && metrics.End.Y < 0.35f && metrics.GroundedFrames > 10,
                    "The actor must descend from the ramp top to the floor.");
            });
            Check($"Ramp {pace} diagonal uphill", context =>
            {
                var metrics = Exercise(seed, context, ramp.Name, new Vector3(8.2f, 0.05f, -3.6f),
                    _ => Input(new Vector3(0.12f, 0, 1).Normalized(), sprint), Frames(seed, sprint, 5.8f));
                Require(metrics.TargetSupportFrames > 10 && metrics.End.Z > 1.8f && metrics.End.Y > 0.8f && metrics.End.X > 8.7f,
                    "Diagonal movement must advance sideways while climbing the original ramp.");
            });
            Check($"Ramp {pace} high side contact", context =>
            {
                var metrics = Exercise(seed, context, ramp.Name, new Vector3(6.2f, 0.05f, 2),
                    _ => Input(Vector3.UnitX, sprint), Frames(seed, sprint, 3));
                Require(metrics.TargetContact && metrics.BlockedFrames > 10 && metrics.End.X is > 7 and < 7.3f && metrics.End.Y < 0.2f,
                    "The ramp's high side exceeds the step limit and must block the actor.");
            });
            foreach (bool south in new[] { false, true })
                Check($"{(south ? "South" : "North")} boundary {pace} contact", context =>
                {
                    var direction = south ? Vector3.UnitZ : -Vector3.UnitZ;
                    var metrics = Exercise(seed, context, south ? "South boundary" : "North boundary", new Vector3(11, 0.05f, south ? 11.5f : -11.5f),
                        _ => Input(direction, sprint), Frames(seed, sprint, 5));
                    Require(metrics.TargetContact && metrics.BlockedFrames > 10 && MathF.Abs(metrics.End.Z) is > 13.7f and < 14.1f && metrics.End.Y < 0.1f,
                        "The actor must reach the original boundary, stop outside it, and remain grounded.");
                });
        }

        foreach (var heading in new[] { "south", "east", "west", "turns" })
            Check($"full seed {heading} walk/run 600 steps", context =>
            {
                var start = new Vector3(0, 0.05f, 7);
                var metrics = Exercise(seed, context, "South boundary", start,
                    frame => Input(Direction(heading, frame), (frame / 75) % 2 == 1), 600);
                Require(metrics.PathLength > 3 && metrics.GroundedFrames > 500,
                    "The long movement regression must execute actual grounded travel, including turns and both speeds.");
            });

        Require(SceneSerializer.Serialize(seed) == seedJson, "All cases must leave the loaded seed unchanged.");
        Console.WriteLine($"Contact approach verification: {cases - failures.Count}/{cases} PASS; failures={failures.Count}.");
        if (failures.Count != 0) throw new InvalidOperationException("Contact approach cases failed: " + string.Join("; ", failures));
    }

    private static void VerifyTransformLoop(SceneDocument seed, Context context, ObjectKind kind, string heading)
    {
        var document = SceneSerializer.Clone(seed);
        var actor = document.Objects.Single(item => item.Kind == kind);
        document.Objects = [actor];
        actor.ParentId = null;
        actor.Transform.Position = Float3.Zero;
        var graph = new SceneGraph(document);
        context.Document = document; context.Graph = graph; context.Target = actor.Name;
        for (int frame = 0; frame < 600; frame++)
        {
            context.Frame = frame; context.Operation = "SetWorldPosition";
            graph.SetWorldPosition(actor.Id, graph.WorldPosition(actor.Id) + Direction(heading, frame) * 0.04f);
            context.Operation = "SetWorldRotation";
            var direction = Direction(heading, frame);
            var desired = Quaternion.FromAxisAngle(Vector3.UnitY, MathF.Atan2(-direction.X, -direction.Z));
            var current = graph.WorldMatrix(actor.Id).ExtractRotation();
            graph.SetWorldRotation(actor.Id, Quaternion.Slerp(current, desired, Dt * 12));
        }
        Require(actor.Transform.Scale == Float3.One, "Position and rotation updates must preserve the actor's exact authored unit scale.");
    }

    private static Metrics Exercise(SceneDocument seed, Context context, string targetName, Vector3 start,
        Func<int, GameInput> inputAtFrame, int frames)
    {
        var document = SceneSerializer.Clone(seed);
        var player = document.Objects.Single(item => item.Kind == ObjectKind.Player);
        var target = document.Objects.Single(item => item.Name == targetName);
        player.Transform.Position = Float3.From(start);
        var graph = new SceneGraph(document);
        context.Document = document; context.Graph = graph; context.Target = $"{target.Name} ({target.Id})";
        context.Operation = "prepare full seed simulation";
        using var simulation = new TrainingSimulation(document, graph);
        context.Simulation = simulation;
        float radius = player.Parameters.GetValueOrDefault("radius", 0.35f);
        float height = player.Parameters.GetValueOrDefault("height", 1.9f);
        Require(simulation.World.CanOccupy(start, radius, height), "The case must begin at a legal capsule position.");
        var metrics = new Metrics { End = start };
        var targetPosition = graph.WorldPosition(target.Id);
        for (int frame = 0; frame < frames; frame++)
        {
            context.Frame = frame; context.Operation = "TrainingSimulation.Tick";
            var before = simulation.PlayerPosition;
            var input = inputAtFrame(frame);
            graph.CapturePrevious();
            simulation.Tick(Dt, input);
            var state = simulation.PlayerState;
            metrics.End = state.Position;
            metrics.PathLength += (state.Position - before).Xz.Length;
            metrics.GroundedFrames += state.Grounded ? 1 : 0;
            metrics.BlockedFrames += state.Blocked ? 1 : 0;
            metrics.MinimumTargetDistance = MathF.Min(metrics.MinimumTargetDistance, (state.Position - targetPosition).Xz.Length);
            var support = simulation.World.SweepCapsule(state.Position, radius, height, -Vector3.UnitY * 0.2f);
            if (support is { } hit && hit.ObjectId == target.Id)
            {
                metrics.TargetContact = true;
                if (state.Grounded && hit.Normal.Y > 0.8f) metrics.TargetSupportFrames++;
            }
            var direction = new Vector3(input.Move.X, 0, -input.Move.Y);
            if (direction.LengthSquared > 0)
            {
                var contact = simulation.World.SweepCapsule(state.Position, radius, height, direction.Normalized() * 0.15f);
                metrics.TargetContact |= contact is { } touching && touching.ObjectId == target.Id;
            }
            Require(TransformMath.Finite(state.Position) && (graph.WorldPosition(player.Id) - state.Position).Length < 0.00001f,
                "The scene graph must contain the finite position produced by the real character controller.");
        }
        context.Operation = $"assert behavior; travel={metrics.PathLength:G9}, targetContact={metrics.TargetContact}, targetSupportFrames={metrics.TargetSupportFrames}, blockedFrames={metrics.BlockedFrames}";
        foreach (var actor in document.Objects.Where(item => item.Kind is ObjectKind.Player or ObjectKind.Npc))
            Require(actor.Transform.Scale == Float3.One, $"{actor.Name} must retain its exact authored unit scale after real simulation ticks.");
        return metrics;
    }

    private static float RampTop(SceneObjectData ramp, float x, float z)
    {
        var matrix = TransformMath.Compose(ramp.Transform);
        var localTop = ramp.Collider!.Center.ToVector() + Vector3.UnitY * ramp.Collider.Size.Y * 0.5f;
        var top = Vector3.TransformPosition(localTop, matrix);
        var normal = Vector3.TransformVector(Vector3.UnitY, Matrix4.CreateFromQuaternion(ramp.Transform.Rotation.ToQuaternion())).Normalized();
        return top.Y - (normal.X * (x - top.X) + normal.Z * (z - top.Z)) / normal.Y;
    }

    private static GameInput Input(Vector3 direction, bool sprint) => new(new Vector2(direction.X, -direction.Z), sprint, false, false, 0);
    private static int Frames(SceneDocument seed, bool sprint, float distance) => (int)MathF.Ceiling(distance /
        (seed.Objects.Single(item => item.Kind == ObjectKind.Player).Parameters[sprint ? "runSpeed" : "walkSpeed"] * Dt));
    private static Vector3 Direction(string heading, int frame) => heading switch
    {
        "north" => -Vector3.UnitZ,
        "south" => Vector3.UnitZ,
        "east" => Vector3.UnitX,
        "west" => -Vector3.UnitX,
        "diagonal" => new Vector3(1, 0, 1).Normalized(),
        _ => ((frame / 30) % 4) switch { 0 => Vector3.UnitZ, 1 => Vector3.UnitX, 2 => -Vector3.UnitZ, _ => -Vector3.UnitX }
    };
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Metrics
    {
        public Vector3 End;
        public float PathLength;
        public float MinimumTargetDistance = float.MaxValue;
        public int GroundedFrames;
        public int BlockedFrames;
        public int TargetSupportFrames;
        public bool TargetContact;
    }

    private sealed class Context(string name)
    {
        public SceneDocument? Document;
        public SceneGraph? Graph;
        public TrainingSimulation? Simulation;
        public string Target = name;
        public int Frame = -1;
        public string Operation = "initialize";

        public string Describe()
        {
            var actors = Document?.Objects.Where(item => item.Kind is ObjectKind.Player or ObjectKind.Npc)
                .Select(item => $"{item.Kind}={item.Name}({item.Id}) position={item.Transform.Position} scale={item.Transform.Scale} rotation={item.Transform.Rotation}") ?? [];
            // SceneGraph失败会回滚非法候选；这里明确打印最后合法DTO以及已推进的控制器坐标。
            return $"target={Target}; frame={Frame}; operation={Operation}; lastAccepted=[{string.Join("; ", actors)}]; controllerPlayer={Simulation?.PlayerPosition}; controllerNpc={Simulation?.NpcState?.Position}";
        }
    }
}
