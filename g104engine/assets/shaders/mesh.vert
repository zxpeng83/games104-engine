#version 430 core
layout(location=0) in vec3 aPosition;
layout(location=1) in vec3 aNormal;
layout(location=2) in vec2 aUv;
layout(location=3) in vec4 aTangent;
layout(location=4) in vec4 aJoints;
layout(location=5) in vec4 aWeights;
layout(std140,binding=0) uniform BonePalette { mat4 uBones[128]; };
uniform mat4 uModel;
uniform mat4 uViewProjection;
uniform int uSkinned;
out vec3 vWorld;
out vec3 vNormal;
out vec4 vTangent;
out vec2 vUv;
void main() {
    mat4 skin=mat4(1.0);
    if(uSkinned!=0) {
        ivec4 joint=ivec4(aJoints);
        skin=uBones[joint.x]*aWeights.x+uBones[joint.y]*aWeights.y+uBones[joint.z]*aWeights.z+uBones[joint.w]*aWeights.w;
    }
    mat4 transform=uModel*skin;
    vec4 world=transform*vec4(aPosition,1.0);
    mat3 normalMatrix=transpose(inverse(mat3(transform)));
    vWorld=world.xyz;
    vNormal=normalize(normalMatrix*aNormal);
    vec3 tangent=mat3(transform)*aTangent.xyz;
    tangent=normalize(tangent-vNormal*dot(vNormal,tangent));
    vTangent=vec4(tangent,aTangent.w*(determinant(mat3(transform))<0.0?-1.0:1.0));
    vUv=aUv;
    gl_Position=uViewProjection*world;
}
