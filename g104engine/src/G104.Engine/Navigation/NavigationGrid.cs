using G104.Engine.Physics;
using OpenTK.Mathematics;

namespace G104.Engine.Navigation;

/// <summary>单层XZ平面网格的范围、采样尺寸及搜索预算，世界长度使用米。</summary>
public sealed class NavigationSettings
{
    /// <summary>导航矩形包含的最小X/Z边界。</summary>
    public Vector2 Minimum { get; set; } = new(-12, -12);
    /// <summary>导航矩形排除的最大X/Z边界，不因格数向上取整而扩大。</summary>
    public Vector2 Maximum { get; set; } = new(12, 12);
    /// <summary>导航脚底所处的统一世界高度，不表示多层或坡面导航。</summary>
    public float PlaneY { get; set; }
    /// <summary>完整格子的边长；末格按实际矩形边界裁剪。</summary>
    public float CellSize { get; set; } = 0.65f;
    /// <summary>物理净空与连接扫掠使用的角色胶囊半径。</summary>
    public float AgentRadius { get; set; } = 0.35f;
    /// <summary>物理净空与连接扫掠使用的角色胶囊总高。</summary>
    public float AgentHeight { get; set; } = 1.9f;
    /// <summary>一次A*允许展开的节点预算，超限返回明确结果。</summary>
    public int SearchBudget { get; set; } = 10000;
}

/// <summary>寻路结果：Success成功；NoPath断路；InvalidStart起点无效；InvalidGoal终点无效；SearchBudgetExceeded超预算。</summary>
public enum NavigationResult { Success, NoPath, InvalidStart, InvalidGoal, SearchBudgetExceeded }
/// <summary>路径执行状态：Idle空闲；Following跟随；Arrived到达；Blocked运动受阻；NoPath寻路失败。</summary>
public enum PathTaskState { Idle, Following, Arrived, Blocked, NoPath }
/// <summary>一次路径搜索的结果快照，供执行层判断是否需要重规划。</summary>
/// <param name="Result">搜索成功或失败的具体原因。</param>
/// <param name="Waypoints">成功路径的世界空间脚底路点，含投影后的实际端点。</param>
/// <param name="ExpandedNodes">本次搜索已展开或尝试计入预算的节点数。</param>
/// <param name="Revision">搜索时使用的网格版本。</param>
public sealed record NavigationPath(NavigationResult Result, IReadOnlyList<Vector3> Waypoints, int ExpandedNodes, int Revision);

// 单层受控平面网格：从同一物理描述采样胶囊净空/地面，坡台和多层不属于此导航表示。
/// <summary>从物理世界采样或占用数组导入单层网格，提供八邻域A*与净空简化。</summary>
public sealed class NavigationGrid
{
    /// <summary>固定八邻域次序，直连与对角代价和启发式一致。</summary>
    private static readonly (int X, int Z)[] Directions = [(1, 0), (0, 1), (-1, 0), (0, -1), (1, 1), (-1, 1), (-1, -1), (1, -1)];
    /// <summary>借用的采样与扫掠世界；纯占用数组模式为null且不能Rebuild。</summary>
    private readonly PhysicsWorld? _world;
    /// <summary>按Z行、X列展开的格子可走标记。</summary>
    private readonly bool[] _walkable;
    /// <summary>缓存无向格心连接的净空结果，重建时全部失效。</summary>
    private readonly Dictionary<(int, int), bool> _edgeCache = [];
    /// <summary>本网格使用的范围与代理参数引用，构造时校验几何范围及预算。</summary>
    public NavigationSettings Settings { get; }
    /// <summary>X方向分配的列数，尾格是否有实际区间需另行判断。</summary>
    public int Width { get; }
    /// <summary>Z方向分配的行数。</summary>
    public int Depth { get; }
    /// <summary>占用数据版本，供NPC识别重建后需要重新规划的路径。</summary>
    public int Revision { get; private set; }
    /// <summary>当前可走标记的数量，供诊断和巡逻目标生成使用。</summary>
    public int WalkableCount => _walkable.Count(value => value);

    /// <summary>按实际矩形跨度分配有限网格，并立即采样水平支撑和胶囊净空。</summary>
    public NavigationGrid(PhysicsWorld world, NavigationSettings? settings = null)
    {
        _world = world;
        Settings = settings ?? new NavigationSettings();
        Validate(Settings);
        // 容量用double跨度，避免float相减吞掉仍有可表示位置的非零小尾格。
        Width = (int)Math.Ceiling(((double)Settings.Maximum.X - Settings.Minimum.X) / Settings.CellSize);
        Depth = (int)Math.Ceiling(((double)Settings.Maximum.Y - Settings.Minimum.Y) / Settings.CellSize);
        if ((long)Width * Depth > 20000) throw new ArgumentException("Navigation area exceeds the V1 grid limit of 20,000 cells.");
        _walkable = new bool[Width * Depth];
        Rebuild();
    }

    // 独立搜索验收不依赖原生库；此构造也可加载已确认的平面占用数据。
    /// <summary>复制已确认占用数据并过滤范围外格子，不创建或持有原生物理世界。</summary>
    public NavigationGrid(bool[,] cells, NavigationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(cells);
        Settings = settings ?? new NavigationSettings();
        Validate(Settings);
        Width = cells.GetLength(0); Depth = cells.GetLength(1);
        if (Width == 0 || Depth == 0) throw new ArgumentException("Navigation grid cannot be empty.");
        _walkable = new bool[Width * Depth];
        for (int z = 0; z < Depth; z++)
        for (int x = 0; x < Width; x++) _walkable[Index(x, z)] = CellInsideBounds(x, z) && cells[x, z];
        Revision = 1;
    }

    /// <summary>重新采样物理支撑与净空，清连接缓存并提升版本；导入模式拒绝此操作。</summary>
    public void Rebuild()
    {
        if (_world is null) throw new InvalidOperationException("An imported occupancy grid has no physics world to rebuild from.");
        _edgeCache.Clear();
        for (int z = 0; z < Depth; z++)
        for (int x = 0; x < Width; x++)
        {
            // 浮点格界舍入可能留下零宽尾格；它没有采样中心，不能交给物理查询。
            if (!CellInsideBounds(x, z)) { _walkable[Index(x, z)] = false; continue; }
            var position = CellCenter(x, z);
            var support = _world.Raycast(position + Vector3.UnitY * 0.2f, -Vector3.UnitY, 0.4f);
            _walkable[Index(x, z)] = support is { } ground && ground.Normal.Y > 0.95f &&
                MathF.Abs(ground.Position.Y - Settings.PlaneY) <= 0.08f &&
                _world.CanOccupy(position + Vector3.UnitY * 0.035f, Settings.AgentRadius, Settings.AgentHeight);
        }
        Revision++;
    }

    /// <summary>返回实际裁剪区间内的可表示格心，避免尾格中点舍入到排除边界。</summary>
    public Vector3 CellCenter(int x, int z)
    {
        if (!CellInsideBounds(x, z)) throw new ArgumentOutOfRangeException(nameof(x), "Cell must intersect the configured navigation rectangle.");
        // ceil仅分配末格；实际区间仍是[Minimum,Maximum)，末格中心取裁剪后的中点。
        var minimumX = Settings.Minimum.X + x * Settings.CellSize;
        var minimumZ = Settings.Minimum.Y + z * Settings.CellSize;
        var maximumX = MathF.Min(minimumX + Settings.CellSize, Settings.Maximum.X);
        var maximumZ = MathF.Min(minimumZ + Settings.CellSize, Settings.Maximum.Y);
        // 仅剩一个float ULP时，中点可能舍入为排除的最大值；取仍在格内的可表示坐标。
        var centerX = MathF.Min(minimumX + (maximumX - minimumX) * 0.5f, MathF.BitDecrement(maximumX));
        var centerZ = MathF.Min(minimumZ + (maximumZ - minimumZ) * 0.5f, MathF.BitDecrement(maximumZ));
        return new Vector3(centerX, Settings.PlaneY, centerZ);
    }
    /// <summary>同时检查分配索引、真实区间和占用标记。</summary>
    public bool IsWalkable(int x, int z) => CellInsideBounds(x, z) && _walkable[Index(x, z)];
    /// <summary>枚举当前可走格心，所有输出均受真实矩形范围约束。</summary>
    public IEnumerable<Vector3> WalkableCells()
    {
        for (int z = 0; z < Depth; z++)
        for (int x = 0; x < Width; x++) if (IsWalkable(x, z)) yield return CellCenter(x, z);
    }

    /// <summary>校验实际端点及其格心连接，再以有限预算执行A*并简化可通行路径。</summary>
    public NavigationPath FindPath(Vector3 start, Vector3 goal)
    {
        if (!TryCell(start, out var startX, out var startZ) || !IsWalkable(startX, startZ))
            return new NavigationPath(NavigationResult.InvalidStart, [], 0, Revision);
        if (!TryCell(goal, out var goalX, out var goalZ) || !IsWalkable(goalX, goalZ) || MathF.Abs(goal.Y - Settings.PlaneY) > 0.25f)
            return new NavigationPath(NavigationResult.InvalidGoal, [], 0, Revision);
        // 实际端点也要验空并连接格心，不能仅凭所在格采样成功就绕过细障碍。
        var projectedStart = new Vector3(start.X, Settings.PlaneY, start.Z);
        var projectedGoal = new Vector3(goal.X, Settings.PlaneY, goal.Z);
        if ((_world is not null && !_world.CanOccupy(projectedStart + Vector3.UnitY * 0.035f, Settings.AgentRadius, Settings.AgentHeight)) ||
            !CanTraverse(projectedStart, CellCenter(startX, startZ)))
            return new NavigationPath(NavigationResult.InvalidStart, [], 0, Revision);
        if ((_world is not null && !_world.CanOccupy(projectedGoal + Vector3.UnitY * 0.035f, Settings.AgentRadius, Settings.AgentHeight)) ||
            !CanTraverse(CellCenter(goalX, goalZ), projectedGoal))
            return new NavigationPath(NavigationResult.InvalidGoal, [], 0, Revision);
        var startIndex = Index(startX, startZ); var goalIndex = Index(goalX, goalZ);
        var cost = new int[_walkable.Length]; Array.Fill(cost, int.MaxValue);
        var cameFrom = new int[_walkable.Length]; Array.Fill(cameFrom, -1);
        var closed = new bool[_walkable.Length];
        // 以索引打破相同评分的平局；重复入队由closed标记过滤。
        var open = new PriorityQueue<int, (int Score, int Tie)>();
        cost[startIndex] = 0;
        open.Enqueue(startIndex, (Heuristic(startX, startZ, goalX, goalZ), startIndex));
        var expanded = 0;
        while (open.TryDequeue(out var current, out _))
        {
            if (closed[current]) continue;
            if (++expanded > Settings.SearchBudget) return new NavigationPath(NavigationResult.SearchBudgetExceeded, [], expanded, Revision);
            if (current == goalIndex)
            {
                var cells = new List<int>();
                for (int cursor = current; cursor >= 0; cursor = cameFrom[cursor]) cells.Add(cursor);
                cells.Reverse();
                var route = cells.Select(cell => CellCenter(cell % Width, cell / Width)).ToList();
                route.Insert(0, projectedStart);
                route.Add(projectedGoal);
                return new NavigationPath(NavigationResult.Success, Simplify(route), expanded, Revision);
            }
            closed[current] = true;
            var x = current % Width; var z = current / Width;
            foreach (var (dx, dz) in Directions)
            {
                var nextX = x + dx; var nextZ = z + dz;
                if (!IsWalkable(nextX, nextZ)) continue;
                // 斜向穿越必须两侧都有净空，不能从障碍角点挤过去。
                if (dx != 0 && dz != 0 && (!IsWalkable(x + dx, z) || !IsWalkable(x, z + dz))) continue;
                var next = Index(nextX, nextZ);
                if (closed[next] || !EdgeClear(current, next)) continue;
                var candidate = cost[current] + (dx == 0 || dz == 0 ? 1000 : 1414);
                if (candidate >= cost[next]) continue;
                cost[next] = candidate; cameFrom[next] = current;
                open.Enqueue(next, (candidate + Heuristic(nextX, nextZ, goalX, goalZ), next));
            }
        }
        return new NavigationPath(NavigationResult.NoPath, [], expanded, Revision);
    }

    /// <summary>检查线段端点范围、可走格与角点，并在物理模式下扫掠完整代理胶囊。</summary>
    public bool CanTraverse(Vector3 from, Vector3 to)
    {
        if (!TryCell(from, out _, out _) || !TryCell(to, out _, out _)) return false;
        if (_world is not null && _world.SweepCapsule(from + Vector3.UnitY * 0.035f, Settings.AgentRadius, Settings.AgentHeight, to - from) is not null) return false;
        // 物理扫掠之外仍沿线检查占用数据，纯数组模式也拒绝穿角和跨越阻塞格。
        var steps = Math.Max(1, (int)MathF.Ceiling((to - from).Length / (Settings.CellSize * 0.2f)));
        int previousX = -1, previousZ = -1;
        for (int step = 0; step <= steps; step++)
        {
            var point = step == 0 ? from : step == steps ? to : Vector3.Lerp(from, to, (float)step / steps);
            if (!TryCell(point, out var x, out var z) || !IsWalkable(x, z)) return false;
            if (previousX >= 0 && x != previousX && z != previousZ &&
                (!IsWalkable(previousX, z) || !IsWalkable(x, previousZ))) return false;
            previousX = x; previousZ = z;
        }
        return true;
    }

    /// <summary>按无向索引对缓存相邻格心连接，避免一次搜索重复物理查询。</summary>
    private bool EdgeClear(int from, int to)
    {
        var key = from < to ? (from, to) : (to, from);
        if (!_edgeCache.TryGetValue(key, out var clear))
            _edgeCache[key] = clear = CanTraverse(CellCenter(from % Width, from / Width), CellCenter(to % Width, to / Width));
        return clear;
    }
    /// <summary>从各锚点保留可直达的最远连续路点，每条捷径仍执行净空与格子检查。</summary>
    private IReadOnlyList<Vector3> Simplify(List<Vector3> route)
    {
        if (route.Count <= 2) return route;
        var result = new List<Vector3> { route[0] };
        var anchor = 0;
        while (anchor < route.Count - 1)
        {
            var farthest = anchor + 1;
            for (int candidate = anchor + 2; candidate < route.Count; candidate++)
            {
                if (!CanTraverse(route[anchor], route[candidate])) break;
                farthest = candidate;
            }
            result.Add(route[farthest]); anchor = farthest;
        }
        return result;
    }
    /// <summary>将有限位置映射到真实半开矩形内的已分配格，拒绝缺失或零宽尾区间。</summary>
    private bool TryCell(Vector3 position, out int x, out int z)
    {
        x = z = -1;
        if (!float.IsFinite(position.Y) || !(position.X >= Settings.Minimum.X && position.X < Settings.Maximum.X &&
            position.Z >= Settings.Minimum.Y && position.Z < Settings.Maximum.Y)) return false;
        x = CellCoordinate(position.X, Settings.Minimum.X, Width);
        z = CellCoordinate(position.Z, Settings.Minimum.Y, Depth);
        return CellInsideBounds(x, z);
    }
    /// <summary>先限制导入数组覆盖范围，再按采样使用的float格下界校正舍入索引。</summary>
    private int CellCoordinate(float position, float minimum, int count)
    {
        // 导入数组可能只覆盖配置范围的一部分，不能把缺失区域夹进最后一格。
        if (position >= minimum + count * Settings.CellSize) return -1;
        var cell = Math.Clamp((int)MathF.Floor((position - minimum) / Settings.CellSize), 0, count - 1);
        // 减法/除法舍入可能跨过真实float下界；与采样相同的下界负责最终归格。
        while (cell > 0 && position < minimum + cell * Settings.CellSize) cell--;
        while (cell + 1 < count && position >= minimum + (cell + 1) * Settings.CellSize) cell++;
        return cell;
    }
    /// <summary>检查索引与格子实际下界，过滤向上取整产生的零宽尾格。</summary>
    private bool CellInsideBounds(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Depth &&
        Settings.Minimum.X + x * Settings.CellSize < Settings.Maximum.X &&
        Settings.Minimum.Y + z * Settings.CellSize < Settings.Maximum.Y;
    /// <summary>将有效X/Z坐标映射到按行存储的一维索引。</summary>
    private int Index(int x, int z) => z * Width + x;
    /// <summary>使用与1000/1414移动代价匹配的Octile距离估计剩余成本。</summary>
    private static int Heuristic(int x, int z, int goalX, int goalZ)
    {
        int dx = Math.Abs(goalX - x), dz = Math.Abs(goalZ - z);
        return 1000 * Math.Max(dx, dz) + 414 * Math.Min(dx, dz);
    }
    /// <summary>检查有限平面范围、正格长和搜索预算；代理尺寸由物理查询边界约束。</summary>
    private static void Validate(NavigationSettings settings)
    {
        if (!float.IsFinite(settings.Minimum.X) || !float.IsFinite(settings.Minimum.Y) || !float.IsFinite(settings.Maximum.X) ||
            !float.IsFinite(settings.Maximum.Y) || !float.IsFinite(settings.PlaneY) || !float.IsFinite(settings.CellSize) || settings.CellSize <= 0 || settings.Maximum.X <= settings.Minimum.X ||
            settings.Maximum.Y <= settings.Minimum.Y || settings.SearchBudget <= 0)
            throw new ArgumentException("Invalid navigation dimensions or search budget.");
    }
}
