using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Sandbox.Gameplay;

public static class GameplayVerification
{
    public static void Run()
    {
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
