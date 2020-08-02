Shader "Bansi/FishShaderGPU"
{
    Properties
    {
		_MainTex("Color Texture", 2D) = "white" {}
		_Shininess("Shininess", float) = 4.0
		_SpecularColor("Specular Color", Color) = (1.0, 1.0, 1.0)
		_AmbientColor("Ambient Color", Color) = (0.2, 0.2, 0.2, 1.0)
		_FishScale ("Fish Scale", Vector) = (1,1,1)

        _MovementSpeed ("Movement Speed", float) = 5.0
        _MaxMovementSpeed("Max Movement Speed", float) = 30.0
        _MinMovementSpeed("Min Movement Speed", float) = 2.0
        _SidewaysTranslationAmplitude ("Sideways Translation Amplitude", Range(0.0, 1.0)) = 0.1
        _YawRotationAmount ("Yaw Rotation Amount", Range(0.0, 90.0)) = 15.0
        _YawRotationOffset ("Yaw Rotation Offset", Range(0.0, 10.0)) = 5.1
        _RollRotationAmount("Roll Rotation Amount", Range(0.0, 60.0)) = 30.0
        _RollRotationOffset("Roll Rotation Offset", Range(0.0, 10.0)) = 6.65
        _WayvingAmount("Wayving Amount", Range(0.0, 1.0)) = 0.1
        _WayvingOffset("Wayving Offset", Range(0.0, 10.0)) = 6.2

        _MaskOffset ("Mask Offset", Range(-3.0, 3.0)) = -0.4
        _MaskSpread ("Mask Spread", Range(0.0001, 3.0)) = 1.5
    }
    SubShader
    {
    	// LightMode sets _WorldSpaceLightPos0 and _LightColor0 for main directional light
        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
			#pragma multi_compile_instancing // Needed for GPU instancing
			#pragma instancing_options procedural:setup // Needed for setting data about each fish (position, rotation, ...)
			#pragma target 4.5

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 objectSpacePosition : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID // Needed for GPU instancing
            };

            struct v2f
            {
                float4 worldPosition : SV_POSITION;
				float2 uv : TEXCOORD0;
				// Passing mask value to fragment shader
                float mask : TEXCOORD1;
                float4 worldNormal : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID // Needed for GPU instancing
				UNITY_FOG_COORDS(3)
            };			

			struct BoidInfo {
				float3 position;
				float3 acceleration;
				float3 velocity;
			};

#if SHADER_TARGET >= 45
			StructuredBuffer<BoidInfo> BoidsBuffer;
#endif

            static const float DEG_TO_RAD = 0.0174533;
            static const float PI = 3.14159265358;
            static const float LERP_FACTOR = 0.05;
            static const float VELOCITY_LERP_FACTOR = 0.1;

			float4 _FishScale;

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float _MovementSpeed;
			float _MaxMovementSpeed;
			float _MinMovementSpeed;

            float _SidewaysTranslationAmplitude;

            float _YawRotationAmount;
            float _YawRotationOffset;

            float _RollRotationAmount;
            float _RollRotationOffset;

            float _WayvingAmount;
            float _WayvingOffset;

            float _MaskOffset;
            float _MaskSpread;
            float _VisualizeMask;

            float _PreviousMovementSpeed;
            float3 _PreviousVelocity;

            float4 _AmbientColor;
            float4 _SpecularColor;
            float _Shininess;

            // Dummy function to tell Unity to use custom instancing data...
			void setup() 
			{
			}

			float4x4 CreateRotationMatrix(float3 forward) 
			{
				float3 up = float3(0, 1, 0);
				float3 zAxis = normalize(forward);

				float3 xAxis = normalize(cross(up, zAxis));
				float3 yAxis = normalize(cross(zAxis, xAxis));

				return float4x4 
					(xAxis.x, yAxis.x, zAxis.x, 0,
					 xAxis.y, yAxis.y, zAxis.y, 0,
					 xAxis.z, yAxis.z, zAxis.z, 0,
					 0, 0, 0, 1);
			}

			float2 RotateAroundPivot(float2 vertexPosition, float angleInDegrees){
            	float angleCos, angleSin;
            	float angleInRadians = angleInDegrees * DEG_TO_RAD;

            	// Get sin and cosine of an angle
            	sincos(angleInRadians, angleSin, angleCos);

            	float2x2 rotationMatrix = float2x2(angleCos, -angleSin, angleSin, angleCos);
            	return mul(rotationMatrix, vertexPosition);
            }

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
				UNITY_SETUP_INSTANCE_ID(v); // Needed for GPU instancing

#if SHADER_TARGET >= 45
				BoidInfo boidInfo = BoidsBuffer[instanceID];
#endif

                v2f o;

                // Clamp and lerp movement speed
                _MovementSpeed = clamp(length(boidInfo.acceleration) * 5, _MinMovementSpeed, _MaxMovementSpeed);
                _MovementSpeed = lerp(_PreviousMovementSpeed, _MovementSpeed, LERP_FACTOR);
                _MovementSpeed = clamp(_MovementSpeed, _MinMovementSpeed, _MaxMovementSpeed);
                _PreviousMovementSpeed = _MovementSpeed;

                // Lerp velocity for rotation purposes
                float3 currentVelocity = boidInfo.velocity;
                currentVelocity = lerp(_PreviousVelocity, currentVelocity, VELOCITY_LERP_FACTOR);
                _PreviousVelocity = currentVelocity;

				// Object space horizontal translation
                float translatedX = v.objectSpacePosition.x + _SidewaysTranslationAmplitude * sin(_Time.y * _MovementSpeed);
                float3 translatedObjectPosition = float3(translatedX, v.objectSpacePosition.y, v.objectSpacePosition.z);

                // Yaw rotation around pivot
                float yawRotationAngle = _YawRotationAmount * sin(_Time.y * _MovementSpeed + _YawRotationOffset);
                float2 rotatedPosition = RotateAroundPivot(float2(v.objectSpacePosition.x, v.objectSpacePosition.z), yawRotationAngle);
                float3 rotatedObjectPositionYaw = float3(rotatedPosition.x, v.objectSpacePosition.y, rotatedPosition.y);

                // Wayving motion along fish body
                float wayvingPositionX = v.objectSpacePosition.x + _WayvingAmount * sin(v.objectSpacePosition.z * PI + _Time.y * _MovementSpeed + _WayvingOffset) * v.objectSpacePosition.z;
                float3 wayvingObjectPosition = float3(wayvingPositionX, v.objectSpacePosition.y, v.objectSpacePosition.z);

                // Squishy roll rotation along fish spine
                float rollRotationAngle = sin(_Time.y * _MovementSpeed + v.objectSpacePosition.z * PI + _RollRotationOffset) * _RollRotationAmount;
                float2 rotatedPositionRoll = RotateAroundPivot(float2(v.objectSpacePosition.x, v.objectSpacePosition.y), rollRotationAngle);
                float3 rotatedObjectPositionRoll = float3(rotatedPositionRoll.x, rotatedPositionRoll.y, v.objectSpacePosition.z);

                // Calculating mask value
                float maskValue = v.objectSpacePosition.z * -1.0 - _MaskOffset;
                o.mask = saturate(maskValue / _MaskSpread);

                // Setting final object space position
                float3 maskBasedRollRotation = lerp(v.objectSpacePosition.xyz, rotatedObjectPositionRoll, o.mask);
                float3 maskBasedWayving = lerp(v.objectSpacePosition.xyz, wayvingObjectPosition, o.mask);
                v.objectSpacePosition.xyz = 0.25 * (translatedObjectPosition + rotatedObjectPositionYaw + maskBasedWayving + maskBasedRollRotation);

                // Calculating world position from object space position
				float4x4 rotationMatrix = CreateRotationMatrix(currentVelocity);
				float3 scaledLocalPosition = _FishScale.xyz * v.objectSpacePosition.xyz;
				float3 localPosition = mul(rotationMatrix, float4(scaledLocalPosition, 1.0)).xyz;
				float3 worldPosition = boidInfo.position.xyz + localPosition;

				// Calculating object projected pixel position
				o.worldPosition = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				// Calculating diffuse lighting
				half3 calculatedWorldNormal = UnityObjectToWorldNormal(v.normal);
				float3 rotatedNormal = normalize(mul(rotationMatrix, float4(calculatedWorldNormal, 1.0)).xyz);

				o.worldNormal = float4(rotatedNormal, 1.0);

				// Transferring fog to the fragment shader
				UNITY_TRANSFER_FOG(o, o.worldPosition);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);

				// Diffuse lighting calculation
				float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				float3 diffuseLighting = _LightColor0.xyz * max(0.0, dot(i.worldNormal, lightDirection));
				diffuseLighting += _AmbientColor.xyz;

				// Specular highlights
				float3 specularReflection = float3(0.0, 0.0, 0.0);
				if (dot(i.worldNormal, lightDirection) >= 0.0) 
            	{
					float3 viewDirection = normalize(float3(float4(_WorldSpaceCameraPos.xyz, 1.0) - i.worldPosition.xyz));
					specularReflection = _SpecularColor.rgb * _LightColor0.rgb * pow(max(0.0, dot(reflect(-lightDirection, i.worldNormal), viewDirection)), _Shininess);
            	}				

				float4 finalLight = float4(diffuseLighting + specularReflection, 1.0);
				fixed4 col = tex2D(_MainTex, i.uv) * finalLight;
				//col = saturate(col);

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				UNITY_OPAQUE_ALPHA(col.a);

				return col;
            }
            ENDCG
        }
    }
}
