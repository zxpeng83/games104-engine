#version 430 core
in vec4 vColor;
in vec2 vUv;
out vec4 color;
void main() {
    float radius=length(vUv*2.0-1.0);
    if(radius>=1.0) discard;
    color=vec4(vColor.rgb,vColor.a*(1.0-radius)*(1.0-radius));
}
