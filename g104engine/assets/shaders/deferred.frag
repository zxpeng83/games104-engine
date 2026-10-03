#version 430 core
#include "lighting.glsl"
in vec2 vUv;
uniform sampler2D uBaseMetal;
uniform sampler2D uNormalRough;
uniform sampler2D uDepth;
uniform samplerCube uSky;
uniform mat4 uInverseViewProjection;
out vec4 color;
void main() {
    float depth=texture(uDepth,vUv).r;
    vec4 reconstructed=uInverseViewProjection*vec4(vUv*2.0-1.0,depth*2.0-1.0,1.0);
    vec3 world=reconstructed.xyz/reconstructed.w;
    if(depth>=0.999999) { color=vec4(texture(uSky,normalize(world-uCamera)).rgb,1.0); return; }
    vec4 bm=texture(uBaseMetal,vUv), nr=texture(uNormalRough,vUv);
    color=vec4(shade(bm.rgb,normalize(nr.xyz),bm.a,nr.a,world),1.0);
}
