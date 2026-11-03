using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Engine.Rendering;

/// <summary>同一显示帧的世界空间相机与CPU行向量矩阵，供各Pass共享。</summary>
/// <param name="Position">世界空间观察点，单位米。</param>
/// <param name="Target">世界空间注视点，也用于确定阴影覆盖中心。</param>
/// <param name="View">世界到相机空间的视图矩阵。</param>
/// <param name="Projection">相机到OpenGL裁剪空间的投影，NDC深度为[-1,1]。</param>
public readonly record struct CameraState(Vector3 Position, Vector3 Target, Matrix4 View, Matrix4 Projection);
/// <summary>固定步交给动画状态机的角色运动事实。</summary>
/// <param name="Speed">水平速度大小，单位米/秒。</param>
/// <param name="Grounded">本步角色是否接地。</param>
/// <param name="VerticalVelocity">竖直速度，向上为正，单位米/秒。</param>
public readonly record struct AnimationInputs(float Speed, bool Grounded, float VerticalVelocity);
/// <summary>连接稳定对象身份、场景设计与显示姿态；本身不拥有GPU资源。</summary>
/// <param name="Id">用于复用动画实例的场景对象标识。</param>
/// <param name="ModelMatrix">对象局部空间到世界空间的CPU行向量矩阵。</param>
/// <param name="Design">绘制时读取的可见性、模型与材质设计。</param>
/// <param name="Animation">当前固定步的角色运动输入。</param>
/// <param name="InterpolationAlpha">前后逻辑姿态之间的显示插值比例。</param>
public sealed record RenderObject(Guid Id, Matrix4 ModelMatrix, SceneObjectData Design, AnimationInputs Animation, float InterpolationAlpha = 1);
/// <summary>CPU粒子提交给渲染器的世界空间Billboard快照。</summary>
/// <param name="Position">粒子中心的世界坐标，单位米。</param>
/// <param name="Color">线性RGB和已经乘入寿命淡出的alpha。</param>
/// <param name="Size">朝向相机的方形边长，单位米。</param>
public readonly record struct ParticleVisual(Vector3 Position, Vector4 Color, float Size);
/// <summary>Final为最终合成；BaseColor为基础色；Normals为世界法线；Roughness为粗糙度；Depth为深度；Shadow为阴影贴图。</summary>
public enum RenderDebugView { Final, BaseColor, Normals, Roughness, Depth, Shadow }
