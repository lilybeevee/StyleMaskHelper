sampler tex : register(s0);

uniform float4 colorFrom;
uniform float4 colorTo;

float4 effect(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR
{
    float alphaFrom = color.r;
    float alphaTo = color.g;
    float alpha = lerp(alphaFrom, alphaTo, tex2D(tex, uv).a);
    return lerp(colorFrom, colorTo, alpha);
}

technique CustomFadeRange
{
    pass
    {
        PixelShader = compile ps_3_0 effect();
    }
} 