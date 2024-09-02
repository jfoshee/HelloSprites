using System.Runtime.InteropServices;

namespace HelloSprites;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InstanceData(Vector2 translation, float scale, int spriteIndex)
{
    public Vector2 Translation = translation;
    public float Scale = scale;
    public float SpriteIndex = spriteIndex;
}
