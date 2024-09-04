#version 100
attribute vec2 aPosition;
attribute vec2 aTexCoord;
attribute vec2 aInstanceTranslation;    // Per-instance translation
attribute float aInstanceScale;         // Per-instance scale
attribute float aInstanceSpriteIndex;   // Per-instance sprite index

varying vec2 vTexCoord;

uniform float uSpriteSheetColumnCount;
uniform float uSpriteSheetRowCount;
uniform float uPaddingRight;        // wasted space on the right of the sprite sheet
uniform float uPaddingBottom;       // wasted space on the bottom of the sprite sheet

void main(void) {
    vec2 scaledPosition = aPosition * aInstanceScale;
    vec2 worldPosition = scaledPosition + aInstanceTranslation;
    gl_Position = vec4(worldPosition, 0.0, 1.0);

    // Adjusted sprite dimensions accounting for padding
    float spriteWidth = (1.0 - uPaddingRight) / uSpriteSheetColumnCount;
    float spriteHeight = (1.0 - uPaddingBottom) / uSpriteSheetRowCount;

    // Calculate column and row from sprite index
    float column = mod(aInstanceSpriteIndex, uSpriteSheetColumnCount);
    float row = floor(aInstanceSpriteIndex / uSpriteSheetColumnCount);

    // Calculate texture offset
    vec2 spriteOffset = vec2(column * spriteWidth, row * spriteHeight);

    // Adjust texture coordinates based on sprite index, accounting for padding
    vTexCoord = aTexCoord * vec2(spriteWidth, spriteHeight) + spriteOffset;
}
