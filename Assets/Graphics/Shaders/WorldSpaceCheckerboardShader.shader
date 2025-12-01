Shader "Custom/WorldSpaceTransparentCheckerboard"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _CheckerboardSize ("Checkerboard Size", Range(0.1, 10)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            fixed4 _Color;
            float _CheckerboardSize;
            float _DrawInEditor; // New variable in the shader

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
               
                float3 checkerPos = floor(i.worldPos * (1.0 / _CheckerboardSize));
                
                float isEven = fmod(checkerPos.x + checkerPos.y + checkerPos.z, 2.0);
                
                fixed4 col;
                if (isEven == 0.0) {
                    col = fixed4(0,0,0,1);
                } else {
                    col = fixed4(1,1,1,1);
                }
                
                return col * _Color;
            }
            ENDCG
        }
    }
}