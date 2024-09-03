#version 100
attribute vec2 aPosition;
attribute vec2 aTexCoord;
attribute vec2 aInstanceTranslation; // Per-instance translation
attribute float aInstanceScale;      // Per-instance scale
attribute float aInstanceSpriteIndex; // Per-instance sprite index

varying vec2 vTexCoord;

void main(void) {
    vec2 scaledPosition = aPosition * aInstanceScale;
    vec2 worldPosition = scaledPosition + aInstanceTranslation;
    gl_Position = vec4(worldPosition, 0.0, 1.0);

    // Sprite sheet properties
    float columnCount = 20.0; // Number of columns in the sprite sheet
    float rowCount = 11.0;    // Number of rows in the sprite sheet
    float spriteWidth = 1.0 / columnCount;  // Width of a single sprite in texture coordinates
    float spriteHeight = 1.0 / rowCount;    // Height of a single sprite in texture coordinates

    // Calculate column and row from sprite index
    float column = mod(aInstanceSpriteIndex, columnCount);
    float row = floor(aInstanceSpriteIndex / columnCount);

    // Calculate texture offset
    vec2 spriteOffset = vec2(column * spriteWidth, row * spriteHeight);

    // Adjust texture coordinates based on sprite index
    vTexCoord = aTexCoord * vec2(spriteWidth, spriteHeight) + spriteOffset;
}
