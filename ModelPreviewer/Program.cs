using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ModelPreviewer {
	
	internal sealed class Program {
		[STAThread]
		private static void Main(string[] args) {
			AppDomain.CurrentDomain.UnhandledException += ShowUnhandledException;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
		
		static string Format(Exception ex) {
			try {
				string msg = ex.GetType().FullName + ": " + ex.Message + Environment.NewLine + ex.StackTrace;
				ExternalException nativeEx = ex as ExternalException;
				
				if (nativeEx == null) return msg;			
				return msg + Environment.NewLine + "HRESULT: " + nativeEx.ErrorCode;
			} catch (Exception) {
				return "";
			}
		}

		static void ShowUnhandledException(object sender, UnhandledExceptionEventArgs e) {
			ShowError((Exception)e.ExceptionObject);
		}
		
		public static void ShowError(Exception ex) {
			MessageBox.Show("Please give this to UnknownShadow200:\r\n\r\n" +
			                Format(ex), "ModelPreviewer crashed");
		}
	}
}
