sampler maskTex : register(s0);
sampler sourceTex : register(s1);

uniform float maxStrength;
uniform int currentStep;

float4 effect(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR
{
    float strength = tex2D(maskTex, uv).a * maxStrength;
    return tex2D(sourceTex, uv) * clamp(strength - currentStep, 0, 1);
}

technique StrengthMask
{
    pass pass0
    {
        PixelShader = compile ps_2_0 effect();
    }
} 