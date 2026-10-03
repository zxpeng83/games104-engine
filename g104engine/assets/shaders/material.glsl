uniform vec4 uBaseColor;
uniform float uMetallic;
uniform float uRoughness;
uniform float uNormalScale;
uniform float uAlphaCutoff;
uniform sampler2D uBaseTexture;
uniform sampler2D uNormalTexture;
uniform sampler2D uMrTexture;
uniform int uHasBaseTexture;
uniform int uHasNormalTexture;
uniform int uHasMrTexture;
in vec3 vWorld;
in vec3 vNormal;
in vec4 vTangent;
in vec2 vUv;
void readMaterial(out vec3 base,out vec3 normal,out float metal,out float rough) {
    vec4 color=uBaseColor;
    if(uHasBaseTexture!=0) color*=texture(uBaseTexture,vUv);
    if(uAlphaCutoff>=0.0 && color.a<uAlphaCutoff) discard;
    base=color.rgb;
    metal=uMetallic;
    rough=uRoughness;
    if(uHasMrTexture!=0) { vec4 mr=texture(uMrTexture,vUv); metal*=mr.b; rough*=mr.g; }
    metal=clamp(metal,0.0,1.0); rough=clamp(rough,0.045,1.0);
    normal=normalize(vNormal);
    if(uHasNormalTexture!=0) {
        vec3 sampleNormal=texture(uNormalTexture,vUv).xyz*2.0-1.0;
        sampleNormal.xy*=uNormalScale;
        vec3 tangent=normalize(vTangent.xyz-normal*dot(normal,vTangent.xyz));
        mat3 tbn=mat3(tangent,cross(normal,tangent)*vTangent.w,normal);
        normal=normalize(tbn*sampleNormal);
    }
    if(!gl_FrontFacing) normal=-normal;
}
