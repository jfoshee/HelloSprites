#version 100
attribute vec2 aPosition;
attribute vec2 aTexCoord;
attribute vec2 aInstanceTranslation; // Per-instance translation
attribute float aInstanceScale;   // Per-instance scale

varying vec2 vTexCoord;

void main(void)
{
    vec2 scaledPosition = aPosition * aInstanceScale;
    vec2 worldPosition = scaledPosition + aInstanceTranslation;
    gl_Position = vec4(worldPosition, 0.0, 1.0);
    vTexCoord = aTexCoord;
}
