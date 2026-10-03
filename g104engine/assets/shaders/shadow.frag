#version 430 core
uniform vec4 uBaseColor;
uniform sampler2D uBaseTexture;
uniform int uHasBaseTexture;
uniform float uAlphaCutoff;
in vec2 vUv;
void main() {
    float alpha=uBaseColor.a;
    if(uHasBaseTexture!=0) alpha*=texture(uBaseTexture,vUv).a;
    if(uAlphaCutoff>=0.0 && alpha<uAlphaCutoff) discard;
}
