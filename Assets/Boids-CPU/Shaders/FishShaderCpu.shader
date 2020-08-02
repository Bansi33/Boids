Shader "Bansi/FishShaderCpu"
{
	Properties
	{
		[Toggle] _UsingTexture("Using Texture", Float) = 1.0
		_MainTex("Albedo Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)

		_MovementSpeed("Movement Speed", float) = 5.0
		_SidewaysTranslationAmplitude("Sideways Translation Amplitude", Range(0.0, 1.0)) = 0.04

		_YawRotationAmount("Yaw Rotation Amount", Range(0.0, 90.0)) = 5
		_YawRotationOffset("Yaw Rotation Offset", Range(0.0, 10.0)) = 5.34

		_RollRotationAmount("Roll Rotation Amount", Range(0.0, 60.0)) = 5.0
		_RollRotationOffset("Roll Rotation Offset", Range(0.0, 10.0)) = 6.65

		_WayvingAmount("Wayving Amount", Range(0.0, 1.0)) = 0.191
		_WayvingOffset("Wayving Offset", Range(0.0, 10.0)) = 0.77
		_WayvingMultiplierAlongSpine("Wayving Multiplier Along Spine", Range(1.0, 2.0)) = 1.865

		_MaskOffset("Mask Offset", Range(-3.0, 3.0)) = -0.26
		_MaskSpread("Mask Spread", Range(0.0001, 3.0)) = 0.89
		[Toggle] _VisualizeMask("Visualize Mask", float) = 1.0

		_PitchRotationAmount("Pitch Rotation Amount", Range(-45.0, 45.0)) = 0.0
		[Toggle] _VisualizePitchMask("Visualize Pitch Mask", float) = 1.0
		_PitchMaskOffset("Pitch Mask Offset", Range(-3.0, 3.0)) = -0.23
		_PitchMaskSpread("Pitch Mask Spread", Range(0.0001, 3.0)) = 0.81
	}

		SubShader
		{
			Tags
			{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
			}

			LOD 100
			ZWrite On
			Blend SrcAlpha OneMinusSrcAlpha

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 objectSpacePosition : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					UNITY_FOG_COORDS(2)
					float4 worldSpacePosition : SV_POSITION;
					float mask : TEXCOORD1;
					float pitchMask : TEXCOORD4;
					float4 objectSpacePosition : TEXCOORD3;
				};

				static const float DEG_TO_RAD = 0.0174533;
				static const float PI = 3.14159265358;

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _Color;

				float _UsingTexture;
				float _MovementSpeed;
				float _SidewaysTranslationAmplitude;

				float _YawRotationAmount;
				float _YawRotationOffset;
				float _RollRotationAmount;
				float _RollRotationOffset;

				float _WayvingAmount;
				float _WayvingOffset;
				float _WayvingMultiplierAlongSpine;

				float _MaskOffset;
				float _MaskSpread;
				float _VisualizeMask;

				float _PitchMaskOffset;
				float _PitchMaskSpread;
				float _VisualizePitchMask;
				float _PitchRotationAmount;

				// Helper function for rotating point around object pivot            
				float2 RotateAroundPivot(float2 vertexPosition, float angleInDegrees)
				{
					// Get sin and cosine of an angle
					float angleCos, angleSin;
					float angleInRadians = angleInDegrees * DEG_TO_RAD;
					sincos(angleInRadians, angleSin, angleCos);

					// Calculate rotation matrix and use it for calculating rotated point position
					float2x2 rotationMatrix = float2x2(angleCos, -angleSin, angleSin, angleCos);
					return mul(rotationMatrix, vertexPosition);
				}

				v2f vert(appdata v)
				{
					v2f o;

					// Calculating mask value
					float maskValue = v.objectSpacePosition.z * (-1.0) - _MaskOffset;
					o.mask = saturate(maskValue / _MaskSpread);

					// Object space horizontal translation
					float translatedX = v.objectSpacePosition.x + _SidewaysTranslationAmplitude * sin(_Time.y * _MovementSpeed);
					float3 translatedObjectPosition = float3(translatedX, v.objectSpacePosition.y, v.objectSpacePosition.z);

					// Yaw rotation around pivot
					float yawRotationAngle = _YawRotationAmount * sin(_Time.y * _MovementSpeed + _YawRotationOffset);
					float2 rotatedPosition = RotateAroundPivot(float2(v.objectSpacePosition.x, v.objectSpacePosition.z), yawRotationAngle);
					float3 rotationOffset = float3(rotatedPosition.x - v.objectSpacePosition.x, 0.0, rotatedPosition.y - v.objectSpacePosition.z);

					// Pitch rotation around pivot
					float pitchLerpPercentage = v.objectSpacePosition.z + _PitchMaskOffset > 0.0 ? 0.0 : (-v.objectSpacePosition.z - _PitchMaskOffset);
					pitchLerpPercentage /= _PitchMaskSpread;
					o.pitchMask = pitchLerpPercentage;
					float pitchRotationAngle = lerp(0.0, -_PitchRotationAmount, pitchLerpPercentage);
					float2 pitchRotatedPosition = RotateAroundPivot(float2(v.objectSpacePosition.y, v.objectSpacePosition.z), pitchRotationAngle);
					float3 pitchRotationOffset = float3(0.0, pitchRotatedPosition.x - v.objectSpacePosition.y, pitchRotatedPosition.y - v.objectSpacePosition.z);

					// Wayving motion along fish body
					float wayvingOffsetX = o.mask * _WayvingAmount * sin(v.objectSpacePosition.z * PI + _Time.y * _MovementSpeed + _WayvingOffset) * lerp(0.0, _WayvingMultiplierAlongSpine, abs(v.objectSpacePosition.z));

					// Squishy roll rotation along fish spine
					float rollRotationAngle = sin(_Time.y * _MovementSpeed + v.objectSpacePosition.z * PI + _RollRotationOffset) * _RollRotationAmount;
					float2 rotatedPositionRoll = RotateAroundPivot(float2(v.objectSpacePosition.x, v.objectSpacePosition.y), rollRotationAngle);
					float3 rollRotationOffset = float3(rotatedPositionRoll.x - v.objectSpacePosition.x, rotatedPositionRoll.y - v.objectSpacePosition.y, 0.0);

					// Setting final object space position
					v.objectSpacePosition.xyz = (translatedObjectPosition + rotationOffset + float3(wayvingOffsetX, 0.0, 0.0) + rollRotationOffset + pitchRotationOffset);
					o.worldSpacePosition = UnityObjectToClipPos(v.objectSpacePosition);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					UNITY_TRANSFER_FOG(o,o.worldSpacePosition);
					o.objectSpacePosition = v.objectSpacePosition;

					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					fixed4 col;

					if (_VisualizeMask)
					{
						col = fixed4(i.mask, i.mask, i.mask, 1.0);
					}
					else if (_VisualizePitchMask)
					{
						col = fixed4(i.pitchMask, 0.0, 0.0, 1.0);
					}
					else
					{
						col = _UsingTexture ? tex2D(_MainTex, i.uv) : _Color;
					}

					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
				ENDCG
			}
		}
}
