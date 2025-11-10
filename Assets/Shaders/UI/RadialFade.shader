Shader "UI/RadialFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RadialProgress ("Radial Progress", Range(0.0, 1.0)) = 0.0
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Smoothness ("Edge Smoothness", Range(0.001, 0.1)) = 0.02
        _Feather ("Edge Feather", Range(0.0, 0.5)) = 0.1
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        
        _ColorMask ("Color Mask", Float) = 15
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _RadialProgress;
            float2 _Center;
            float _Smoothness;
            float _Feather;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                // Calculate distance from center for radial effect with aspect ratio correction
                float2 uv = IN.texcoord;
                float2 center = _Center.xy;
                
                // Get screen aspect ratio to maintain circular shape
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                
                // Adjust UV coordinates to maintain circular shape regardless of aspect ratio
                float2 adjustedUV = uv;
                if (aspectRatio < 1.0) // Portrait mode
                {
                    adjustedUV.x = (uv.x - 0.5) * aspectRatio + 0.5;
                }
                else // Landscape mode
                {
                    adjustedUV.y = (uv.y - 0.5) / aspectRatio + 0.5;
                }
                
                // Calculate distance from center in aspect-corrected space
                float distance = length(adjustedUV - center);
                
                // Calculate maximum distance to screen corners in aspect-corrected space
                float2 corner = float2(0.5, 0.5);
                if (aspectRatio < 1.0) // Portrait
                {
                    corner.x *= aspectRatio;
                }
                else // Landscape
                {
                    corner.y /= aspectRatio;
                }
                float maxDistance = length(corner) * 1.1; // Add 10% buffer to ensure full coverage
                
                float normalizedDistance = distance / maxDistance;
                
                // Create hard cutoff instead of alpha blending
                // When _RadialProgress = 0, we want full coverage (show everything)
                // When _RadialProgress = 1, we want no coverage (hide everything)
                float threshold = 1.0 - _RadialProgress;
                
                // Use step function for hard cutoff, with tiny smoothstep for anti-aliasing
                float radialMask = smoothstep(threshold - 0.002, threshold + 0.002, normalizedDistance);
                
                // Complete cutoff - either fully visible or fully transparent
                if (radialMask < 0.5)
                {
                    color.a = 0.0;
                }
                // If visible, keep original alpha
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif
                
                return color;
            }
        ENDCG
        }
    }
} 