using G104.Engine.Rendering;
using OpenTK.Mathematics;

namespace G104.Engine.Effects;

public sealed class ParticleSystem
{
    private sealed class Particle
    {
        public Vector3 Position, Velocity;
        public Vector4 Color;
        public float Life, MaximumLife, Size;
    }
    private readonly List<Particle> _particles = [];
    private readonly Random _random = new(104);
    public int Count => _particles.Count;
    public int Capacity { get; } = 512;

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

    public void Tick(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i]; particle.Life -= dt;
            if (particle.Life <= 0) { _particles.RemoveAt(i); continue; }
            particle.Velocity.Y -= 3.5f * dt; particle.Position += particle.Velocity * dt;
        }
    }

    public IReadOnlyList<ParticleVisual> Visuals() => _particles.Select(p =>
        new ParticleVisual(p.Position, new Vector4(p.Color.Xyz, p.Color.W * p.Life / p.MaximumLife), p.Size)).ToArray();
    public void Clear() => _particles.Clear();
}
