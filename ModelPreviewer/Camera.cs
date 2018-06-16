using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace ModelPreviewer {

	public class Camera {
		
		public Vector3 target;		
		public Vector3 GetPosition() {
			return target + new Vector3(
				Cos(T * 0.5f) * Cos(Angle * Math.PI / 180) * Distance,
				Sin(Angle * Math.PI / 180) * Distance,
				Sin(T * 0.5f) * Cos(Angle * Math.PI / 180) * Distance
			);
		}
		
		public float Distance = 5, Angle = 45, FOV = 70, T;
		
		float Cos(double angle) { return (float)Math.Cos(angle); }		
		float Sin(double angle) { return (float)Math.Sin(angle); }
		
		public void UpdateView() {
			Matrix4 matrix = Matrix4.LookAt(GetPosition(),
			                                target, Vector3.UnitY);
			GL.LoadMatrix(ref matrix);
		}
		
		public void UpdateProjection(int width, int height) {
			float fovy = FOV * Utils.Deg2Rad;
			float ratio = width / (float)height;
			
			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(fovy, ratio, 0.01f, 1000f);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref projection);
		}	
	}
}
