Shader "Particles/FireGlowParticle"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2.0
        _GlowColor ("Glow Color", Color) = (1, 0.5, 0, 1)
        _InnerGlow ("Inner Glow", Range(0, 1)) = 0.5
        _OuterGlow ("Outer Glow", Range(0, 2)) = 1.0
        _SoftParticlesFactor ("Soft Particles Factor", Range(0.01, 3.0)) = 1.0
    }

    Category
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Transparent" 
            "PreviewType" = "Plane"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        Cull Off 
        Lighting Off 
        ZWrite Off

        SubShader
        {
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_particles
                #pragma multi_compile_fog

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                fixed4 _TintColor;
                fixed4 _GlowColor;
                float _GlowIntensity;
                float _InnerGlow;
                float _OuterGlow;

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    #ifdef SOFTPARTICLES_ON
                    float4 projPos : TEXCOORD2;
                    #endif
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                float4 _MainTex_ST;

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    #ifdef SOFTPARTICLES_ON
                    o.projPos = ComputeScreenPos (o.vertex);
                    COMPUTE_EYEDEPTH(o.projPos.z);
                    #endif
                    o.color = v.color * _TintColor;
                    o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }

                UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
                float _SoftParticlesFactor;

                fixed4 frag (v2f i) : SV_Target
                {
                    fixed4 texColor = tex2D(_MainTex, i.texcoord);
                    
                    // Early exit if completely transparent
                    if (texColor.a < 0.01) discard;

                    #ifdef SOFTPARTICLES_ON
                    float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                    float partZ = i.projPos.z;
                    float fade = saturate (_SoftParticlesFactor * (sceneZ-partZ));
                    texColor.a *= fade;
                    #endif

                    // Sample nearby pixels for edge-based glow
                    float2 texelSize = float2(0.002, 0.002); // Adjusted for typical particle textures
                    
                    // Sample surrounding pixels for edge detection
                    float alpha = texColor.a;
                    float alphaUp = tex2D(_MainTex, i.texcoord + float2(0, texelSize.y)).a;
                    float alphaDown = tex2D(_MainTex, i.texcoord + float2(0, -texelSize.y)).a;
                    float alphaLeft = tex2D(_MainTex, i.texcoord + float2(-texelSize.x, 0)).a;
                    float alphaRight = tex2D(_MainTex, i.texcoord + float2(texelSize.x, 0)).a;
                    
                    // Calculate maximum nearby alpha
                    float maxAlpha = max(max(alphaUp, alphaDown), max(alphaLeft, alphaRight));
                    
                    // Base color
                    fixed4 baseColor = i.color * texColor;
                    
                    // Create glow based on sprite shape
                    float glowFactor = 0.0;
                    
                    if (alpha > 0.01) {
                        // Inner glow - brighten opaque areas
                        glowFactor = alpha * _InnerGlow;
                    } else if (maxAlpha > 0.01) {
                        // Outer glow - glow around transparent areas near opaque ones
                        glowFactor = maxAlpha * _OuterGlow * 0.6;
                    }
                    
                    glowFactor *= _GlowIntensity;
                    
                    // Apply glow color
                    fixed4 glow = _GlowColor * glowFactor;
                    
                    // Combine base color with glow
                    fixed4 finalColor = baseColor + (glow * (1.0 - alpha * 0.5));
                    finalColor += glow * alpha * 0.4; // Inner glow
                    
                    // Set alpha based on original sprite or glow
                    finalColor.a = max(baseColor.a, glowFactor * 0.3);
                    
                    UNITY_APPLY_FOG(i.fogCoord, finalColor);
                    return finalColor;
                }
                ENDCG
            }
            
            // Second pass for additional outer glow
            Pass
            {
                Blend One One
                ZWrite Off
                
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment fragGlow
                #pragma target 2.0
                #pragma multi_compile_particles
                #pragma multi_compile_fog

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                fixed4 _TintColor;
                fixed4 _GlowColor;
                float _GlowIntensity;
                float _OuterGlow;

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    #ifdef SOFTPARTICLES_ON
                    float4 projPos : TEXCOORD2;
                    #endif
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                float4 _MainTex_ST;

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    #ifdef SOFTPARTICLES_ON
                    o.projPos = ComputeScreenPos (o.vertex);
                    COMPUTE_EYEDEPTH(o.projPos.z);
                    #endif
                    o.color = v.color * _TintColor;
                    o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }

                UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
                float _SoftParticlesFactor;

                fixed4 fragGlow (v2f i) : SV_Target
                {
                    fixed4 texColor = tex2D(_MainTex, i.texcoord);
                    
                    // Don't render glow where sprite is fully opaque
                    if (texColor.a > 0.9) return fixed4(0,0,0,0);

                    #ifdef SOFTPARTICLES_ON
                    float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                    float partZ = i.projPos.z;
                    float fade = saturate (_SoftParticlesFactor * (sceneZ-partZ));
                    #endif

                    // Sample nearby pixels to detect sprite edges
                    float2 texelSize = float2(0.003, 0.003) * _OuterGlow;
                    
                    // Sample in multiple directions for better edge detection
                    float maxAlpha = 0.0;
                    for (int x = -1; x <= 1; x++) {
                        for (int y = -1; y <= 1; y++) {
                            if (x == 0 && y == 0) continue;
                            float2 offset = float2(x, y) * texelSize;
                            float sampleAlpha = tex2D(_MainTex, i.texcoord + offset).a;
                            maxAlpha = max(maxAlpha, sampleAlpha);
                        }
                    }
                    
                    // Create glow only near sprite edges
                    float glowStrength = maxAlpha * (1.0 - texColor.a);
                    glowStrength = pow(glowStrength, 0.7); // Adjust falloff curve
                    
                    // Apply particle color modulation
                    fixed4 glowColor = _GlowColor * i.color * glowStrength * _GlowIntensity * 0.5;
                    glowColor.a = glowStrength * 0.4;

                    #ifdef SOFTPARTICLES_ON
                    glowColor.a *= fade;
                    #endif
                    
                    UNITY_APPLY_FOG_COLOR(i.fogCoord, glowColor, fixed4(0,0,0,0));
                    return glowColor;
                }
                ENDCG
            }
        }
    }
}