namespace HelloSprites;

public sealed class Particle(Vector3 position, Vector3 velocity, float scale)
{
    public Vector3 Position { get; set; } = position;
    public Vector3 Velocity { get; set; } = velocity;
    public float Scale { get; set; } = scale;
    public TimeSpan Lifetime { get; private set; } = TimeSpan.FromSeconds(5);
    public bool Dead { get; private set; } = false;
    public int SpriteIndex { get; private set; }

    private static readonly int[] SpriteIndices = [0, 1, 2, 1];
    private static readonly TimeSpan FrameDuration = TimeSpan.FromSeconds(1.0 / 6.0); // 6 fps

    public void Update(TimeSpan deltaTime)
    {
        if (Dead)
            return;

        // Update position based on velocity
        Position += Velocity * (float)deltaTime.TotalSeconds;

        // Decrease the lifetime by the elapsed time
        Lifetime -= deltaTime;

        // Update the particle's sprite index based on time
        int totalFramesElapsed = (int)((5 - Lifetime.TotalSeconds) / FrameDuration.TotalSeconds);
        SpriteIndex = SpriteIndices[totalFramesElapsed % SpriteIndices.Length];

        // Check if the particle's lifetime has expired
        if (Lifetime <= TimeSpan.Zero)
        {
            // Mark the particle as dead and reset its values
            Dead = true;
            Reset();
        }
    }

    private void Reset()
    {
        Position = default;
        Velocity = default;
        Scale = 0;
        Lifetime = TimeSpan.Zero;
    }
}
