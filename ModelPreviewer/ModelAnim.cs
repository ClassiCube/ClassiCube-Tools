using System;

namespace ModelPreviewer {
	public static class ModelAnim {
		public static float Animate(Player p, string anim) {
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
			
			// e.g. pitch + 90
			int angle;
			if (int.TryParse(anim, out angle)) return angle * Utils.Deg2Rad;
			
			// e.g pitch * 0.5
			float value;
			if (float.TryParse(anim, out value)) return value;
			
			return 0;
		}
	}
}
