// Basic unlit shader for MonoGame
// For pixel-art style rendering

float4x4 WorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct PixelShaderInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

PixelShaderInput VertexShaderMain(VertexShaderInput input)
{
    PixelShaderInput output;
    
    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    
    return output;
}

float4 PixelShaderMain(PixelShaderInput input) : SV_TARGET
{
    return input.Color;
}

technique Unlit
{
    pass Pass0
    {
        VertexShader = compile vs_4_0_level_9_1 VertexShaderMain();
        PixelShader = compile ps_4_0_level_9_1 PixelShaderMain();
    }
}

