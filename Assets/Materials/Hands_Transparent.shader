Shader "Custom/Hands_Transparent" { 
     Properties 
     {
       _InnerColor ("Inner Color", Color) = (1.0, 1.0, 1.0, 1.0)
       _InnerAlpha("Inner Alpha", Range(0.0,1.0)) = 1.0
       _RimColor ("Rim Color", Color) = (0.26,0.19,0.16,0.0)
       _RimPower ("Rim Power", Range(0.5,8.0)) = 3.0
       _MainTex("Alpha Map", 2D) = "white" {}
       _Amount("Extrusion Amount", Range(-1,1)) = 0.1
     }
     SubShader 
     {
       Tags {"Queue" = "Transparent" "Render" = "Transparent" "IgnoreProjector" = "True"}
       
       Cull Back
       //Blend One One
       Blend SrcAlpha OneMinusSrcAlpha

       // Write depth values so that you see topmost layer.
       Pass
        {
            ZWrite On
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 vert(float4 vertex : POSITION) : SV_POSITION
            {
            return UnityObjectToClipPos(vertex);
            }

            fixed4 frag() : SV_Target
            {
            return 0;
            }
            ENDCG
        }

       
       CGPROGRAM
       #pragma surface surf Lambert alpha:fade noshadow addshadow vertex:vert
       
       struct Input
       {
           float3 viewDir;
           float2 uv_MainTex;
           INTERNAL_DATA
       };
       
       sampler2D _MainTex;
       float4 _InnerColor;
       float _InnerAlpha;
       float4 _RimColor;
       float _RimPower;

       float3 SafeNormalize(float3 normal) {
           float magSq = dot(normal, normal);
           if (magSq == 0) {
               return 0;
           }
           return normalize(normal);
       }

       float _Amount;
       void vert(inout appdata_full v) {
           v.vertex.xyz += v.normal * _Amount;
       }
       
       void surf (Input IN, inout SurfaceOutput o) 
       {
           o.Albedo = _InnerColor.rgb;

           float3 normalDirection = SafeNormalize(o.Normal);
           float3 viewDir = SafeNormalize(IN.viewDir);

           float3 c = tex2D(_MainTex, IN.uv_MainTex).rgb;
           float blackness = (c.r + c.g + c.b) / 3.0;

           half rim = 1.0 - saturate(dot (viewDir, o.Normal));
           half rim_2 = 1.5 - saturate(dot(viewDir, o.Normal));

           o.Emission = _RimColor.rgb * pow (rim, _RimPower) * blackness;
           o.Alpha = blackness * _InnerAlpha * pow(rim_2, _RimPower);
       }
       ENDCG
     } 
     Fallback "VertexLit"
   }
