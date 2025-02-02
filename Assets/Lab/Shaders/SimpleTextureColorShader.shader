Shader "Lit/DiffuseWithShadows" {
    Properties {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
        _Lightmap ("Lightmap", 2D) = "white" {} // Add a lightmap property for baked lighting
    }
    SubShader {
        Pass {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            // Compile shader into multiple variants, with and without shadows
            #pragma multi_compile_fwdbase
            // Include shadow helper functions and macros
            #include "AutoLight.cginc"
            
            struct v2f {
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1) // Put shadows data into TEXCOORD1
                fixed3 diff : COLOR0;
                fixed3 ambient : COLOR1;
                float4 pos : SV_POSITION;
            };
            
            v2f vert (appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0.rgb;
                o.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(o); // Compute shadows data
                return o;
            }
            
            sampler2D _MainTex;
            sampler2D _Lightmap; // Declare a lightmap sampler
            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed shadow = SHADOW_ATTENUATION(i); // Compute shadow attenuation
                fixed3 lighting = i.diff * shadow + i.ambient; // Darken light's illumination with shadow, keep ambient intact
                
                // Use baked lightmap for the lighting, replacing real-time light computations
                fixed4 bakedLight = tex2D(_Lightmap, i.uv);
                lighting *= bakedLight.rgb; // Combine baked lighting with real-time lighting
                
                col.rgb *= lighting;
                return col;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
