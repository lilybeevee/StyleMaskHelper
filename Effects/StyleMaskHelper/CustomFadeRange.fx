sampler tex : register(s0);

float4 effect(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR
{
    float alphaFrom = color.r;
    float alphaTo = color.g;

    float alpha = tex2D(tex, uv).a;
    alpha = (alpha * (alphaTo - alphaFrom)) + alphaFrom;

    return float4(alpha, alpha, alpha, alpha);
}

technique CustomFadeRange
{
    pass pass0
    {
        PixelShader = compile ps_2_0 effect();
    }
} 