using OpenTK.Mathematics;

namespace G104.Engine.Rendering;

internal static class PrimitiveMeshes
{
    public static GpuMesh Cube()
    {
        var vertices = new List<float>(); var indices = new List<uint>();
        void Face(Vector3 n, Vector3 tangent, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            uint start = (uint)(vertices.Count / 20);
            Add(vertices, a, n, new(0, 0), tangent); Add(vertices, b, n, new(1, 0), tangent);
            Add(vertices, c, n, new(1, 1), tangent); Add(vertices, d, n, new(0, 1), tangent);
            indices.AddRange([start, start + 1, start + 2, start, start + 2, start + 3]);
        }
        const float h = .5f;
        Face(Vector3.UnitZ, Vector3.UnitX, new(-h,-h,h), new(h,-h,h), new(h,h,h), new(-h,h,h));
        Face(-Vector3.UnitZ, -Vector3.UnitX, new(h,-h,-h), new(-h,-h,-h), new(-h,h,-h), new(h,h,-h));
        Face(Vector3.UnitX, -Vector3.UnitZ, new(h,-h,h), new(h,-h,-h), new(h,h,-h), new(h,h,h));
        Face(-Vector3.UnitX, Vector3.UnitZ, new(-h,-h,-h), new(-h,-h,h), new(-h,h,h), new(-h,h,-h));
        Face(Vector3.UnitY, Vector3.UnitX, new(-h,h,h), new(h,h,h), new(h,h,-h), new(-h,h,-h));
        Face(-Vector3.UnitY, Vector3.UnitX, new(-h,-h,-h), new(h,-h,-h), new(h,-h,h), new(-h,-h,h));
        return new(vertices.ToArray(), indices.ToArray());
    }
    public static GpuMesh Plane()
    {
        var v = new List<float>();
        Add(v, new(-.5f,0,.5f), Vector3.UnitY, new(0,0), Vector3.UnitX);
        Add(v, new(.5f,0,.5f), Vector3.UnitY, new(1,0), Vector3.UnitX);
        Add(v, new(.5f,0,-.5f), Vector3.UnitY, new(1,1), Vector3.UnitX);
        Add(v, new(-.5f,0,-.5f), Vector3.UnitY, new(0,1), Vector3.UnitX);
        return new(v.ToArray(), [0,1,2,0,2,3]);
    }
    public static GpuMesh Sphere()
    {
        const int rings = 16, segments = 24;
        var vertices = new List<float>(); var indices = new List<uint>();
        for (int y = 0; y <= rings; y++) for (int x = 0; x <= segments; x++)
        {
            float phi = y * MathF.PI / rings, theta = x * MathF.Tau / segments;
            var normal = new Vector3(MathF.Sin(phi) * MathF.Cos(theta), MathF.Cos(phi), MathF.Sin(phi) * MathF.Sin(theta));
            Add(vertices, normal * .5f, normal, new((float)x / segments, (float)y / rings), new(-MathF.Sin(theta), 0, MathF.Cos(theta)));
        }
        for (int y = 0; y < rings; y++) for (int x = 0; x < segments; x++)
        {
            uint a = (uint)(y * (segments + 1) + x), b = a + (uint)segments + 1;
            indices.AddRange([a, a + 1, b, a + 1, b + 1, b]);
        }
        return new(vertices.ToArray(), indices.ToArray());
    }
    private static void Add(List<float> vertices, Vector3 p, Vector3 n, Vector2 uv, Vector3 t) => vertices.AddRange([
        p.X,p.Y,p.Z,n.X,n.Y,n.Z,uv.X,uv.Y,t.X,t.Y,t.Z,1,0,0,0,0,1,0,0,0]);
}
