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
		
		protected override void DrawPlayerModel(Player p) {
			Gfx.Texturing = true;
			Gfx.BindTexture( texId );
			
			for (int i = 0; i < RawParts.Count; i++) {
				RawPart raw = RawParts[i];
				Gfx.AlphaTest = raw.AlphaTesting;
				
				if (raw.Wireframe) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
				DrawRotate(p, raw, parts[i]);
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