﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIS.Items.Weapons;

namespace SIS.Characters.Sis
{
	public class IKAiming : MonoBehaviour
	{
		Animator anim;
		Sis owner;

		float handWeight;
		float lookWeight;
		float bodyWeight;

		Transform rhTarget;
		public Transform shoulder;
		public Transform aimPivot;
		Vector3 lookDir;
		Weapon curWeapon;
		RecoilHandler recoilHandler;
		Vector3 basePosition;
		Vector3 baseRotation;

		public void Init(Sis sis)
		{
			owner = sis;
			anim = owner.anim;

			if (shoulder == null)
				shoulder = anim.GetBoneTransform(HumanBodyBones.RightShoulder);

			CreateAimPivot();
			CreateRightHandTarget();
			//Setup Aim Position
			owner.movementValues.aimPosition = owner.mTransform.position + owner.mTransform.forward * 15;
			owner.movementValues.aimPosition.y += 1.4f;

			recoilHandler = new RecoilHandler();
		}

		
		//Update pivot and right hand
		private void OnAnimatorMove()
		{
			if (owner == null)
				return;
			lookDir = CaculateLookDirection();
			HandlePivot();
		}

		#region Right Hand Target is on Weapon
		//Creates empty object for right hand target
		void CreateRightHandTarget()
		{
			rhTarget = new GameObject().transform;
			rhTarget.name = "Right Hand Target";
			rhTarget.parent = aimPivot;
		}
		//Sets right hand target ontop of weapon
		//Activate method whenever you change weapons
		public void UpdateWeaponAiming(Weapon w)
		{
			curWeapon = w;
			if (w == null)
				return;
			//Update so right hand is holding weapon
			rhTarget.localPosition = w.holdingPosition.value;
			rhTarget.localEulerAngles = w.holdingEulers.value;

			//Store local transform for recoil
			basePosition = rhTarget.localPosition;
			baseRotation = rhTarget.localEulerAngles;
		}
		#endregion

		#region Pivot towards Look Direction
		//Creates Aiming Pivot
		void CreateAimPivot()
		{
			aimPivot = new GameObject().transform;
			aimPivot.name = "Aim Pivot";
			aimPivot.parent = owner.transform;
		}

		//Handles Transform
		void HandlePivot()
		{
			HandlePivotPosition();
			HandlePivotRotation();
		}

		//Pivot around shoulder
		void HandlePivotPosition()
		{
			aimPivot.position = shoulder.position;
		}

		//Rotate towards aiming target
		void HandlePivotRotation()
		{
			float speed = 15;
			Vector3 targetDir = lookDir;
			if (targetDir == Vector3.zero)
				targetDir = aimPivot.forward;
			Quaternion tr = Quaternion.LookRotation(targetDir);
			aimPivot.rotation = Quaternion.Slerp(aimPivot.rotation, tr, owner.delta * speed);
		}

		//Calculates the direction to aim towards
		private Vector3 CaculateLookDirection()
		{
			return owner.movementValues.aimPosition - owner.mTransform.position;
		}
		#endregion

		//Uses Mecanim's IK feature to look at aimPosition
		private void OnAnimatorIK()
		{
			if (owner == null) {
				Debug.Log("Owner is null in IK Aiming");
				return;
			}

			HandleWeights();

			anim.SetLookAtWeight(lookWeight, bodyWeight, 1, 1, 1);
			anim.SetLookAtPosition(owner.movementValues.aimPosition);

			UpdateIK(AvatarIKGoal.RightHand, rhTarget, handWeight);
		}

		//Tunes IK weights based on a number of factors
		void HandleWeights()
		{
			//targets to lerp to
			float targetLookWeight = 1;
			float targetHandWeight = 0;

			//Intensity if aiming down sights
			if (owner.isGunReady)
				targetHandWeight = 0.5f;
			if (owner.isShooting)
				targetHandWeight = 0.075f;
			if (owner.isAiming) {
				targetHandWeight = 1;
				bodyWeight = 0.4f;
			}
			else
				bodyWeight = 0.3f;

			//Constraints on IK for looking at sharp angles
			float angle = Vector3.Angle(owner.mTransform.forward, lookDir);
			if (angle > 45)
				targetLookWeight = 0;

			if (angle > 60)
				targetHandWeight = 0;

			//Smoothly Change Weights
			lookWeight = Mathf.Lerp(lookWeight, targetLookWeight, owner.delta);
			handWeight = Mathf.Lerp(handWeight, targetHandWeight, 9 * owner.delta);
		}
		#region Recoil
		//Start the recoil process
		public void StartRecoil()
		{
			recoilHandler.StartRecoil(curWeapon.recoilLength);
		}

		//Tick the recoil and update offset positions
		public void HandleRecoil()
		{
			recoilHandler.Tick(curWeapon, owner.delta);
			rhTarget.localPosition = basePosition + recoilHandler.OffsetPosition;
			rhTarget.localEulerAngles = baseRotation + recoilHandler.OffsetRotation;
		}
		#endregion

		//Helper Functions
		private void UpdateIK(AvatarIKGoal goal, Transform t, float w)
		{
			anim.SetIKPositionWeight(goal, w);
			anim.SetIKRotationWeight(goal, w);
			anim.SetIKPosition(goal, t.position);
			anim.SetIKRotation(goal, t.rotation);
		}
	}
}
