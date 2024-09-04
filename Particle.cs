namespace HelloSprites;

public sealed class Particle(Vector3 position, Vector3 velocity, float scale, int[] frameIndices)
{
    private static readonly TimeSpan FrameDuration = TimeSpan.FromSeconds(1.0 / 60.0);
    private const double LifeSpanSeconds = 5;
    // private const double LifeSpanSeconds = 2 - 1/20.0;

    public Vector3 Position { get; set; } = position;
    public Vector3 Velocity { get; set; } = velocity;
    public float Scale { get; set; } = scale;
    public TimeSpan Lifetime { get; private set; } = TimeSpan.FromSeconds(LifeSpanSeconds);
    public bool Dead { get; private set; } = false;
    public int FrameIndex { get; private set; } = frameIndices[0];
    private readonly int[] _frameIndices = frameIndices;

    public void Update(TimeSpan deltaTime)
    {
        if (Dead)
            return;

        // Update position based on velocity
        Position += Velocity * (float)deltaTime.TotalSeconds;

        // Decrease the lifetime by the elapsed time
        Lifetime -= deltaTime;

        // Update the particle's sprite index based on time
        var elapsedSeconds = LifeSpanSeconds - Lifetime.TotalSeconds;
        int totalFramesElapsed = (int)(elapsedSeconds / FrameDuration.TotalSeconds);
        FrameIndex = _frameIndices[totalFramesElapsed % _frameIndices.Length];

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
