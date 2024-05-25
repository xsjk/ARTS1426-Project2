Shader "Custom/SimpleTextureShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // 指定渲染队列以及一些基础渲染状态
        Tags { "RenderType"="Opaque" }
        LOD 100

        // 定义用于渲染的Pass
        Pass
        {
            CGPROGRAM
            // 使用vertex和fragment程序块
            #pragma vertex vert
            #pragma fragment frag

            // 包含Unity提供的标准变换和纹理采样的头文件
            #include "UnityCG.cginc"

            // 定义接收纹理的变量
            sampler2D _MainTex;
            float4 _MainTex_ST;

            // 顶点着色器输入结构
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // 顶点着色器输出结构
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // 顶点着色器
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 片元着色器
            fixed4 frag (v2f i) : SV_Target
            {
                // 从纹理中采样颜色并返回
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
