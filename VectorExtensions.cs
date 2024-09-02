namespace HelloSprites;

public static class VectorExtensions
{
    public static Vector2 XY(this Vector3 v) => new(v.X, v.Y);
}
