sampler maskTex : register(s0);
sampler sourceTex : register(s1);

float4 effect(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR
{
    return tex2D(sourceTex, uv) * tex2D(maskTex, uv);
}

technique MaskEffect
{
    pass pass0
    {
        PixelShader = compile ps_2_0 effect();
    }
} 