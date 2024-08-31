namespace HelloSprites;

public sealed class Particle(Vector3 position, Vector3 velocity, float scale)
{
    public Vector3 Position { get; set; } = position;
    public Vector3 Velocity { get; set; } = velocity;
    public float Scale { get; set; } = scale;

    public void Update(TimeSpan deltaTime)
    {
        // Update position based on velocity
        Position += Velocity * (float)deltaTime.TotalSeconds;
    }
}
