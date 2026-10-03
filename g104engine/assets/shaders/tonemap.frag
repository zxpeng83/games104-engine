#version 430 core
in vec2 vUv;
uniform sampler2D uHdr;
uniform sampler2D uBaseMetal;
uniform sampler2D uNormalRough;
uniform sampler2D uDepth;
uniform sampler2D uShadow;
uniform float uExposure;
uniform int uDebug;
out vec4 color;
void main() {
    vec3 result;
    if(uDebug==1) result=pow(max(texture(uBaseMetal,vUv).rgb,vec3(0.0)),vec3(1.0/2.2));
    else if(uDebug==2) result=texture(uNormalRough,vUv).xyz*0.5+0.5;
    else if(uDebug==3) result=vec3(texture(uNormalRough,vUv).a);
    else if(uDebug==4) result=vec3(1.0-pow(texture(uDepth,vUv).r,50.0));
    else if(uDebug==5) result=vec3(texture(uShadow,vUv).r);
    else {
        vec3 hdr=max(texture(uHdr,vUv).rgb*uExposure,vec3(0.0));
        result=pow(hdr/(hdr+1.0),vec3(1.0/2.2)); // 只在此处Reinhard和Gamma一次。
    }
    color=vec4(result,1.0);
}
