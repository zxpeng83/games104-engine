using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Engine.Physics;

public static class PhysicsVerification
{
    public static void Run()
    {
        var ownersBefore = PhysicsWorld.ActiveWorlds;
        var document = new SceneDocument();
        var floor = Box("Floor", new Vector3(0, -0.5f, 0), new Vector3(24, 1, 24));
        var wall = Box("Wall", new Vector3(0, 1.5f, -3), new Vector3(8, 3, 0.4f));
        var step = Box("Step", new Vector3(5, 0.15f, 3), new Vector3(2, 0.3f, 2));
        document.Objects.AddRange([floor, wall, step]);
        var graph = new SceneGraph(document);
        using (var world = new PhysicsWorld(document, graph))
        {
            var hit = world.Raycast(new Vector3(0, 5, 0), -Vector3.UnitY, 10);
            Require(hit is { } ground && ground.ObjectId == floor.Id && MathF.Abs(ground.Position.Y) < 0.02f && ground.Normal.Y > 0.99f, "Native ray query must identify the transformed floor and its normal.");
            var sweep = world.SweepCapsule(new Vector3(0, 0.03f, 0), 0.35f, 1.9f, new Vector3(0, 0, -8));
            Require(sweep is { } swept && swept.ObjectId == wall.Id && swept.Fraction is > 0.25f and < 0.4f && swept.Normal.Z > 0.9f, "Native shape cast must prevent a fast capsule from tunneling through a wall.");
            var overlap = world.OverlapCapsule(new Vector3(0, 0.03f, -3), 0.35f, 1.9f);
            Require(overlap.Any(contact => contact.ObjectId == wall.Id && contact.Depth > 0.1f), "Native overlap must expose penetration depth.");

            var character = new KinematicCharacter(world, new Vector3(0, 0.03f, 0));
            for (int i = 0; i < 120; i++) character.Tick(1f / 60, new Vector3(0, 0, -3), false);
            Require(character.State.Position.Z > -2.5f && character.State.Grounded, "Character must stop in front of the wall and remain grounded.");
            var beforeSlide = character.State.Position;
            for (int i = 0; i < 45; i++) character.Tick(1f / 60, new Vector3(2, 0, -2), false);
            Require(character.State.Position.X > beforeSlide.X + 1 && character.State.Position.Z > -2.5f, "Character must slide tangentially along a wall.");

            character.Teleport(new Vector3(0, 0.03f, 3));
            character.Tick(1f / 60, Vector3.Zero, false);
            var jump = character.Tick(1f / 60, Vector3.Zero, true);
            Require(jump.Jumped && !jump.Grounded, "A grounded jump edge must start one jump.");
            var peak = jump.Position.Y;
            var landedCount = 0;
            for (int i = 0; i < 100; i++)
            {
                var state = character.Tick(1f / 60, Vector3.Zero, false);
                peak = MathF.Max(peak, state.Position.Y);
                if (state.Landed) landedCount++;
            }
            Require(peak > 0.8f && character.State.Grounded && character.State.Position.Y < 0.1f && landedCount == 1,
                "Jump must rise, fall and emit exactly one landing transition.");

            character.Teleport(new Vector3(5, 0.03f, 5));
            for (int i = 0; i < 65; i++) character.Tick(1f / 60, new Vector3(0, 0, -2), false);
            Require(character.State.Position.Z < 3.5f && character.State.Position.Y > 0.29f && character.State.Position.Y < 0.4f,
                "A 0.30m stair below the 0.35m limit must be climbed.");

            character.Teleport(new Vector3(0, 0.03f, -3));
            character.Tick(1f / 60, Vector3.Zero, false);
            Require(world.CanOccupy(character.State.Position, 0.35f, 1.9f), "Initial penetration recovery must leave a clear capsule.");

            world.SetEnabled(wall.Id, false);
            Require(world.SweepCapsule(new Vector3(0, 0.03f, 0), 0.35f, 1.9f, new Vector3(0, 0, -8)) is null,
                "Disabled door-like collision must disappear from queries.");
            world.SetEnabled(wall.Id, true);
            Require(world.SweepCapsule(new Vector3(0, 0.03f, 0), 0.35f, 1.9f, new Vector3(0, 0, -8)) is not null,
                "Re-enabled collision must return to the same world.");

            var dynamic = world.CreateDynamicBox(new Vector3(-7, 3, 0), Vector3.One);
            for (int i = 0; i < 180; i++) world.Step(1f / 60);
            Require(MathF.Abs(world.BodyPosition(dynamic).Y - 0.5f) < 0.08f && world.BodySleeping(dynamic),
                "A Jolt dynamic box must fall, settle on the floor and sleep.");
            using var pendingWorld = new PhysicsWorld(document, graph);
            Require(PhysicsWorld.ActiveWorlds == ownersBefore + 2 && pendingWorld.Raycast(new Vector3(0, 5, 0), -Vector3.UnitY, 10) is not null,
                "Two scene scopes must coexist without shutting down the library.");
        }
        Require(PhysicsWorld.ActiveWorlds == ownersBefore, "Disposal must release both native worlds.");
        VerifyParentTransform();
        VerifyCeiling();
        VerifySlopeAndStepLimit();
    }

    private static void VerifyParentTransform()
    {
        var parent = new SceneObjectData
        {
            Name = "Rotated parent", Kind = ObjectKind.Group,
            Transform = new TransformData { Position = new Float3(5, 0, 0), Scale = new Float3(2, 2, 2), Rotation = RotationData.From(Quaternion.FromAxisAngle(Vector3.UnitY, MathF.PI / 2)) }
        };
        var child = Box("Scaled child", new Vector3(0, 1, 2), new Vector3(1, 1, 2)); child.ParentId = parent.Id;
        child.Collider!.Center = new Float3(0.1f, 0, 0);
        var document = new SceneDocument { Objects = [parent, child] };
        var graph = new SceneGraph(document);
        using var world = new PhysicsWorld(document, graph);
        var center = Vector3.TransformPosition(child.Collider.Center.ToVector(), graph.WorldMatrix(child.Id));
        var hit = world.Raycast(center + Vector3.UnitY * 5, -Vector3.UnitY, 10);
        Require(hit is { } value && value.ObjectId == child.Id && MathF.Abs(value.Position.Y - (center.Y + 1)) < 0.03f,
            "Collision size, center offset and orientation must inherit the parent's transform exactly once.");
    }

    private static void VerifyCeiling()
    {
        var document = new SceneDocument { Objects = [Box("Floor", new Vector3(0, -0.5f, 0), new Vector3(6, 1, 6)), Box("Ceiling", new Vector3(0, 2.3f, 0), new Vector3(3, 0.4f, 3))] };
        using var world = new PhysicsWorld(document, new SceneGraph(document));
        var character = new KinematicCharacter(world, new Vector3(0, 0.03f, 0));
        character.Tick(1f / 60, Vector3.Zero, false);
        character.Tick(1f / 60, Vector3.Zero, true);
        var peak = 0f;
        for (int i = 0; i < 90; i++)
        {
            character.Tick(1f / 60, Vector3.Zero, false);
            peak = MathF.Max(peak, character.State.Position.Y);
        }
        Require(peak < 0.23f && character.State.Grounded, "A ceiling must cancel upward velocity without leaving the character suspended.");
    }

    private static void VerifySlopeAndStepLimit()
    {
        var ramp = Box("30 degree ramp", new Vector3(0, 0.9f, 0), new Vector3(3, 0.2f, 4));
        ramp.Transform.Rotation = RotationData.From(Quaternion.FromAxisAngle(Vector3.UnitX, -MathF.PI / 6));
        var document = new SceneDocument { Objects = [Box("Floor", new Vector3(0, -0.5f, 0), new Vector3(12, 1, 12)), ramp] };
        using (var world = new PhysicsWorld(document, new SceneGraph(document)))
        {
            var walker = new KinematicCharacter(world, new Vector3(0, 0.03f, -3));
            for (int i = 0; i < 90; i++) walker.Tick(1f / 60, new Vector3(0, 0, 2), false);
            Require(walker.State.Position.Y > 0.75f && walker.State.Grounded, $"A 30 degree ramp must be walkable below the 45 degree limit; actual {walker.State.Position}.");
            var limited = new KinematicCharacter(world, new Vector3(0, 0.03f, -3), new CharacterSettings { MaxSlopeDegrees = 15 });
            for (int i = 0; i < 100; i++) limited.Tick(1f / 60, new Vector3(0, 0, 2), false);
            Require(limited.State.Position.Y < 0.4f && limited.State.Position.Z < -0.7f,
                $"A slope above the configured limit must block ascent; actual {limited.State.Position}.");
        }
        var tallStep = Box("0.5m step", new Vector3(0, 0.25f, 0), new Vector3(3, 0.5f, 2));
        var stepDocument = new SceneDocument { Objects = [Box("Floor", new Vector3(0, -0.5f, 0), new Vector3(8, 1, 8)), tallStep] };
        using var stepWorld = new PhysicsWorld(stepDocument, new SceneGraph(stepDocument));
        var character = new KinematicCharacter(stepWorld, new Vector3(0, 0.03f, 2));
        for (int i = 0; i < 80; i++) character.Tick(1f / 60, new Vector3(0, 0, -2), false);
        Require(character.State.Position.Z > 1.2f && character.State.Position.Y < 0.1f,
            $"A step above the 0.35m height limit must block movement; actual {character.State.Position}.");
    }

    private static SceneObjectData Box(string name, Vector3 position, Vector3 size) => new()
    {
        Name = name, Collider = new ColliderData { Kind = ColliderKind.Box, Size = Float3.From(size) },
        Transform = new TransformData { Position = Float3.From(position) }
    };
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
