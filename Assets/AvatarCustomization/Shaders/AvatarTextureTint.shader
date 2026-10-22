Shader "Hidden/Luma/AvatarTextureTint"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Tint ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Tint;
            float4 frag(v2f_img i) : SV_Target
            {
                float4 source = tex2D(_MainTex, i.uv);
                return float4(_Tint.rgb, source.a);
            }
            ENDHLSL
        }
    }
}
