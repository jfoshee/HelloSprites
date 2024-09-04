namespace HelloSprites;

public sealed class Particle
{
    private const double LifeSpanSeconds = 5;
    // private const double LifeSpanSeconds = 2 - 1/20.0;

    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Scale { get; set; }
    public TimeSpan Lifetime { get; private set; } = TimeSpan.FromSeconds(LifeSpanSeconds);
    public bool Dead { get; private set; } = false;
    public int SpriteIndex { get; private set; }
    private int _spriteRow;

    const int RowCount = 11;
    const int ColumnCount = 20;

    private static readonly int[] SpriteIndices = Enumerable.Range(0, ColumnCount - 1).ToArray();
    private static readonly TimeSpan FrameDuration = TimeSpan.FromSeconds(1.0 / 24.0);

    public Particle(Vector3 position, Vector3 velocity, float scale)
    {
        Position = position;
        Velocity = velocity;
        Scale = scale;
        // Pick random row when constructed
        _spriteRow = new Random().Next(0, RowCount - 1);
    }

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
        var rowOffset = _spriteRow * ColumnCount;
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
