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
		_DiffuseVsRefraction("Diffuse Vs Refraction", Float ) = .2
		_RefractionAmount("Refraction Amount", Float ) = 1
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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _SpecialTex;
			float4 _SpecialTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float4 specialTex = tex2D(_SpecialTex, i.uv);
				fixed4 col = tex2D(_MainTex, i.uv );
				col.a = min(1,col.a*3);
				return col;
			}
			ENDCG
		}
	}
}