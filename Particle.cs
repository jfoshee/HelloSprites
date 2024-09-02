namespace HelloSprites;

public sealed class Particle(Vector3 position, Vector3 velocity, float scale)
{
    private const int LifeSpanSeconds = 5;

    public Vector3 Position { get; set; } = position;
    public Vector3 Velocity { get; set; } = velocity;
    public float Scale { get; set; } = scale;
    public TimeSpan Lifetime { get; private set; } = TimeSpan.FromSeconds(LifeSpanSeconds);
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

        // Choose the Row of the sprite-sheet based on the velocity
        // 0 = Down, 1 = Right, 2 = Up, 3 = Left (flipped vertically from image)
        int row = Math.Abs(Velocity.X) > Math.Abs(Velocity.Y)
              ? (Velocity.X > 0 ? 1 : 3)
              : (Velocity.Y > 0 ? 2 : 0);
        var rowOffset = row * 4;

        // Update the particle's sprite index based on time
        var elapsedSeconds = LifeSpanSeconds - Lifetime.TotalSeconds;
        int totalFramesElapsed = (int)(elapsedSeconds / FrameDuration.TotalSeconds);
        SpriteIndex = SpriteIndices[totalFramesElapsed % SpriteIndices.Length] + rowOffset;

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
