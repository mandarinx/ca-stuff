// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/VertexColorUnlit" {
    SubShader {
        Tags { "RenderType"="Opaque" }
     
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
             
                #include "UnityCG.cginc"
 
                struct VertOut {
                    float4 position : POSITION;
                     float4 color : COLOR;
                };
 
                struct VertIn {
                    float4 vertex : POSITION;
                     float4 color : COLOR;
                };
             
                VertOut vert (VertIn input)
                {
                    VertOut output;
                    output.position = UnityObjectToClipPos(input.vertex);
                    output.color = input.color;
                    return output;
                }
             
                float4 frag (VertOut input) : SV_Target
                {
                    return input.color;
                }
            ENDCG
        }
    }
}
