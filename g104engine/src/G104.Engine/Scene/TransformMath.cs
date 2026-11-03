using OpenTK.Mathematics;

namespace G104.Engine.Scene;

/// <summary>统一OpenTK行向量TRS、校验与插值，不在此处执行GL矩阵上传。</summary>
public static class TransformMath
{
    /// <summary>TRS重建与统一缩放判断使用的基础相对容差。</summary>
    public const float Tolerance = 0.0001f;

    // OpenTK 原生行向量：先缩放、再旋转、最后平移；上传GL时由渲染边界统一映射。
    /// <summary>按缩放、旋转、平移顺序生成行向量局部矩阵。</summary>
    public static Matrix4 Compose(TransformData transform) =>
        Matrix4.CreateScale(transform.Scale.ToVector()) *
        Matrix4.CreateFromQuaternion(Normalize(transform.Rotation.ToQuaternion())) *
        Matrix4.CreateTranslation(transform.Position.ToVector());

    /// <summary>以double长度归一化四元数，拒绝非有限值和接近零的旋转。</summary>
    public static Quaternion Normalize(Quaternion value)
    {
        var squared = (double)value.X * value.X + (double)value.Y * value.Y + (double)value.Z * value.Z + (double)value.W * value.W;
        if (!Finite(value.X) || !Finite(value.Y) || !Finite(value.Z) || !Finite(value.W) || squared < 1e-12)
            throw new SceneValidationException("Rotation must be finite and nonzero.");
        var inverseLength = 1 / Math.Sqrt(squared);
        return new Quaternion((float)(value.X * inverseLength), (float)(value.Y * inverseLength), (float)(value.Z * inverseLength), (float)(value.W * inverseLength));
    }

    /// <summary>从可逆正缩放矩阵提取TRS，再用重建比较拒绝剪切、反射和投影。</summary>
    public static TransformData Decompose(Matrix4 matrix)
    {
        if (!Finite(matrix)) throw new SceneValidationException("Transform matrix must be finite.");
        var scale = matrix.ExtractScale();
        if (!Finite(scale) || scale.X <= Tolerance || scale.Y <= Tolerance || scale.Z <= Tolerance)
            throw new SceneValidationException("Transform scale must be positive and invertible.");
        var result = new TransformData
        {
            Position = Float3.From(matrix.ExtractTranslation()),
            Rotation = RotationData.From(ExtractRotation(matrix, scale)),
            Scale = Float3.From(scale)
        };
        // 分解结果能重建原矩阵才可接受，防止把不可表达的变换悄悄近似为TRS。
        if (!NearlyEqual(matrix, Compose(result)))
            throw new SceneValidationException("Transform contains shear, reflection or a non-TRS projection.");
        return result;
    }

    /// <summary>移除各行缩放后稳定反求四元数，覆盖接近180度的旋转。</summary>
    private static Quaternion ExtractRotation(Matrix4 matrix, Vector3 scale)
    {
        // 先移除行缩放；trace接近-1（180度）时用最大对角分支，避免除以接近零的w。
        double m00 = matrix.M11 / (double)scale.X, m01 = matrix.M12 / (double)scale.X, m02 = matrix.M13 / (double)scale.X;
        double m10 = matrix.M21 / (double)scale.Y, m11 = matrix.M22 / (double)scale.Y, m12 = matrix.M23 / (double)scale.Y;
        double m20 = matrix.M31 / (double)scale.Z, m21 = matrix.M32 / (double)scale.Z, m22 = matrix.M33 / (double)scale.Z;
        double trace = m00 + m11 + m22;
        double x, y, z, w;
        if (trace > 0)
        {
            double s = 2 * Math.Sqrt(1 + trace);
            w = s / 4; x = (m12 - m21) / s; y = (m20 - m02) / s; z = (m01 - m10) / s;
        }
        else if (m00 >= m11 && m00 >= m22)
        {
            double s = 2 * Math.Sqrt(1 + m00 - m11 - m22);
            x = s / 4; y = (m01 + m10) / s; z = (m02 + m20) / s; w = (m12 - m21) / s;
        }
        else if (m11 >= m22)
        {
            double s = 2 * Math.Sqrt(1 + m11 - m00 - m22);
            x = (m01 + m10) / s; y = s / 4; z = (m12 + m21) / s; w = (m20 - m02) / s;
        }
        else
        {
            double s = 2 * Math.Sqrt(1 + m22 - m00 - m11);
            x = (m02 + m20) / s; y = (m12 + m21) / s; z = s / 4; w = (m01 - m10) / s;
        }
        return Normalize(new Quaternion((float)x, (float)y, (float)z, (float)w));
    }

    /// <summary>只返回有限且可逆的父矩阵逆，防止世界转局部产生非法数据。</summary>
    public static Matrix4 Inverse(Matrix4 matrix)
    {
        if (!Finite(matrix) || !Finite(matrix.Determinant) || MathF.Abs(matrix.Determinant) < 1e-10f)
            throw new SceneValidationException("Parent transform is not invertible.");
        var inverse = Matrix4.Invert(matrix);
        if (!Finite(inverse)) throw new SceneValidationException("Parent inverse is not finite.");
        return inverse;
    }

    /// <summary>逐元素按幅值缩放容差比较矩阵，兼顾近零项与较大平移。</summary>
    public static bool NearlyEqual(Matrix4 a, Matrix4 b, float tolerance = Tolerance)
    {
        for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
            if (MathF.Abs(a[row, column] - b[row, column]) > tolerance * MathF.Max(1, MathF.Max(MathF.Abs(a[row, column]), MathF.Abs(b[row, column]))))
                return false;
        return true;
    }

    /// <summary>判断标量是否排除NaN与正负无穷。</summary>
    public static bool Finite(float value) => float.IsFinite(value);
    /// <summary>要求向量三个分量都为有限值。</summary>
    public static bool Finite(Vector3 value) => Finite(value.X) && Finite(value.Y) && Finite(value.Z);
    /// <summary>要求矩阵十六个元素都为有限值。</summary>
    public static bool Finite(Matrix4 value)
    {
        for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
            if (!Finite(value[row, column])) return false;
        return true;
    }

    /// <summary>复制三个值类型字段，得到独立可变TRS对象。</summary>
    public static TransformData Clone(TransformData value) => new() { Position = value.Position, Rotation = value.Rotation, Scale = value.Scale };

    /// <summary>限制alpha后线性插值位移/缩放、球面插值旋转，返回独立局部TRS。</summary>
    public static TransformData Interpolate(TransformData before, TransformData after, float alpha)
    {
        alpha = Math.Clamp(alpha, 0, 1);
        var rotation = Quaternion.Slerp(Normalize(before.Rotation.ToQuaternion()), Normalize(after.Rotation.ToQuaternion()), alpha);
        return new TransformData
        {
            Position = Float3.From(Vector3.Lerp(before.Position.ToVector(), after.Position.ToVector(), alpha)),
            Rotation = RotationData.From(Normalize(rotation)),
            Scale = Float3.From(Vector3.Lerp(before.Scale.ToVector(), after.Scale.ToVector(), alpha))
        };
    }
}

/// <summary>设计数据不满足场景合同时抛出的统一异常。</summary>
/// <param name="message">指出非法字段或关联约束的诊断原因。</param>
public sealed class SceneValidationException(string message) : InvalidOperationException(message);
