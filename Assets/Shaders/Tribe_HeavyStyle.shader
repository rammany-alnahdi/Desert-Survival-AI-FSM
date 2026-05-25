Shader "Custom/Tribe_LayeredWarrior"
{
    Properties
    {
        // This is the 'map' that tells the shader where the chest plates are
        [MainTexture] _BaseMap("Mask Map (R=Armor, G=Dress, B=Skin)", 2D) = "white" {}
        
        _SkinColor ("Skin Tone", Color) = (0.8, 0.6, 0.5, 1)
        _ClothColor ("Dress Color (Heavy Maroon)", Color) = (0.35, 0.1, 0.1, 1)
        _ArmorColor ("Chest Plate Color (Iron)", Color) = (0.2, 0.2, 0.2, 1)
        
        _ArmorGlint ("Metal Shininess", Range(0, 2)) = 1.0
    }

    SubShader
    {
        // These tags tell Unity this is a URP shader (Stops the Pink Error)
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Import URP core libraries
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Setup variables in HLSL memory
            sampler2D _BaseMap;
            float4 _SkinColor, _ClothColor, _ArmorColor;
            float _ArmorGlint;

            Varyings vert(Attributes input) {
                Varyings output;
                // Convert 3D position to your Screen position
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // Read the Mask texture
                half4 mask = tex2D(_BaseMap, input.uv);
                
                // 1. Start with Skin as the base
                half3 finalColor = _SkinColor.rgb;

                // 2. Use the GREEN channel of the texture to paint the Dress
                finalColor = lerp(finalColor, _ClothColor.rgb, mask.g);

                // 3. Use the RED channel of the texture to paint the Armor Plates
                // We add 'Glint' to make the metal pop like in your photo
                half3 metal = _ArmorColor.rgb + (mask.r * _ArmorGlint * 0.2);
                finalColor = lerp(finalColor, metal, mask.r);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}