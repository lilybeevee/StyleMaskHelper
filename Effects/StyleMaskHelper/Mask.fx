sampler maskTex : register(s0);
sampler sourceTex : register(s1);

float4 effect(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR
{
    return tex2D(sourceTex, uv) * tex2D(maskTex, uv) * color;
}

technique MaskEffect
{
    pass
    {
        PixelShader = compile ps_3_0 effect();
    }
} 