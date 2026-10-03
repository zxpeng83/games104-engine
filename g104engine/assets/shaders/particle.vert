#version 430 core
layout(location=0) in vec3 aPosition;
layout(location=1) in vec4 aColor;
layout(location=2) in vec2 aUv;
uniform mat4 uViewProjection;
out vec4 vColor;
out vec2 vUv;
void main() { vColor=aColor; vUv=aUv; gl_Position=uViewProjection*vec4(aPosition,1); }
