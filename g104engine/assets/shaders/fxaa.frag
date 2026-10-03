#version 430 core
in vec2 vUv;
uniform sampler2D uLdr;
uniform vec2 uInvResolution;
uniform int uFxaa;
out vec4 color;
void main() {
    vec3 center=texture(uLdr,vUv).rgb;
    if(uFxaa==0) { color=vec4(center,1.0); return; }
    vec3 nw=texture(uLdr,vUv+vec2(-1,-1)*uInvResolution).rgb;
    vec3 ne=texture(uLdr,vUv+vec2(1,-1)*uInvResolution).rgb;
    vec3 sw=texture(uLdr,vUv+vec2(-1,1)*uInvResolution).rgb;
    vec3 se=texture(uLdr,vUv+vec2(1,1)*uInvResolution).rgb;
    vec3 luma=vec3(0.299,0.587,0.114);
    float lnw=dot(nw,luma),lne=dot(ne,luma),lsw=dot(sw,luma),lse=dot(se,luma),lc=dot(center,luma);
    float low=min(lc,min(min(lnw,lne),min(lsw,lse))), high=max(lc,max(max(lnw,lne),max(lsw,lse)));
    vec2 direction=vec2(-((lnw+lne)-(lsw+lse)),(lnw+lsw)-(lne+lse));
    float reduce=max((lnw+lne+lsw+lse)*0.03125,0.0078125);
    direction=clamp(direction/(min(abs(direction.x),abs(direction.y))+reduce),vec2(-8),vec2(8))*uInvResolution;
    vec3 a=0.5*(texture(uLdr,vUv+direction*(1.0/3.0-0.5)).rgb+texture(uLdr,vUv+direction*(2.0/3.0-0.5)).rgb);
    vec3 b=a*0.5+0.25*(texture(uLdr,vUv-direction*0.5).rgb+texture(uLdr,vUv+direction*0.5).rgb);
    float lb=dot(b,luma);
    color=vec4(lb<low || lb>high ? a : b,1.0);
}
