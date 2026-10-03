#version 430 core
#include "material.glsl"
layout(location=0) out vec4 baseMetal;
layout(location=1) out vec4 normalRough;
void main() {
    vec3 base,normal; float metal,rough;
    readMaterial(base,normal,metal,rough);
    baseMetal=vec4(base,metal); // RGBA8是线性颜色，不使用sRGB附件。
    normalRough=vec4(normal,rough); // 世界空间法线为有符号RGBA16F。
}
