using G104.Engine.Rendering;
using OpenTK.Mathematics;

namespace G104.Engine.Effects;

/// <summary>固定步推进的世界空间CPU爆发粒子；只拥有个体数据，不拥有渲染器GPU缓冲。</summary>
public sealed class ParticleSystem
{
    /// <summary>单个存活粒子的可变模拟状态。</summary>
    private sealed class Particle
    {
        /// <summary>Position为世界位置（米）；Velocity为世界速度（米/秒）。</summary>
        public Vector3 Position, Velocity;
        /// <summary>出生时的线性RGB和基础alpha。</summary>
        public Vector4 Color;
        /// <summary>Life为剩余秒数；MaximumLife为出生寿命秒数；Size为Billboard边长米数。</summary>
        public float Life, MaximumLife, Size;
    }
    /// <summary>仅保存当前存活个体，死亡时原地移除。</summary>
    private readonly List<Particle> _particles = [];
    /// <summary>固定种子的出生随机源，便于同一执行序列对照效果。</summary>
    private readonly Random _random = new(104);
    /// <summary>当前存活粒子数量。</summary>
    public int Count => _particles.Count;
    /// <summary>CPU粒子上限，突发创建达到512个后停止追加。</summary>
    public int Capacity { get; } = 512;

    /// <summary>在世界坐标一次生成随机上抛粒子；count受剩余容量限制。</summary>
    public void Burst(Vector3 position, Vector4 color, int count = 24)
    {
        for (int i = 0; i < count && _particles.Count < Capacity; i++)
        {
            float angle = _random.NextSingle() * MathF.Tau;
            float speed = 0.4f + _random.NextSingle() * 2;
            float life = 0.45f + _random.NextSingle() * 0.65f;
            _particles.Add(new Particle { Position = position,
                Velocity = new Vector3(MathF.Cos(angle) * speed, 1 + _random.NextSingle() * 2, MathF.Sin(angle) * speed),
                Color = color, Life = life, MaximumLife = life, Size = 0.07f + _random.NextSingle() * 0.08f });
        }
    }

    /// <summary>按固定步秒数推进寿命、向下加速度和位置，倒序删除死亡粒子。</summary>
    /// <param name="dt">一次模拟步长，单位秒。</param>
    public void Tick(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i]; particle.Life -= dt;
            if (particle.Life <= 0) { _particles.RemoveAt(i); continue; }
            particle.Velocity.Y -= 3.5f * dt; particle.Position += particle.Velocity * dt;
        }
    }

    /// <summary>复制本帧显示数据并按剩余寿命衰减alpha，渲染读取不推进模拟。</summary>
    public IReadOnlyList<ParticleVisual> Visuals() => _particles.Select(p =>
        new ParticleVisual(p.Position, new Vector4(p.Color.Xyz, p.Color.W * p.Life / p.MaximumLife), p.Size)).ToArray();
    /// <summary>清空全部CPU个体，供Play/Stop等生命周期边界使用。</summary>
    public void Clear() => _particles.Clear();
}
