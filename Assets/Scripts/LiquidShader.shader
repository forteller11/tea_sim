Shader "Unlit/LiquidShader"
{
	Properties
	{
		_MainTex ("Color", 2D) = "white" {}
		_SpecialTex ("Depth", 2D) = "white" {}
		//special tex
		//r,g: uv refraction, b: depth
		_ScreenGrab ("ScreenGrab", 2D) = "white" {}
		
		_TintColor("Tint Color", Color) = (.25, .5, .8, 1)
		_DiffuseVsRefraction("Diffuse Vs Refraction", Range(0.0, 1.0)) = .2
		_RefractionAmount("Refraction Amount",  Range(-0.3, 0.3) ) = 0.1
		_AlphaThreshold("Alpha Treshold", Float ) = 0.2
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
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};
			float4 _TintColor;
			float _DiffuseVsRefraction;
			float _RefractionAmount;
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _SpecialTex;
			// float4 _SpecialTex_ST;
			sampler2D _ScreenGrab;
			sampler2D _CameraDepthTexture;
			// sampler2D _CameraOpaqueTexture;
			// float4 _ScreenGrab_ST;

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
				float2 refractedUV = i.uv + (specialTex.xy * _RefractionAmount);
				
				fixed4 liquidCol = tex2D(_MainTex, i.uv);
				float alpha = liquidCol.a;
				alpha = min(1,alpha*3);

				fixed3 diffuseColor = liquidCol.xyz;

				//todo use _CameraOpaqueTexture instead.... look into https://forum.unity.com/threads/how-to-make-_cameraopaquetexture-work-with-new-2d-renderer.808260/
				//(requires custom renderer feature i think)
				fixed3 refractCol = tex2D(_ScreenGrab, refractedUV).xyz;
				fixed3 diffuseRefracted = lerp(diffuseColor, refractCol, _DiffuseVsRefraction);

				fixed4 output = fixed4(diffuseRefracted, 1);
				
				// fixed4 depthCol = tex2D(_CameraDepthTexture, i.uv);
				// output = fixed4(fixed3(depthCol.r, depthCol.r, depthCol.r), 1);

				//Linear01Depth
				// output.rgb = fixed3(_CameraDepthTexture);
				// UNITY_OUTPUT_DEPTH(i.depth);
				return output;
			}
			ENDCG
		}
	}
}