/// <class>GaussianBlurEffect</class>

#define MAX_BLUR_RADIUS 15

//-----------------------------------------------------------------------------------------
// Shader constant register mappings
//-----------------------------------------------------------------------------------------
/// <defaultValue>5.0</defaultValue>
float BlurRadius : register(C0);

/// <defaultValue>1.0,1.0</defaultValue>
float2 TexelSize : register(C1);

/// <defaultValue>0.0</defaultValue>
float Sigma : register(C2);

//--------------------------------------------------------------------------------------
// Sampler Inputs
//--------------------------------------------------------------------------------------
sampler2D Texture1Sampler : register(S0);

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 main(float2 uv : TEXCOORD) : COLOR
{
    int blurRadiusInt = (int)clamp(BlurRadius, 0, MAX_BLUR_RADIUS);

    if(blurRadiusInt <= 0)
    {
        return tex2D(Texture1Sampler, uv);
    }

    float sigma = Sigma <= 0.0 ? blurRadiusInt / 3.0 : Sigma;
    float twoSigmaSqu = 2.0 * sigma * sigma;
    float weightSum = 0.0;
    float4 finalColor = float4(0, 0, 0, 0);

    [unroll(10)]
    for(int x = -MAX_BLUR_RADIUS; x <= MAX_BLUR_RADIUS; x++)
    {
        [unroll(10)]
        for(int y = -MAX_BLUR_RADIUS; y <= MAX_BLUR_RADIUS; y++)
        {
            if(abs(x) > blurRadiusInt || abs(y) > blurRadiusInt) continue;
            
            float2 sampleUV = uv + float2(x, y) * TexelSize;
            float4 sampleColor = tex2Dlod(Texture1Sampler, float4(sampleUV, 0, 0));            
            float weight = twoSigmaSqu;
            weightSum = weightSum + weight;
            finalColor = finalColor + sampleColor * weight;
        }
    }
    finalColor /= weightSum;
    return finalColor;
}