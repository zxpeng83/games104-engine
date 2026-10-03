using G104.Engine.Physics;
using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Sandbox.Gameplay;

public static class GameplayVerification
{
    public static void Run()
    {
        VerifyNpcFacing();
        VerifyNpcFacingTransition();
        VerifyCharacterRadiusContract();
        var document = CreateScene(new Vector3(0, 0.05f, 2));
        var graph = new SceneGraph(document);
        using (var simulation = new TrainingSimulation(document, graph))
        {
            var door = document.Objects.Single(item => item.Kind == ObjectKind.Door);
            var neutral = new GameInput(Vector2.Zero, false, false, false, 0);
            simulation.Tick(1f / 60, neutral);
            Require(!simulation.DoorOpen && simulation.World.IsEnabled(door.Id), "The initial door must block the world.");
            Require(!simulation.Navigation!.CanTraverse(new Vector3(0, 0, 2), new Vector3(0, 0, -2)), "Closed door must obstruct navigation.");
            var revision = simulation.Navigation.Revision;
            simulation.Tick(1f / 60, neutral with { Interact = true });
            Require(simulation.DoorOpen && !simulation.World.IsEnabled(door.Id) && simulation.Navigation.Revision > revision,
                "An accepted interaction must commit door visibility, collision and navigation together.");
            Require(graph.WorldPosition(door.Id).Y > 4 && simulation.Events.Count(item => item.Sound == "button") == 1,
                "Door movement and one interaction fact must be observable.");
            simulation.Tick(1f / 60, neutral);
            Require(simulation.Events.All(item => item.Sound != "button" && item.Sound != "door"), "Interaction feedback cannot repeat on a neutral tick.");
            Require(simulation.Navigation.CanTraverse(new Vector3(0, 0, 2), new Vector3(0, 0, -2)), "Open doorway must be traversable.");
            var completions = 0;
            for (int i = 0; i < 115; i++)
            {
                simulation.Tick(1f / 60, neutral with { Move = Vector2.UnitY });
                completions += simulation.Events.Count(item => item.Sound == "complete");
            }
            Require(simulation.Completed && completions == 1, "Entering the goal after opening the door must complete once.");
            Require(simulation.NpcPath.Count > 0 && simulation.NpcState is not null, "NPC must produce a path and execute the shared character controller.");
        }
        var occupied = CreateScene(new Vector3(0, 0.05f, 2));
        using var guardedSimulation = new TrainingSimulation(occupied, new SceneGraph(occupied));
        var interaction = new GameInput(Vector2.Zero, false, false, true, 0);
        guardedSimulation.Tick(1f / 60, interaction);
        Require(guardedSimulation.DoorOpen, "The guarded test must open the door before entering its volume.");
        for (int i = 0; i < 40; i++) guardedSimulation.Tick(1f / 60, interaction with { Interact = false, Move = Vector2.UnitY });
        guardedSimulation.Tick(1f / 60, interaction);
        Require(guardedSimulation.DoorOpen && guardedSimulation.Events.Any(item => item.Sound == "denied"),
            "Door closing must be rejected while a character occupies the original collision volume.");
        var unlinked = CreateScene(new Vector3(0, 0.05f, 2));
        unlinked.Objects.Single(item => item.Kind == ObjectKind.Button).TargetId = null;
        using var unlinkedSimulation = new TrainingSimulation(unlinked, new SceneGraph(unlinked));
        unlinkedSimulation.Tick(1f / 60, interaction);
        Require(!unlinkedSimulation.DoorOpen && unlinkedSimulation.Events.Any(item => item.Sound == "denied"),
            "A cleared explicit button target must not fall back to another door.");
    }

    public static void VerifyNpcFacing()
    {
        foreach (var (yaw, playerZ, expected) in new[]
        {
            (MathF.PI, 3.5f, NpcMode.Follow),
            (MathF.PI, -2.5f, NpcMode.Patrol),
            (0f, -2.5f, NpcMode.Follow)
        })
        {
            var document = CreatePerceptionScene(new Vector3(0.5f, 0.05f, playerZ), yaw);
            var graph = new SceneGraph(document);
            var npc = document.Objects.Single(item => item.Kind == ObjectKind.Npc);
            var before = graph.WorldMatrix(npc.Id);
            using var simulation = new TrainingSimulation(document, graph);
            simulation.Tick(1f / 60, new GameInput(Vector2.Zero, false, false, false, 0));
            Require(simulation.NpcMode == expected,
                $"A stationary NPC with design yaw {yaw} must perceive using its world -Z forward; expected {expected}, got {simulation.NpcMode}.");
            var beforeForward = Vector3.TransformVector(-Vector3.UnitZ, before).Normalized();
            var afterForward = Vector3.TransformVector(-Vector3.UnitZ, graph.WorldMatrix(npc.Id)).Normalized();
            Require((beforeForward - afterForward).Length < 0.0001f && simulation.NpcState!.Value.Speed < 0.001f,
                "The initial perception fixture must stay stationary and preserve its design orientation.");
        }
    }

    public static void VerifyNpcFacingTransition()
    {
        var document = CreatePerceptionScene(new Vector3(3.5f, 0.05f, 0.5f), 0);
        var npc = document.Objects.Single(item => item.Kind == ObjectKind.Npc);
        npc.Parameters["patrolRadius"] = 2;
        npc.Parameters["fieldOfView"] = 20;
        var graph = new SceneGraph(document);
        using var simulation = new TrainingSimulation(document, graph);
        var neutral = new GameInput(Vector2.Zero, false, false, false, 0);
        for (int i = 0; i < 3; i++) simulation.Tick(0.05f, neutral);
        Require(simulation.NpcMode == NpcMode.Patrol, "The side-on player must initially be outside the narrow field of view.");
        npc.Parameters["walkSpeed"] = 2.3f;
        simulation.Tick(0.05f, neutral); // 本步只生成向+X的下一步意图，随后两步朝向仍在Slerp过渡中。
        for (int i = 0; i < 2; i++)
        {
            simulation.Tick(0.05f, neutral);
            var forward = Vector3.TransformVector(-Vector3.UnitZ, graph.WorldMatrix(npc.Id)).Xz.Normalized();
            var towardPlayer = (simulation.PlayerPosition - simulation.NpcState!.Value.Position).Xz.Normalized();
            Require(Vector2.Dot(forward, towardPlayer) < MathF.Cos(10 * MathF.PI / 180),
                "The transition fixture must keep the player outside the NPC's current logical cone while the target yaw points at them.");
            Require(simulation.NpcMode == NpcMode.Patrol,
                "Perception must not use the future target yaw before the current logical rotation reaches the player.");
        }
        for (int i = 0; i < 15 && simulation.NpcMode != NpcMode.Follow; i++) simulation.Tick(0.05f, neutral);
        Require(simulation.NpcMode == NpcMode.Follow, "The NPC must see the player once its current rotation enters the cone.");
    }

    public static void VerifyCharacterRadiusContract()
    {
        var skin = new CharacterSettings().Skin;
        foreach (var kind in new[] { ObjectKind.Player, ObjectKind.Npc })
        {
            var item = new SceneObjectData { Name = "Radius contract", Kind = kind };
            foreach (var radius in new[] { 0.02f, skin })
            {
                item.Parameters["radius"] = radius;
                var rejected = false;
                try { SceneValidator.ValidateObject(item); }
                catch (SceneValidationException) { rejected = true; }
                Require(rejected, $"Design validation must reject {kind} radius {radius}, matching the controller's default skin {skin}.");
            }
            foreach (var radius in new[] { MathF.BitIncrement(skin), 0.35f })
            {
                var document = CreatePerceptionScene(new Vector3(0.5f, 0.05f, -2.5f), 0);
                var character = document.Objects.Single(value => value.Kind == kind);
                character.Parameters["radius"] = radius;
                SceneValidator.Validate(document);
                using var simulation = new TrainingSimulation(document, new SceneGraph(document));
                Require(simulation.PlayerState.Position.Y == 0.05f && simulation.NpcState is not null,
                    "An accepted radius above the shared default skin must prepare Play for both character kinds.");
            }
        }
    }

    private static SceneDocument CreatePerceptionScene(Vector3 playerPosition, float yaw)
    {
        return new SceneDocument
        {
            Objects =
            [
                new SceneObjectData { Name = "Perception ground", Transform = new TransformData { Position = new Float3(0, -0.5f, 0) }, Collider = new ColliderData { Size = new Float3(12, 1, 12) } },
                new SceneObjectData { Name = "Player", Kind = ObjectKind.Player, Transform = new TransformData { Position = Float3.From(playerPosition) } },
                new SceneObjectData { Name = "NPC", Kind = ObjectKind.Npc,
                    Transform = new TransformData { Position = new Float3(0.5f, 0.05f, 0.5f), Rotation = RotationData.From(Quaternion.FromAxisAngle(Vector3.UnitY, yaw)) },
                    Parameters = new Dictionary<string, float> { ["navMinX"] = -4, ["navMinZ"] = -4, ["navMaxX"] = 4, ["navMaxZ"] = 4,
                        ["navCellSize"] = 1, ["sightRange"] = 10, ["fieldOfView"] = 60, ["patrolRadius"] = 0, ["walkSpeed"] = 0, ["runSpeed"] = 0 } }
            ]
        };
    }

    private static SceneDocument CreateScene(Vector3 playerPosition)
    {
        var door = new SceneObjectData
        {
            Name = "Door", Kind = ObjectKind.Door,
            Transform = new TransformData { Position = new Float3(0, 1.5f, 0), Scale = new Float3(4, 3, 0.3f) },
            Collider = new ColliderData(), Parameters = new Dictionary<string, float> { ["openHeight"] = 3.5f }
        };
        return new SceneDocument
        {
            Objects =
            [
                new SceneObjectData { Name = "Ground", Transform = new TransformData { Position = new Float3(0, -0.5f, 0), Scale = new Float3(28, 1, 28) }, Collider = new ColliderData() },
                new SceneObjectData { Name = "Player", Kind = ObjectKind.Player, Transform = new TransformData { Position = Float3.From(playerPosition) } },
                new SceneObjectData { Name = "NPC", Kind = ObjectKind.Npc, Transform = new TransformData { Position = new Float3(5, 0.05f, 2) } },
                door,
                new SceneObjectData { Name = "Button", Kind = ObjectKind.Button, TargetId = door.Id, Transform = new TransformData { Position = new Float3(-1, 0.75f, 1) }, Parameters = new Dictionary<string, float> { ["interactDistance"] = 2.5f } },
                new SceneObjectData { Name = "Goal", Kind = ObjectKind.Goal, Transform = new TransformData { Position = new Float3(0, 0.05f, -3) }, Parameters = new Dictionary<string, float> { ["radius"] = 1 } }
            ]
        };
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
