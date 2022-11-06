Shader "Unlit/LiquidShader"
{
	Properties
	{
		_MainTex ("Color", 2D) = "white" {}
		_SpecialTex ("Depth", 2D) = "white" {}
		//special tex
		//r,g: uv refraction, b: depth?
		_ScreenGrab ("ScreenGrab", 2D) = "white" {}
		_ScreenGrabDepth ("ScreenGrabDepth", 2D) = "white" {}
		
		_TintColor("Tint Color", Color) = (.25, .5, .8, 1)
		_DiffuseVsRefraction("Diffuse Vs Refraction",  Range(0,1) ) = .5
		_RefractionAmount("Refraction Amount", Range(-0.3,0.3)) = .03
		_AlphaMult("_AlphaMult", Float) = 3
	}
	SubShader
	{
		Tags { "RenderType"="Transparent"
			 "Queue"="Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			BlendOp Add // (is default anyway)
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};
			float4 _TintColor;
			float _DiffuseVsRefraction;
			float _RefractionAmount;
			float _AlphaMult;
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _SpecialTex;
			sampler2D _ScreenGrab;
			sampler2D _ScreenGrabDepth;
			

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 specialTex = tex2D(_SpecialTex, i.uv);
				fixed4 screenDepth = tex2D(_ScreenGrabDepth, i.uv);
				// return fixed4(screenDepth.xyz, 1);
				if (specialTex.z > screenDepth.r)
				{
					return fixed4(0,0,0,0);
				}
				
				float2 refractedUV = i.uv + (specialTex.xy * _RefractionAmount);
				
				fixed4 liquidCol = tex2D(_MainTex, i.uv);
				float alpha = liquidCol.a;
				alpha = min(1, alpha*_AlphaMult);

				fixed3 diffuseColor = liquidCol.xyz;
				fixed3 refractCol = tex2D(_ScreenGrab, refractedUV).xyz;
				fixed3 refractTintCol = _TintColor.xyz * refractCol;
				fixed3 diffuseRefracted = lerp(diffuseColor, refractTintCol, _DiffuseVsRefraction);
				fixed4 output = fixed4(diffuseRefracted, alpha);
				return output;
			}
			ENDCG
		}
	}
}