Shader "TDOM/UI/CardGrayscale"
{
 Properties {
 _Silhouette ("Silhouette", Float) = 0
 _Backdrop ("Backdrop", Float) = 0
 [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
 _StencilComp ("Stencil Comparison", Float) = 8
 _Stencil ("Stencil ID", Float) = 0
 _StencilOp ("Stencil Operation", Float) = 0
 _StencilWriteMask ("Stencil Write Mask", Float) = 255
 _StencilReadMask ("Stencil Read Mask", Float) = 255
 _ColorMask ("Color Mask", Float) = 15
 }
 SubShader {
 Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
 Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
 Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
 Blend SrcAlpha OneMinusSrcAlpha
 ColorMask [_ColorMask]
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
 #include "UnityCG.cginc"
 #include "UnityUI.cginc"
 struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
 struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float4 local:TEXCOORD1; };
 sampler2D _MainTex;
 fixed4 _TextureSampleAdd;
 float4 _ClipRect;
 float _Silhouette;
 float _Backdrop;
 Output vert(Input v) { Output o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
 fixed4 frag(Output i):SV_Target {
 fixed4 c=tex2D(_MainTex,i.uv)+_TextureSampleAdd;
 fixed luminance=dot(c.rgb,fixed3(0.299,0.587,0.114));
 c.rgb=luminance.xxx; c*=i.color;
 if (_Silhouette > 0.5) { c.rgb=i.color.rgb; c.a*=(1-smoothstep(0.08,0.23,luminance)); }
 if (_Backdrop > 0.5) { float light=saturate(1-length((i.uv-float2(0.5,0.65))*float2(1,0.7))); c=fixed4(lerp(fixed3(0.47,0.47,0.45),fixed3(0.89,0.88,0.83),light),1); }
 #ifdef UNITY_UI_CLIP_RECT
 c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
 #endif
 return c;
 }
 ENDCG
 }
 }
}
