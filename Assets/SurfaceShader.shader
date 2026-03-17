Shader "Custom/EdgeOutlineShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        
        // --- Outline Properties ---
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
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
                float3 normal : NORMAL; // Required for extrusion
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                
                float4 clipPos = UnityObjectToClipPos(v.vertex);

                float4 extrudedPos = v.vertex + float4(v.normal, 0) * _OutlineWidth;
                
                o.pos = UnityObjectToClipPos(extrudedPos);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
        Pass
        {
            // Draw back faces normally
            Cull Back 

            CGPROGRAM
            // ... (Your standard vertex/fragment shader code for the main mesh surface goes here) ...
            ENDCG
        }
    }
}