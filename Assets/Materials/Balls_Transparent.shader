Shader "Custom/Balls_Transparent" { 
     Properties 
     {
       _Color ("Inner Color", Color) = (1.0, 1.0, 1.0, 1.0)
       _Alpha("Inner Alpha", Range(0.0,1.0)) = 1.0
       _RimColor ("Rim Color", Color) = (0.26,0.19,0.16,0.0)
       _RimPower ("Rim Power", Range(0.5,8.0)) = 3.0
     }
     SubShader 
     {
       Tags {"Queue" = "Transparent" "Render" = "Transparent" "IgnoreProjector" = "True"}
       
       Cull Back
       //Blend One One
       Blend SrcAlpha OneMinusSrcAlpha
       
       CGPROGRAM
       #pragma surface surf Lambert alpha:fade noshadow
       
       struct Input
       {
           float3 viewDir;
           INTERNAL_DATA
       };
       
       float4 _Color;
       float _Alpha;
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
           o.Albedo = _Color.rgb;

           float3 normalDirection = SafeNormalize(o.Normal);
           float3 viewDir = SafeNormalize(IN.viewDir);

           half rim = 1.0 - saturate(dot (viewDir, o.Normal));
           half rim_2 = 1.5 - saturate(dot(viewDir, o.Normal));

           o.Emission = _RimColor.rgb * pow (rim, _RimPower);
           o.Alpha = _Alpha * pow(rim_2, _RimPower);
       }
       ENDCG
     } 
     Fallback "VertexLit"
   }
