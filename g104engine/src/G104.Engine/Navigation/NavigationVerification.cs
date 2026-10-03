using G104.Engine.Physics;
using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Engine.Navigation;

public static class NavigationVerification
{
    public static void Run()
    {
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
