Shader "Custom/GrassWithFrustumCulling"
{
    Properties
    {
        _MainTex ("Density Map", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float isVisible : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4x4 _CameraMatrix;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_TexelSize)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                int gridSize = 64; // Adjust for density
                float worldSize = 10.0; // Scale up properly
                int x = instanceID % gridSize;
                int y = instanceID / gridSize;

                float2 uv = float2(x / (float)gridSize, y / (float)gridSize);
                float density = tex2Dlod(_MainTex, float4(uv, 0, 0)).r;

                if (density < 0.5) 
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.isVisible = 0.0;
                    return o;
                }

                float3 worldPos = float3(
                    (x / (float)gridSize) * worldSize - (worldSize / 2.0), 
                    0, 
                    (y / (float)gridSize) * worldSize - (worldSize / 2.0)
                );

                float4 clipPos = mul(_CameraMatrix, float4(worldPos, 1.0));

                if (clipPos.x < -clipPos.w || clipPos.x > clipPos.w ||
                    clipPos.y < -clipPos.w || clipPos.y > clipPos.w ||
                    clipPos.z < 0 || clipPos.z > clipPos.w)
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.isVisible = 0.0;
                    return o;
                }

                // Scale and adjust position
                v.vertex.xyz *= 0.3;
                v.vertex.xyz += worldPos;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.isVisible = 1.0;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (i.isVisible < 0.5) discard;
                return fixed4(0.3, 0.8, 0.3, 1.0); 
            }
            ENDCG
        }
    }
}
