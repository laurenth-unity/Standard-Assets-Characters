﻿using StandardAssets.Characters.Physics;
using UnityEngine;
using Util;

namespace StandardAssets.Characters.ThirdPerson
{
	public class AnimationThirdPersonMotor : InputThirdPersonMotor
	{
		[SerializeField]
		private AnimationMotorProperties animationMotorProperties;

		[SerializeField]
		private bool inheritGroundVelocity;

		private float normalizedInputLateralSpeed;
		private float normalizedInputForwardSpeed;

		private Vector3 groundMovementVector, cacheGroundMovementVector;

		public override float normalizedLateralSpeed
		{
			get { return -Mathf.Clamp(normalizedInputLateralSpeed, -1, 1); }
		}

		public override float normalizedForwardSpeed
		{
			get { return Mathf.Clamp(normalizedInputForwardSpeed, -1, 1); }
		}

		private float clampSpeed
		{
			get
			{
				if (isRunToggled)
				{
					return 1f;
				}

				return animationMotorProperties.walkSpeedProportion;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (inheritGroundVelocity)
			{
				characterInput.jumpPressed += CacheCurrentMovement;
			}
		}

		private void CacheCurrentMovement()
		{
			cacheGroundMovementVector = groundMovementVector;
		}

		protected override void CalculateForwardMovement()
		{
			if (!characterPhysics.isGrounded)
			{
				return;
			}

			if (!characterInput.hasMovementInput)
			{
				EaseOffForwardInput();
				characterPhysics.Move(Vector3.zero);
				return;
			}

			ApplyForwardInput(1f);
		}

		protected override void CalculateStrafeMovement()
		{
			if (!characterPhysics.isGrounded)
			{
				return;
			}

			Vector2 moveInput = characterInput.moveInput;

			// we need to ease each axis
			if (Mathf.Abs(moveInput.y) > Mathf.Epsilon)
			{
				ApplyForwardInput(Mathf.Sign(moveInput.y));
			}
			else
			{
				EaseOffForwardInput();
			}

			if (Mathf.Abs(moveInput.x) > Mathf.Epsilon)
			{
				ApplyLateralInput(Mathf.Sign(moveInput.x));
			}
			else
			{
				EaseOffLateralInput();
			}
		}

		protected override bool CanSetForwardLookDirection()
		{
			return characterInput.hasMovementInput;
		}

		protected override void HandleTargetRotation(Quaternion targetRotation)
		{
			float angleDifference = Mathf.Abs((transform.eulerAngles - targetRotation.eulerAngles).y);

			float calculatedTurnSpeed = turnSpeed;
			if (angleSnapBehaviour < angleDifference && angleDifference < 360 - angleSnapBehaviour)
			{
				// rapid turn deceleration complete, now we rotate appropriately.
				calculatedTurnSpeed += (maxTurnSpeed - turnSpeed) *
				                       turnSpeedAsAFunctionOfForwardSpeed.Evaluate(Mathf.Abs(normalizedForwardSpeed));
			}

			float actualTurnSpeed = calculatedTurnSpeed;
			if (!characterPhysics.isGrounded)
			{
				actualTurnSpeed *= airborneTurnSpeedProportion;
			}

			targetRotation =
				Quaternion.RotateTowards(transform.rotation, targetRotation, actualTurnSpeed * Time.fixedDeltaTime);

			transform.rotation = targetRotation;
		}

		private void ApplyForwardInput(float input)
		{
			float forwardVelocity = animationMotorProperties.forwardInputVelocity;
			if (Mathf.Abs(Mathf.Sign(input) - Mathf.Sign(normalizedInputForwardSpeed)) > 0)
			{
				forwardVelocity = animationMotorProperties.forwardInputChangeVelocity;
			}

			normalizedInputForwardSpeed =
				Mathf.Clamp(normalizedInputForwardSpeed + input * forwardVelocity * Time.fixedDeltaTime, -clampSpeed,
				            clampSpeed);
		}

		private void EaseOffForwardInput()
		{
			normalizedInputForwardSpeed =
				Mathf.Lerp(normalizedInputForwardSpeed, 0, animationMotorProperties.forwardInputDecay * Time.fixedDeltaTime);
		}

		private void ApplyLateralInput(float input)
		{
			float lateralVelocity = animationMotorProperties.lateralInputVelocity;
			if (Mathf.Abs(Mathf.Sign(input) - Mathf.Sign(normalizedInputLateralSpeed)) > 0)
			{
				lateralVelocity = animationMotorProperties.lateralInputChangeVelocity;
			}

			normalizedInputLateralSpeed =
				Mathf.Clamp(normalizedInputLateralSpeed + input * lateralVelocity * Time.fixedDeltaTime, -clampSpeed,
				            clampSpeed);
		}

		private void EaseOffLateralInput()
		{
			normalizedInputLateralSpeed =
				Mathf.Lerp(normalizedInputLateralSpeed, 0, animationMotorProperties.lateralInputDecay * Time.fixedDeltaTime);
		}

		private void OnAnimatorMove()
		{
			if (!inheritGroundVelocity || characterPhysics.isGrounded)
			{
				groundMovementVector = new Vector3(animator.deltaPosition.x, 0, animator.deltaPosition.z);
				characterPhysics.Move(groundMovementVector);
			}
			else
			{
				characterPhysics.Move(cacheGroundMovementVector);
			}
		}
	}
}