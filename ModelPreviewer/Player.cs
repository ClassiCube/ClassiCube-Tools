using System;
using System.Collections.Generic;
using OpenTK;

namespace ModelPreviewer {

	public enum MoveType { Still, Idle, Walk, Run, WalkNod, RunNod }
	
	public class Player {
		public Vector3 Position;
		public float YawRadians, PitchRadians;
		public IModel Model;
		public float leftLegXRot, leftArmXRot, leftArmZRot;
		public float rightLegXRot, rightArmXRot, rightArmZRot;
		protected float walkTimeO, walkTimeN, swingO, swingN;
		public float Accumulator;		
		public MoveType MoveType;
		
		public void Render(double deltaTime, float t) {
			Accumulator += (float)deltaTime;
			GetCurrentAnimState(t);
			Model.RenderModel(this);
		}		
		
		
		public void Tick(double delta) {
			walkTimeO = walkTimeN;
			swingO = swingN;
			bool walk = MoveType == MoveType.Walk || MoveType == MoveType.WalkNod;
			double distance = MoveType == MoveType.Idle ? 0 : (walk ? 0.06 : 0.26);
			
			if (distance > 0.05) {
				walkTimeN += (float)distance * 2 * (float)(20 * delta);
				swingN += (float)delta * 3;
			} else {
				swingN -= (float)delta * 3;
			}
			Utils.Clamp(ref swingN, 0, 1);
			
			if (MoveType == MoveType.WalkNod || MoveType == MoveType.RunNod) {
				PitchRadians += 2 * Utils.Deg2Rad;
			}
		}
		
		const float armMax = 60 * Utils.Deg2Rad;
		const float legMax = 80 * Utils.Deg2Rad;
		const float idleMax = 3 * Utils.Deg2Rad;
		const float idleXPeriod = (float)(2 * Math.PI / 5.0f);
		const float idleZPeriod = (float)(2 * Math.PI / 3.5f);
		
		void GetCurrentAnimState(float t) {
			float swing = Utils.Lerp(swingO, swingN, t);
			float walkTime = Utils.Lerp(walkTimeO, walkTimeN, t);
			float idleTime = (float)Accumulator;
			float idleXRot = (float)(Math.Sin(idleTime * idleXPeriod) * idleMax);
			float idleZRot = (float)(idleMax + Math.Cos(idleTime * idleZPeriod) * idleMax);
			
			leftArmXRot = (float)(Math.Cos(walkTime) * swing * armMax) - idleXRot;
			rightArmXRot = -leftArmXRot;
			rightLegXRot = (float)(Math.Cos(walkTime) * swing * legMax);
			leftLegXRot = -rightLegXRot;
			rightArmZRot = idleZRot;
			leftArmZRot = -idleZRot;
			
			if (MoveType == MoveType.Still) {
				leftArmXRot = 0; rightArmXRot = 0;
				leftLegXRot = 0; rightLegXRot = 0;
				leftArmZRot = 0; rightArmZRot = 0;
			}
		}
	}
}