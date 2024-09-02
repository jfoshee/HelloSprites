#version 100
attribute vec2 aPosition;
attribute vec2 aTexCoord;
attribute vec2 aInstanceTranslation; // Per-instance translation
attribute float aInstanceScale;      // Per-instance scale
attribute float aInstanceSpriteIndex; // Per-instance sprite index

varying vec2 vTexCoord;

void main(void)
{
    vec2 scaledPosition = aPosition * aInstanceScale;
    vec2 worldPosition = scaledPosition + aInstanceTranslation;
    gl_Position = vec4(worldPosition, 0.0, 1.0);

    // Sprite sheet properties
    float spritesPerRow = 4.0;
    float spriteSize = 1.0 / spritesPerRow;

    // Calculate row and column from sprite index
    float column = mod(aInstanceSpriteIndex, spritesPerRow);
    float row = floor(aInstanceSpriteIndex / spritesPerRow);

    // Calculate texture offset
    vec2 spriteOffset = vec2(column * spriteSize, row * spriteSize);

    // Adjust texture coordinates based on sprite index
    vTexCoord = aTexCoord * spriteSize + spriteOffset;
}
