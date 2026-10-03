#version 430 core
in vec2 vUv;
uniform samplerCube uSky;
uniform mat4 uInverseViewProjection;
uniform vec3 uCamera;
out vec4 color;
void main() {
    vec4 p=uInverseViewProjection*vec4(vUv*2.0-1.0,1.0,1.0);
    color=vec4(texture(uSky,normalize(p.xyz/p.w-uCamera)).rgb,1.0);
}
