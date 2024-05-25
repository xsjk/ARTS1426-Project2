Shader "Custom/SingleColorEmission"
{
    Properties
    {
        _Color ("Glow Color", Color) = (1, 1, 1, 1)
    }
 
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            struct appdata_t
            {
                float4 vertex : POSITION;
            };
 
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
 
            float4 _Color;
 
            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
 
            half4 frag (v2f i) : SV_Target
            {
                // Emit a constant glow color
                return _Color;
            }
            ENDCG
        }
    }
}