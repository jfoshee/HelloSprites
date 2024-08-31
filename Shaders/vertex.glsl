#version 100
attribute vec2 aPosition;
attribute vec2 aTexCoord;

uniform mat4 uModelMatrix;

varying vec2 vTexCoord;

void main(void)
{
    gl_Position = uModelMatrix * vec4(aPosition, 0.0, 1.0);
    vTexCoord = aTexCoord;
}
