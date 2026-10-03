using OpenTK.Mathematics;

namespace G104.Engine.Scene;

public static class TransformMath
{
    public const float Tolerance = 0.0001f;

    // OpenTK 原生行向量：先缩放、再旋转、最后平移；上传GL时由渲染边界统一映射。
    public static Matrix4 Compose(TransformData transform) =>
        Matrix4.CreateScale(transform.Scale.ToVector()) *
        Matrix4.CreateFromQuaternion(Normalize(transform.Rotation.ToQuaternion())) *
        Matrix4.CreateTranslation(transform.Position.ToVector());

    public static Quaternion Normalize(Quaternion value)
    {
        var squared = (double)value.X * value.X + (double)value.Y * value.Y + (double)value.Z * value.Z + (double)value.W * value.W;
        if (!Finite(value.X) || !Finite(value.Y) || !Finite(value.Z) || !Finite(value.W) || squared < 1e-12)
            throw new SceneValidationException("Rotation must be finite and nonzero.");
        var inverseLength = 1 / Math.Sqrt(squared);
        return new Quaternion((float)(value.X * inverseLength), (float)(value.Y * inverseLength), (float)(value.Z * inverseLength), (float)(value.W * inverseLength));
    }

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
        if (!NearlyEqual(matrix, Compose(result)))
            throw new SceneValidationException("Transform contains shear, reflection or a non-TRS projection.");
        return result;
    }

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

    public static Matrix4 Inverse(Matrix4 matrix)
    {
        if (!Finite(matrix) || !Finite(matrix.Determinant) || MathF.Abs(matrix.Determinant) < 1e-10f)
            throw new SceneValidationException("Parent transform is not invertible.");
        var inverse = Matrix4.Invert(matrix);
        if (!Finite(inverse)) throw new SceneValidationException("Parent inverse is not finite.");
        return inverse;
    }

    public static bool NearlyEqual(Matrix4 a, Matrix4 b, float tolerance = Tolerance)
    {
        for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
            if (MathF.Abs(a[row, column] - b[row, column]) > tolerance * MathF.Max(1, MathF.Max(MathF.Abs(a[row, column]), MathF.Abs(b[row, column]))))
                return false;
        return true;
    }

    public static bool Finite(float value) => float.IsFinite(value);
    public static bool Finite(Vector3 value) => Finite(value.X) && Finite(value.Y) && Finite(value.Z);
    public static bool Finite(Matrix4 value)
    {
        for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
            if (!Finite(value[row, column])) return false;
        return true;
    }

    public static TransformData Clone(TransformData value) => new() { Position = value.Position, Rotation = value.Rotation, Scale = value.Scale };

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

public sealed class SceneValidationException(string message) : InvalidOperationException(message);
