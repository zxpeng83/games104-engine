const float PI=3.14159265359;
uniform vec3 uCamera;
uniform vec3 uSunDirection;
uniform vec3 uSunRadiance;
uniform mat4 uLightViewProjection;
uniform sampler2D uShadow;
uniform int uShadows;
uniform int uPointCount;
uniform vec4 uPointPositions[4];
uniform vec4 uPointColors[4];
float shadowVisibility(vec3 world,vec3 normal,vec3 light) {
    if(uShadows==0) return 1.0;
    vec4 clip=uLightViewProjection*vec4(world,1.0);
    vec3 p=clip.xyz/clip.w*0.5+0.5;
    if(p.z<=0.0 || p.z>=1.0 || any(lessThan(p.xy,vec2(0.0))) || any(greaterThan(p.xy,vec2(1.0)))) return 1.0;
    float bias=max(0.0007*(1.0-dot(normal,light)),0.00018);
    vec2 texel=1.0/vec2(textureSize(uShadow,0));
    float visible=0.0;
    for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++)
        visible += p.z-bias <= texture(uShadow,p.xy+vec2(x,y)*texel).r ? 1.0 : 0.0;
    return visible/9.0;
}
vec3 brdf(vec3 base,vec3 n,vec3 v,vec3 l,float metal,float rough) {
    rough=clamp(rough,0.045,1.0); // G-buffer的半精度舍入不能使实际BRDF低于声明的粗糙度下限。
    vec3 sum=v+l;
    float sumScale=max(abs(sum.x),max(abs(sum.y),abs(sum.z)));
    if(sumScale==0.0) return vec3(0.0); // 完全相反方向没有可定义的half，且不产生正面的共同反射。
    // 先缩放再单位化：很小但非零的V+L不被epsilon缩短，也不因length平方下溢而破坏cross恒等式。
    vec3 scaledSum=sum/sumScale;
    vec3 h=scaledSum/length(scaledSum);
    float nv=max(dot(n,v),0.0001), nl=max(dot(n,l),0.0), nh=clamp(dot(n,h),0.0,1.0), vh=clamp(dot(v,h),0.0,1.0);
    float a=rough*rough, a2=a*a;
    // 单位n/h有|n×h|²=1-(n·h)²；避免高光附近的相消，不给分母叠加改变GGX能量的偏移。
    vec3 nxh=cross(n,h);
    float denominator=nh>0.0 ? dot(nxh,nxh)+nh*nh*a2 : 1.0;
    float ratio=a/max(denominator,1e-20); // rough>=.045时正常分母远大于此仅防零的下限。
    float distribution=ratio*ratio/PI;
    float k=(rough+1.0)*(rough+1.0)/8.0;
    float geometry=(nv/(nv*(1.0-k)+k))*(nl/(nl*(1.0-k)+k));
    vec3 f0=mix(vec3(0.04),base,metal);
    vec3 fresnel=f0+(1.0-f0)*pow(1.0-vh,5.0);
    vec3 specular=distribution*geometry*fresnel/(4.0*nv*max(nl,0.0001));
    vec3 diffuse=(1.0-fresnel)*(1.0-metal)*base/PI;
    return (diffuse+specular)*nl;
}
vec3 shade(vec3 base,vec3 normal,float metal,float rough,vec3 world) {
    vec3 cameraDelta=uCamera-world;
    vec3 view=cameraDelta/max(length(cameraDelta),0.000001), sun=normalize(-uSunDirection);
    vec3 color=base*0.035*(1.0-metal); // V1固定弱环境项，不冒充IBL。
    color+=brdf(base,normal,view,sun,metal,rough)*uSunRadiance*shadowVisibility(world,normal,sun);
    for(int i=0;i<uPointCount;i++) {
        vec3 delta=uPointPositions[i].xyz-world;
        float distance=max(length(delta),0.05), radius=uPointPositions[i].w;
        float edge=clamp(1.0-pow(distance/max(radius,0.01),4.0),0.0,1.0);
        float attenuation=edge*edge/(distance*distance+1.0);
        color+=brdf(base,normal,view,delta/distance,metal,rough)*uPointColors[i].rgb*uPointColors[i].a*attenuation;
    }
    return color;
}
