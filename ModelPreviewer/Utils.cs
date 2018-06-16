using System;
using OpenTK;

namespace ModelPreviewer {
	
	/// <summary> Stores the four texture coordinates that describe a textured quad. </summary>
	public struct TextureRec {
		public float U1, V1, U2, V2;
		
		public TextureRec( float u, float v, float uWidth, float vHeight ) {
			U1 = u; V1 = v;
			U2 = u + uWidth; V2 = v + vHeight;
		}
		
		public static TextureRec FromPoints( float u1, float u2, float v1, float v2 ) {
			TextureRec rec;
			rec.U1 = u1;rec.U2 = u2;
			rec.V1 = v1; rec.V2 = v2;
			return rec;
		}

		public override string ToString() {
			return String.Format( "{0}, {1} : {2}, {3}", U1, V1, U2, V2 );
		}
		
		internal void SwapU() {
			float u2 = U2;
			U2 = U1;
			U1 = u2;
		}
	}
	
	public static class Utils {
		
		public static float Lerp( float a, float b, float t ) {
			return a + ( b - a ) * t;
		}
		
		/// <summary> Multiply a value in degrees by this to get its value in radians. </summary>
		public const float Deg2Rad = (float)(Math.PI / 180);
		/// <summary> Multiply a value in radians by this to get its value in degrees. </summary>
		public const float Rad2Deg = (float)(180 / Math.PI);
		
		/// <summary> Clamps that specified value such that min ≤ value ≤ max </summary>
		public static void Clamp( ref float value, float min, float max ) {
			if( value < min ) value = min;
			if( value > max ) value = max;
		}
		
		/// <summary> Rotates the given 3D coordinates around the y axis. </summary>
		public static Vector3 RotateY( Vector3 v, float angle ) {
			float cosA = (float)Math.Cos( angle );
			float sinA = (float)Math.Sin( angle );
			return new Vector3( cosA * v.X - sinA * v.Z, v.Y, sinA * v.X + cosA * v.Z );
		}
		
		/// <summary> Rotates the given 3D coordinates around the y axis. </summary>
		public static Vector3 RotateY( float x, float y, float z, float angle ) {
			float cosA = (float)Math.Cos( angle );
			float sinA = (float)Math.Sin( angle );
			return new Vector3( cosA * x - sinA * z, y, sinA * x + cosA * z );
		}
		
		/// <summary> Rotates the given 3D coordinates around the x axis. </summary>
		public static Vector3 RotateX( Vector3 p, float cosA, float sinA ) {
			return new Vector3( p.X, cosA * p.Y + sinA * p.Z, -sinA * p.Y + cosA * p.Z );
		}
		
		/// <summary> Rotates the given 3D coordinates around the x axis. </summary>
		public static Vector3 RotateX( float x, float y, float z, float cosA, float sinA ) {
			return new Vector3( x, cosA * y + sinA * z, -sinA * y + cosA * z );
		}
		
		/// <summary> Rotates the given 3D coordinates around the y axis. </summary>
		public static Vector3 RotateY( Vector3 p, float cosA, float sinA ) {
			return new Vector3( cosA * p.X - sinA * p.Z, p.Y, sinA * p.X + cosA * p.Z );
		}
		
		/// <summary> Rotates the given 3D coordinates around the y axis. </summary>
		public static Vector3 RotateY( float x, float y, float z, float cosA, float sinA ) {
			return new Vector3( cosA * x - sinA * z, y, sinA * x + cosA * z );
		}
		
		/// <summary> Rotates the given 3D coordinates around the z axis. </summary>
		public static Vector3 RotateZ( Vector3 p, float cosA, float sinA ) {
			return new Vector3( cosA * p.X + sinA * p.Y, -sinA * p.X + cosA * p.Y, p.Z );
		}		
		
		/// <summary> Rotates the given 3D coordinates around the z axis. </summary>
		public static Vector3 RotateZ( float x, float y, float z, float cosA, float sinA ) {
			return new Vector3( cosA * x + sinA * y, -sinA * x + cosA * y, z );
		}
	}
}