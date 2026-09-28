Shader "Buoyancy/FluidDebug"
{
    Properties
    {
        _BaseColor ("Fluid Color", Color) = (0.05, 0.35, 0.8, 0.18)
        _EdgeColor ("Edge Color", Color) = (0.2, 0.7, 1.0, 0.35)

        _EdgePower ("Edge Power", Range(0.5, 8.0)) = 3.0
        _EdgeStrength ("Edge Strength", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            fixed4 _BaseColor;
            fixed4 _EdgeColor;
            float _EdgePower;
            float _EdgeStrength;

            v2f vert(appdata v)
            {
                v2f o;

                o.position = UnityObjectToClipPos(v.vertex);

                o.worldPosition =
                    mul(unity_ObjectToWorld, v.vertex).xyz;

                o.worldNormal =
                    UnityObjectToWorldNormal(v.normal);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal =
                    normalize(i.worldNormal);

                float3 viewDirection =
                    normalize(_WorldSpaceCameraPos - i.worldPosition);

                float fresnel =
                    pow(
                        1.0 - saturate(dot(normal, viewDirection)),
                        _EdgePower
                    );

                float edge =
                    saturate(fresnel * _EdgeStrength);

                float3 color =
                    lerp(
                        _BaseColor.rgb,
                        _EdgeColor.rgb,
                        edge
                    );

                float alpha =
                    saturate(
                        _BaseColor.a +
                        fresnel * _EdgeColor.a * _EdgeStrength
                    );

                return fixed4(color, alpha);
            }

            ENDCG
        }
    }
}