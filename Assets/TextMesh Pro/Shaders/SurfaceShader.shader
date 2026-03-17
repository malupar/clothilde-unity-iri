Shader "Custom/EdgeOutlineShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        
        // --- Outline Properties ---
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.1)) = 0.05 
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Cull Front
            
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;   // REQUIRED for lighting calculations
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed3 diffuseColor : COLOR; // Output for the calculated light
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _OutlineColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // 3. Position Transformation (Crucial Step)
                // Converts the local vertex position (v.vertex) into clip space (o.pos).
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // 4. UV Transformation
                // Applies scaling and offset defined in the material inspector
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                return _OutlineColor;
            }
            ENDCG
        }
        Pass
        {
            // Draw back faces normally
            Cull Back 

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Need the lighting include for the lighting functions
            #include "Lighting.cginc" 
            #include "UnityCG.cginc"

            // 1. INPUT DATA STRUCTURE
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;   // REQUIRED for lighting calculations
                float2 uv : TEXCOORD0;
            };

            // 2. OUTPUT DATA STRUCTURE
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed3 diffuseColor : COLOR; // Output for the calculated light
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // 3. Position Transformation (Crucial Step)
                // Converts the local vertex position (v.vertex) into clip space (o.pos).
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // 4. UV Transformation
                // Applies scaling and offset defined in the material inspector
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                return fixed4(texColor.rgb * i.diffuseColor, 1.0);
            }
            ENDCG
        }
    }
}