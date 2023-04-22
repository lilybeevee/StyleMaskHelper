sampler tex : register(s0);

float4 effect(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR
{
    float lightFrom = color.r;
    float lightTo = color.g;

    float alpha = tex2D(tex, uv).a;
    alpha = (alpha * (lightTo - lightFrom)) + lightFrom;

    return float4(1, 1, 1, alpha);
}

technique LightMaskAreaEffect
{
    pass pass0
    {
        PixelShader = compile ps_2_0 effect();
    }
} 