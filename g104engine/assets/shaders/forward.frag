#version 430 core
#include "material.glsl"
#include "lighting.glsl"
layout(location=0) out vec4 color;
void main() {
    vec3 base,normal; float metal,rough;
    readMaterial(base,normal,metal,rough);
    color=vec4(shade(base,normal,metal,rough,vWorld),1.0);
}
