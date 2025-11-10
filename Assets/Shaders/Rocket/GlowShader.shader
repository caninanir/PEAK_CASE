Shader "UI/FireGlowShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2.0
        _GlowColor ("Glow Color", Color) = (1, 0.5, 0, 1)
        _InnerGlow ("Inner Glow", Range(0, 1)) = 0.5
        _OuterGlow ("Outer Glow", Range(0, 2)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _InnerGlow;
            float _OuterGlow;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                fixed4 baseColor = i.color * texColor;
                
                // Early exit if completely transparent
                if (baseColor.a < 0.01) discard;
                
                // Calculate distance from center for glow effect
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.texcoord, center);
                
                // Create glow falloff based on texture alpha
                float innerGlow = (1.0 - smoothstep(0.0, _InnerGlow, dist)) * texColor.a;
                float outerGlow = (1.0 - smoothstep(_InnerGlow, _OuterGlow, dist)) * texColor.a;
                
                // Combine glow effects
                float glowFactor = (innerGlow * 0.8) + (outerGlow * 0.4);
                glowFactor *= _GlowIntensity;
                
                // Apply glow color
                fixed4 glow = _GlowColor * glowFactor;
                
                // Combine base color with glow
                fixed4 finalColor = baseColor + glow;
                
                // Preserve original alpha for UI clipping
                finalColor.a = baseColor.a;
                
                // Apply UI clipping
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                
                return finalColor;
            }
            ENDCG
        }
        
        // Second pass for additional glow (additive blend)
        Pass
        {
            Blend One One
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragGlow

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _OuterGlow;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 fragGlow(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                
                // Don't render glow where sprite is fully opaque
                if (texColor.a > 0.95) return fixed4(0,0,0,0);
                
                // Sample nearby pixels to detect sprite edges
                float2 texelSize = float2(1.0 / 512.0, 1.0 / 512.0);
                
                // Sample in multiple directions for better edge detection
                float maxAlpha = 0.0;
                for (int x = -2; x <= 2; x++) {
                    for (int y = -2; y <= 2; y++) {
                        float2 offset = float2(x, y) * texelSize * _OuterGlow;
                        float sampleAlpha = tex2D(_MainTex, i.texcoord + offset).a;
                        maxAlpha = max(maxAlpha, sampleAlpha);
                    }
                }
                
                // Create glow only near sprite edges
                float glowStrength = maxAlpha * (1.0 - texColor.a);
                glowStrength = pow(glowStrength, 0.5); // Adjust falloff curve
                
                // Apply glow
                fixed4 glowColor = _GlowColor * glowStrength * _GlowIntensity * 0.4;
                glowColor.a = glowStrength * 0.6;
                
                // Apply UI clipping
                glowColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                
                return glowColor;
            }
            ENDCG
        }
    }
}