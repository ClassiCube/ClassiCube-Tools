using System;
using System.Collections.Generic;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace ModelPreviewer {

	public class CustomModel : IModel {
		
		public List<RawPart> RawParts = new List<RawPart>();
		List<ModelPart> parts;
		
		public CustomModel(List<RawPart> inputParts) : base() {
			RawParts = inputParts;
			vertices = new ModelVertex[RawParts.Count * boxVertices];
			parts = new List<ModelPart>();
			
			foreach (RawPart raw in RawParts) {
				BoxDescription desc = MakeBoxBounds(raw.X1, raw.Y1, raw.Z1,
				                                    raw.X2, raw.Y2, raw.Z2);
				desc.SetTexOrigin(raw.TexX, raw.TexY);
				
				ModelPart part;
				if (raw.Rotated) {
					part = BuildRotatedBox(desc);
				} else {
					part = BuildBox(desc);
				}
				parts.Add(part);
			}
			
			/*
			 
			Set.Head = BuildBox( MakeBoxBounds( -4, 24, -4, 4, 32, 4 )
			                    .SetTexOrigin( 0, 0 ) );
			Set.Torso = BuildBox( MakeBoxBounds( -4, 12, -2, 4, 24, 2 )
			                     .SetTexOrigin( 16, 16 ) );
			Set.LeftLeg = BuildBox( MakeBoxBounds( 0, 0, -2, -4, 12, 2 )
			                       .SetTexOrigin( 0, 16 ) );
			Set.RightLeg = BuildBox( MakeBoxBounds( 0, 0, -2, 4, 12, 2 ).
			                        SetTexOrigin( 0, 16 ) );
			Set.Hat = BuildBox( MakeBoxBounds( -4, 24, -4, 4, 32, 4 )
			                   .SetTexOrigin( 32, 0 )
			                   .SetModelBounds( -4.5f, 23.5f, -4.5f, 4.5f, 32.5f, 4.5f ) );
			Set.LeftArm = BuildBox( MakeBoxBounds( -4, 12, -2, -8, 24, 2 )
			                       .SetTexOrigin( 40, 16 ) );
			Set.RightArm = BuildBox( MakeBoxBounds( 4, 12, -2, 8, 24, 2 )
			                        .SetTexOrigin( 40, 16 ) );
			 */
		}
		
		static float Animate(Player p, string anim) {
			if (anim == null || anim.Length == 0) return 0;	
			anim = anim.Replace(" ", "").ToLower();
			
			string expr = GetExpr(anim, 0);
			float angle = AnimateExpr(p, expr);
			
			for (int i = expr.Length; i < anim.Length;) {
				char op = anim[i]; i++;
				expr = GetExpr(anim, i); i += expr.Length;
				
				if (op == '-') angle -= AnimateExpr(p, expr);
				if (op == '+') angle += AnimateExpr(p, expr);
				if (op == '*') angle *= AnimateExpr(p, expr);
				if (op == '/') angle /= AnimateExpr(p, expr);
			}
			
			return angle;
		}
		
		static string GetExpr(string value, int i) {
			int j = i;
			
			for (; j < value.Length; j++) {
				char c = value[j];
				if (c == '-' || c == '+' || c == '*' || c == '/') break;
			}
			return value.Substring(i, j - i);
		}
		
		static float AnimateExpr(Player p, string anim) {
			if (anim == "") return 0;
			
			if (anim == "yaw")   return p.YawRadians;
			if (anim == "pitch") return p.PitchRadians;
			
			if (anim == "leftarmx") return p.leftArmXRot;
			if (anim == "leftarmz") return p.leftArmZRot;
			if (anim == "leftlegx") return p.leftLegXRot;
			
			if (anim == "rightarmx") return p.rightArmXRot;
			if (anim == "rightarmz") return p.rightArmZRot;
			if (anim == "rightlegx") return p.rightLegXRot;
			
			int angle;
			if (int.TryParse(anim, out angle)) return angle * Utils.Deg2Rad;
			
			return 0;
		}
		
		protected override void DrawPlayerModel(Player p) {
			Gfx.Texturing = true;
			Gfx.BindTexture( texId );
			
			for (int i = 0; i < RawParts.Count; i++) {
				RawPart raw = RawParts[i];
				Gfx.AlphaTest = raw.AlphaTesting;
				
				if (raw.Wireframe) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
				DrawRotate(raw.RotX / 16f, raw.RotY / 16f, raw.RotZ / 16f,
				           Animate(p, raw.XAnim), Animate(p, raw.YAnim), Animate(p, raw.ZAnim),
				           parts[i]);
				if (raw.Wireframe) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
			}
			
			/*
			DrawRotate( 0, 24/16f, 0, -p.PitchRadians, 0, 0, model.Head );
			DrawPart( model.Torso );
			DrawRotate( 0, 12/16f, 0, p.leftLegXRot, 0, 0, model.LeftLeg );
			DrawRotate( 0, 12/16f, 0, p.rightLegXRot, 0, 0, model.RightLeg );
			DrawRotate( -6/16f, 22/16f, 0, p.leftArmXRot, 0, p.leftArmZRot, model.LeftArm );
			DrawRotate( 6/16f, 22/16f, 0, p.rightArmXRot, 0, p.rightArmZRot, model.RightArm );
			
			Gfx.AlphaTest = true;
			DrawRotate( 0, 24f/16f, 0, -p.PitchRadians, 0, 0, model.Hat );
			 */
		}
	}
}