using G104.Engine.Physics;
using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Engine.Navigation;

public static class NavigationVerification
{
    public static void Run()
    {
        VerifyNonIntegralBounds();
        var cells = new bool[7, 7];
        for (int z = 0; z < 7; z++)
        for (int x = 0; x < 7; x++) cells[x, z] = true;
        for (int z = 0; z < 6; z++) cells[3, z] = false;
        var settings = new NavigationSettings { Minimum = Vector2.Zero, Maximum = new Vector2(7), CellSize = 1 };
        var grid = new NavigationGrid(cells, settings);
        var start = grid.CellCenter(1, 1); var goal = grid.CellCenter(5, 1);
        var path = grid.FindPath(start, goal);
        Require(path.Result == NavigationResult.Success && path.Waypoints.Count > 2, "A* must route around a separating wall.");
        for (int i = 1; i < path.Waypoints.Count; i++) Require(grid.CanTraverse(path.Waypoints[i - 1], path.Waypoints[i]), "Simplification crossed an occupied cell.");
        cells[3, 6] = false;
        var blockedGrid = new NavigationGrid(cells, settings);
        Require(blockedGrid.FindPath(start, goal).Result == NavigationResult.NoPath, "A* must report a disconnected goal.");
        var corner = new bool[,] { { true, false }, { false, true } };
        var cornerGrid = new NavigationGrid(corner, settings);
        Require(cornerGrid.FindPath(cornerGrid.CellCenter(0, 0), cornerGrid.CellCenter(1, 1)).Result == NavigationResult.NoPath,
            "Diagonal corner cutting must be rejected.");
        var limited = new NavigationGrid(cells, new NavigationSettings { Minimum = Vector2.Zero, Maximum = new Vector2(7), CellSize = 1, SearchBudget = 1 });
        Require(limited.FindPath(start, goal).Result == NavigationResult.SearchBudgetExceeded, "Search budget must be observable.");
        VerifyPhysicsEndpoints();
    }

    public static void VerifyNonIntegralBounds()
    {
        VerifyRoundedTailSampling();
        VerifyInteriorMaximumRounding();
        VerifySingleUlpTailAllocation();
        var cells = new bool[3, 3];
        for (int z = 0; z < 3; z++)
        for (int x = 0; x < 3; x++) cells[x, z] = true;
        var settings = new NavigationSettings { Minimum = Vector2.Zero, Maximum = new Vector2(2.1f), CellSize = 1 };
        var grid = new NavigationGrid(cells, settings);
        var start = new Vector3(0.5f, 0, 0.5f);
        Require(grid.FindPath(start, new Vector3(2.8f, 0, 0.5f)).Result == NavigationResult.InvalidGoal,
            "Ceiling the grid dimensions must not extend the configured X boundary from 2.1 to 3.");
        Require(grid.FindPath(start, new Vector3(0.5f, 0, 2.8f)).Result == NavigationResult.InvalidGoal,
            "Ceiling the grid dimensions must not extend the configured Z boundary from 2.1 to 3.");
        Require(grid.FindPath(new Vector3(2.8f, 0, 0.5f), start).Result == NavigationResult.InvalidStart,
            "A start inside an allocated tail cell but outside actual bounds must be rejected.");
        Require(!grid.CanTraverse(start, new Vector3(2.8f, 0, 0.5f)), "Traversal cannot leave the configured rectangle.");
        foreach (var maximum in new[] { 2.1f, 3f })
        {
            var boundaryGrid = new NavigationGrid(cells, new NavigationSettings { Minimum = Vector2.Zero, Maximum = new Vector2(maximum), CellSize = 1 });
            Require(boundaryGrid.FindPath(start, new Vector3(maximum, 0, 0.5f)).Result == NavigationResult.InvalidGoal &&
                boundaryGrid.FindPath(start, new Vector3(0.5f, 0, maximum)).Result == NavigationResult.InvalidGoal,
                "Maximum boundaries must be exclusive for both integral and clipped grids.");
        }
        bool Inside(Vector3 point) => point.X >= 0 && point.Z >= 0 && point.X < 2.1f && point.Z < 2.1f;
        Require((grid.CellCenter(2, 2) - new Vector3(2.05f, 0, 2.05f)).Length < 0.00001f,
            "The final cell must use the center of its clipped interval [2,2.1).");
        Require(grid.WalkableCells().All(Inside), "Every advertised walkable cell center must lie in actual bounds.");
        var goal = new Vector3(2.09f, 0, 2.09f);
        var path = grid.FindPath(Vector3.Zero, goal);
        Require(path.Result == NavigationResult.Success && path.Waypoints.All(Inside),
            "The inclusive minimum and interior clipped-cell goal must connect with all waypoints in bounds.");
        for (int i = 1; i < path.Waypoints.Count; i++)
            Require(grid.CanTraverse(path.Waypoints[i - 1], path.Waypoints[i]), "A clipped-cell path segment must remain traversable.");

        var narrowMinimum = 0.100000225f;
        var narrowLower = narrowMinimum + 2;
        var narrowMaximum = MathF.BitIncrement(narrowLower);
        var narrow = new NavigationGrid(cells, new NavigationSettings
        {
            Minimum = new Vector2(narrowMinimum), Maximum = new Vector2(narrowMaximum), CellSize = 1
        });
        var narrowCenter = narrow.CellCenter(2, 2);
        Require(narrowCenter.X >= narrowLower && narrowCenter.Z >= narrowLower && narrowCenter.X < narrowMaximum && narrowCenter.Z < narrowMaximum,
            "A one-ULP final cell must not round its midpoint to the exclusive maximum boundary.");
        Require(narrow.FindPath(narrow.CellCenter(0, 0), narrowCenter).Result == NavigationResult.Success,
            "The representable center of a one-ULP final cell must remain a valid path endpoint.");

        var document = new SceneDocument
        {
            Objects = [new SceneObjectData { Name = "Bounded floor", Transform = new TransformData { Position = new Float3(1.05f, -0.5f, 1.05f) },
                Collider = new ColliderData { Size = new Float3(2.1f, 1, 2.1f) } }]
        };
        using var world = new PhysicsWorld(document, new SceneGraph(document));
        var sampled = new NavigationGrid(world, settings);
        Require(sampled.IsWalkable(2, 2) && sampled.WalkableCells().All(Inside),
            "Physics sampling must query the clipped final cell inside the bounded floor.");
        Require(sampled.FindPath(start, goal).Result == NavigationResult.Success,
            "Physics-backed navigation must connect valid positions in the final clipped cell.");
    }

    public static void VerifyRoundedTailSampling()
    {
        var document = new SceneDocument
        {
            Objects = [new SceneObjectData { Name = "Rounded-tail floor", Transform = new TransformData { Position = new Float3(-2.25f, -0.5f, 0) },
                Collider = new ColliderData { Size = new Float3(19.5f, 1, 24) } }]
        };
        using var world = new PhysicsWorld(document, new SceneGraph(document));
        // 19.5 / float(0.65)向上取整为31，但x=30的实际float下界已经等于7.5。
        var settings = new NavigationSettings { Maximum = new Vector2(7.5f, 12) };
        var grid = new NavigationGrid(world, settings);
        Require(grid.Width == 31 && !grid.IsWalkable(30, 0) && grid.WalkableCount > 0,
            "An allocated zero-width tail cell must remain unwalkable without breaking physics sampling.");
        Require(grid.WalkableCells().All(point => point.X >= -12 && point.X < 7.5f && point.Z >= -12 && point.Z < 12),
            "Physics sampling must expose only centers inside the actual rounded navigation rectangle.");
        var previousRevision = grid.Revision;
        grid.Rebuild();
        Require(grid.Revision > previousRevision && !grid.IsWalkable(30, 0), "A rebuild must continue to skip the zero-width tail.");
        var start = new Vector3(0, 0, 0);
        var goal = new Vector3(MathF.BitDecrement(7.5f), 0, 0);
        Require(grid.FindPath(start, goal).Result == NavigationResult.Success && grid.FindPath(goal, start).Result == NavigationResult.Success,
            "The last representable interior X endpoint must remain valid in the physics-backed rounded grid.");
        Require(grid.CanTraverse(start, goal), "Rounding into an empty tail cannot reject an otherwise clear interior traversal.");
    }

    public static void VerifyInteriorMaximumRounding()
    {
        foreach (var axis in new[] { 0, 1 })
        {
            var cells = new bool[axis == 0 ? 21 : 37, axis == 0 ? 37 : 21];
            for (int z = 0; z < cells.GetLength(1); z++)
            for (int x = 0; x < cells.GetLength(0); x++) cells[x, z] = true;
            var settings = new NavigationSettings { Maximum = axis == 0 ? new Vector2(1, 12) : new Vector2(12, 1) };
            var grid = new NavigationGrid(cells, settings);
            var interior = MathF.BitDecrement(1);
            var goal = axis == 0 ? new Vector3(interior, 0, 0) : new Vector3(0, 0, interior);
            Require(grid.FindPath(Vector3.Zero, goal).Result == NavigationResult.Success &&
                grid.FindPath(goal, Vector3.Zero).Result == NavigationResult.Success,
                "A point strictly below Maximum must not be rejected when float subtraction/division rounds its index into an empty tail.");
            Require(grid.CanTraverse(Vector3.Zero, goal), "Interior endpoint traversal must survive rounded cell-index arithmetic on both axes.");
            var boundary = axis == 0 ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1);
            Require(grid.FindPath(Vector3.Zero, boundary).Result == NavigationResult.InvalidGoal && !grid.CanTraverse(Vector3.Zero, boundary),
                "Correcting an interior rounded index must keep the actual maximum boundary exclusive.");
        }

        var partial = new NavigationGrid(new bool[,] { { true, true }, { true, true } },
            new NavigationSettings { Minimum = Vector2.Zero, Maximum = new Vector2(7), CellSize = 1 });
        Require(partial.FindPath(new Vector3(0.5f, 0, 0.5f), new Vector3(2.8f, 0, 0.5f)).Result == NavigationResult.InvalidGoal &&
            partial.FindPath(new Vector3(2.8f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f)).Result == NavigationResult.InvalidStart &&
            !partial.CanTraverse(new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, 2.8f)),
            "Correcting rounded indices cannot extend a smaller imported occupancy array into unrepresented cells.");
        var partialInterior = MathF.BitDecrement(2);
        Require(partial.FindPath(new Vector3(0.5f, 0, 0.5f), new Vector3(partialInterior, 0, partialInterior)).Result == NavigationResult.Success,
            "The last representable interior position of a smaller imported occupancy array must remain accepted.");
    }

    public static void VerifySingleUlpTailAllocation()
    {
        // 固定大地板与导航边界独立，避免地板形状的舍入混入容量回归。
        var document = new SceneDocument
        {
            Objects = [new SceneObjectData { Name = "Single-ULP-tail floor", Transform = new TransformData { Position = new Float3(0, -0.5f, 0) },
                Collider = new ColliderData { Size = new Float3(32, 1, 32) } }]
        };
        using var world = new PhysicsWorld(document, new SceneGraph(document));
        var maximum = MathF.BitIncrement(1);
        foreach (var axis in new[] { 0, 1 })
        {
            var settings = new NavigationSettings { CellSize = 1, Maximum = axis == 0 ? new Vector2(maximum, 12) : new Vector2(12, maximum) };
            var grid = new NavigationGrid(world, settings);
            var interior = axis == 0 ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1);
            Require(grid.FindPath(Vector3.Zero, interior).Result == NavigationResult.Success &&
                grid.FindPath(interior, Vector3.Zero).Result == NavigationResult.Success,
                "A nonzero one-ULP tail cannot be omitted when float subtraction rounds the overall span to an integer.");
            Require((axis == 0 ? grid.Width : grid.Depth) == 14, "The nonzero interval [1,BitIncrement(1)) must have an allocated fourteenth cell.");
            var center = grid.CellCenter(axis == 0 ? 13 : 12, axis == 0 ? 12 : 13);
            var coordinate = axis == 0 ? center.X : center.Z;
            Require(coordinate >= 1 && coordinate < maximum && grid.FindPath(Vector3.Zero, center).Result == NavigationResult.Success &&
                grid.CanTraverse(center, interior), "The sampled one-ULP tail center must remain in bounds and connect to its interior endpoint.");
            var boundary = axis == 0 ? new Vector3(maximum, 0, 0) : new Vector3(0, 0, maximum);
            Require(grid.FindPath(Vector3.Zero, boundary).Result == NavigationResult.InvalidGoal &&
                grid.FindPath(boundary, Vector3.Zero).Result == NavigationResult.InvalidStart && !grid.CanTraverse(Vector3.Zero, boundary),
                "Allocating a one-ULP tail must keep the actual maximum boundary exclusive.");
        }
    }

    private static void VerifyPhysicsEndpoints()
    {
        var document = new SceneDocument
        {
            Objects =
            [
                new SceneObjectData { Name = "Floor", Transform = new TransformData { Position = new Float3(0, -0.5f, 0) }, Collider = new ColliderData { Size = new Float3(8, 1, 8) } },
                new SceneObjectData { Name = "Subcell wall", Transform = new TransformData { Position = new Float3(0.5f, 1, 0) }, Collider = new ColliderData { Size = new Float3(0.1f, 2, 1.2f) } }
            ]
        };
        using var world = new PhysicsWorld(document, new SceneGraph(document));
        var grid = new NavigationGrid(world, new NavigationSettings { Minimum = new Vector2(-1), Maximum = new Vector2(3), CellSize = 2 });
        var clearCell = grid.CellCenter(0, 0);
        var otherSide = new Vector3(0.95f, 0, 0);
        Require(grid.IsWalkable(0, 0) && world.CanOccupy(otherSide + Vector3.UnitY * 0.035f, 0.35f, 1.9f), "Endpoint fixture must contain two clear positions in one sampled cell.");
        Require(!grid.CanTraverse(clearCell, otherSide), "An unsampled wall must separate the actual endpoint from its cell center.");
        Require(grid.FindPath(grid.CellCenter(1, 1), otherSide).Result == NavigationResult.InvalidGoal,
            "A valid goal cell cannot hide a blocked connection to the actual goal.");
        Require(grid.FindPath(otherSide, grid.CellCenter(1, 1)).Result == NavigationResult.InvalidStart,
            "A valid start cell cannot hide a blocked connection from the actual start.");
        Require(grid.FindPath(grid.CellCenter(1, 1), new Vector3(0.5f, 0, 0)).Result == NavigationResult.InvalidGoal,
            "An actual goal inside a collider must fail even when its sampled cell is clear.");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
