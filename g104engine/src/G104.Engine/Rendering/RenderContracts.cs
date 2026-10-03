using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Engine.Rendering;

public readonly record struct CameraState(Vector3 Position, Vector3 Target, Matrix4 View, Matrix4 Projection);
public readonly record struct AnimationInputs(float Speed, bool Grounded, float VerticalVelocity);
public sealed record RenderObject(Guid Id, Matrix4 ModelMatrix, SceneObjectData Design, AnimationInputs Animation, float InterpolationAlpha = 1);
public readonly record struct ParticleVisual(Vector3 Position, Vector4 Color, float Size);
public enum RenderDebugView { Final, BaseColor, Normals, Roughness, Depth, Shadow }
