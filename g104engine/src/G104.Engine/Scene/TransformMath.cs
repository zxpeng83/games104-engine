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
        if (scale.X <= Tolerance || scale.Y <= Tolerance || scale.Z <= Tolerance)
            throw new SceneValidationException("Transform scale must be positive and invertible.");
        var result = new TransformData
        {
            Position = Float3.From(matrix.ExtractTranslation()),
            Rotation = RotationData.From(Normalize(matrix.ExtractRotation())),
            Scale = Float3.From(scale)
        };
        if (!NearlyEqual(matrix, Compose(result)))
            throw new SceneValidationException("Transform contains shear, reflection or a non-TRS projection.");
        return result;
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
