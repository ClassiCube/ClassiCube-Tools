using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;

namespace ModelPreviewer {
	
	public partial class MainForm : Form {
		
		bool loaded = false;
		int texId, gridTexId;
		public MainForm() {
			InitializeComponent();
			p = new Player();
			Application.Idle += InvalidateRenderer;
			this.MouseWheel += HandleMouseWheel;
		}
		Player p;
		Stopwatch sw;
		List<RawPart> parts = new List<RawPart>();
		
		void InvalidateRenderer(object sender, EventArgs e) {
			renderer.Invalidate();
		}
		
		void RendererLoad(object sender, EventArgs e) {
			try {
				byte[] data = Encoding.ASCII.GetBytes(ModelFormat.HumanoidRaw);
				MemoryStream ms = new MemoryStream(data);

				parts = ModelFormat.Import(ms);
				foreach (RawPart part in parts) {
					lbModels.Items.Add(part.Name);
				}
				lbModels.SelectedIndex = 0;
				RebuildModel();
				loaded = true;
				
				Color blue = Color.FromArgb(0, 0, 0x70);
				GL.ClearColor(blue);
				GL.AlphaFunc(AlphaFunction.Greater, 0.5f);
				camera.UpdateProjection(renderer.Width, renderer.Height);
			} catch (Exception ex) {
				Program.ShowError(ex);
			}
		}
		
		void MainFormLoad(object sender, EventArgs e) {
		}
		
		void HandleResize(object sender, EventArgs e) {
			if (!loaded) return;
		}
		
		RawPart cur;
		void RebuildModel() {
			if (cur == null) return;
			p.Model = new CustomModel(parts);
			p.Model.texId = texId;
		}
		
		const int ticksFrequency = 20;
		const double ticksPeriod = 1.0 / ticksFrequency;
		double ticksAccumulator = 0;
		int ticksDone = 0;
		void RendererPaint(object sender, PaintEventArgs e) {
			if (!loaded) return;
			
			try {
				if (sw == null) { sw = new Stopwatch(); }
				
				double delta = sw.Elapsed.TotalSeconds;
				sw.Reset();
				sw.Start();
				ticksAccumulator += delta;
				
				while(ticksAccumulator >= ticksPeriod) {
					if (p.Model != null) p.Tick(ticksPeriod);
					ticksAccumulator -= ticksPeriod;
					ticksDone++;
				}
				
				GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				GL.MatrixMode(MatrixMode.Modelview);
				camera.UpdateView();
				PaintBackground();
				
				GL.Enable(EnableCap.DepthTest);
				GL.Enable(EnableCap.AlphaTest);
				
				if (p.Model != null)
					p.Render(delta, (float)(ticksAccumulator / ticksPeriod));
				renderer.SwapBuffers();
			} catch (Exception ex) {
				Program.ShowError(ex);
			}
		}
		
		void PaintBackground() {
			Gfx.Texturing = false;
			GL.Begin(BeginMode.Quads);
			GL.Color3(0.2f, 0.2f, 0.2f);
			GL.Vertex3(-2f, -0.1f, -2f);
			GL.Vertex3(2f, -0.1f, -2f);
			GL.Vertex3(2f, -0.1f, 2f);
			GL.Vertex3(-2f, -0.1f, 2f);
			GL.Color3(1f, 1f, 1f);
			GL.End();
			
			GL.Begin(BeginMode.Lines);
			Line(1, 0, 0);
			Line(0, 1, 0);
			Line(0, 0, 1);
			GL.End();
			GL.Color3(1f, 1f, 1f);
		}
		
		void Line(float x, float y, float z) {
			GL.Color3(x, y, z);
			GL.Vertex3(0f, 0f, 0f);
			GL.Vertex3(x * 5, y * 5, -z * 5);
		}
		
		bool down;
		int oldmousex = 0, oldmousey = 0;
		Camera camera = new Camera();
		
		void HandleMouseWheel(object sender, MouseEventArgs e) {
			camera.Distance -= e.Delta / 120f;
			if (camera.Distance < 0.1f) camera.Distance = 0.1f;
			numDistance.Value = (decimal)camera.Distance;
		}
		
		void HandleMouseMove(object sender, MouseEventArgs e) {
			if(!down) return;
			int deltax = e.X - oldmousex, deltay = e.Y - oldmousey;
			oldmousex = e.X; oldmousey = e.Y;
			
			camera.T += deltax * 0.05f;
			camera.Angle += deltay;
			if(camera.Angle > 89) camera.Angle = 89;
			if(camera.Angle < -89) camera.Angle= -89;
		}
		
		void HandleMouseDown(object sender, MouseEventArgs e) {
			oldmousex = e.X; oldmousey = e.Y;
			down = true;
		}
		
		void HandleMouseUp(object sender, MouseEventArgs e) {
			down = false;
		}

		void CbStateSelectedIndexChanged(object sender, EventArgs e) {
			int i = cbState.SelectedIndex;
			if (i == -1) return;
			p.MoveType = (MoveType)Enum.Parse(typeof(MoveType), (string)cbState.Items[i], true);
		}
		
		void NumYawValueChanged(object sender, EventArgs e) {
			p.YawRadians = (int)numYaw.Value * Utils.Deg2Rad;
		}
		
		void NumPitchValueChanged(object sender, EventArgs e) {
			p.PitchRadians = (int)numPitch.Value * Utils.Deg2Rad;
		}
		
		
		void NumDistanceValueChanged(object sender, EventArgs e) {
			camera.Distance = (float)numDistance.Value;
		}
		
		void NumFovValueChanged(object sender, EventArgs e) {
			camera.FOV = (float)numFov.Value;
			GL.MatrixMode(MatrixMode.Projection);
			camera.UpdateProjection(renderer.Width, renderer.Height);
			GL.MatrixMode(MatrixMode.Modelview);
		}
		
		void NumXValueChanged(object sender, EventArgs e) {
			camera.target.X = (float)numX.Value / 16.0f;
		}
		
		void NumYValueChanged(object sender, EventArgs e) {
			camera.target.Y = (float)numY.Value / 16.0f;
		}
		
		void NumZValueChanged(object sender, EventArgs e) {
			camera.target.Z = (float)numZ.Value / 16.0f;
		}
		
		string DoOpenFileDialog(string filter) {
			using (OpenFileDialog d = new OpenFileDialog()) {
				d.RestoreDirectory = true;
				d.CheckFileExists = true;
				d.Filter = filter;
				
				DialogResult result = d.ShowDialog();
				if (result != DialogResult.OK) return null;
				return d.FileName;
			}
		}
		
		string DoSaveFileDialog(string filter) {
			using (SaveFileDialog d = new SaveFileDialog()) {
				d.RestoreDirectory = true;
				d.Filter = filter;
				
				DialogResult result = d.ShowDialog();
				if (result != DialogResult.OK) return null;
				return d.FileName;
			}
		}
		
		void BtnImportClick(object sender, EventArgs e) {
			string path = DoOpenFileDialog("Accepted File Types (*.txt)|*.txt|Model text files (*.txt)|*.txt");
			if (path == null) return;

			using (FileStream fs = File.OpenRead(path)) {
				parts = ModelFormat.Import(fs);
			}
			RebuildModel();
		}
		
		void BtnExportClick(object sender, EventArgs e) {
			string path = DoSaveFileDialog("Accepted File Types (*.txt)|*.txt|Model text files (*.txt)|*.txt");
			if (path == null) return;

			using (FileStream fs = File.Create(path)) {
				ModelFormat.Export(parts, fs);
			}
		}
		
		void BtnExportCodeClick(object sender, EventArgs e) {
			throw new NotImplementedException();
		}
		
		void BtnModelTexClick(object sender, EventArgs e) {
			string path = DoOpenFileDialog("Accepted File Types (*.png)|*.png|Images (*.png)|*.png");
			if (path == null) return;

			Gfx.DeleteTexture(ref texId);
			texId = Gfx.CreateTexture(path);
			p.Model.texId = texId;
		}
		
		void BtnGridTexClick(object sender, EventArgs e) {
			string path = DoOpenFileDialog("Accepted File Types (*.png)|*.png|Images (*.png)|*.png");
			if (path == null) return;

			Gfx.DeleteTexture(ref gridTexId);
			gridTexId = Gfx.CreateTexture(path);
			throw new NotImplementedException();
		}
		
		void BtnShowGridClick(object sender, EventArgs e) {
			throw new NotImplementedException();
		}
		
		void NumP1_XValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.X1 = (int)numP1_X.Value;
			RebuildModel();
		}
		
		void NumP1_YValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.Y1 = (int)numP1_Y.Value;
			RebuildModel();
		}
		
		void Num_p1ZValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.Z1 = (int)numP1_Z.Value;
			RebuildModel();
		}
		
		void NumP2_XValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.X2 = (int)numP2_X.Value;
			RebuildModel();
		}
		
		void NumP2_YValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.Y2 = (int)numP2_Y.Value;
			RebuildModel();
		}
		
		void NumP2_ZValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.Z2 = (int)numP2_Z.Value;
			RebuildModel();
		}
		
		void NumRotXValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.RotX = (int)numRotX.Value;
			RebuildModel();
		}
		
		void NumRotYValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.RotY = (int)numRotY.Value;
			RebuildModel();
		}
		
		void NumRotZValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.RotZ = (int)numRotZ.Value;
			RebuildModel();
		}
		
		void NumTexXValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.TexX = (int)numTexX.Value;
			RebuildModel();
		}
		
		void NumTexYValueChanged(object sender, EventArgs e) {
			if (cur != null) cur.TexY = (int)numTexY.Value;
			RebuildModel();
		}
		
		void CbRotateCheckedChanged(object sender, EventArgs e) {
			if (cur != null) cur.Rotated = cbRotate.Checked;
			RebuildModel();
		}
		
		void CbAlphaTestingCheckedChanged(object sender, EventArgs e) {
			if (cur != null) cur.AlphaTesting = cbAlphaTesting.Checked;
			RebuildModel();
		}
		
		void CbWireframeCheckedChanged(object sender, EventArgs e) {
			if (cur != null) cur.Wireframe = cbWireframe.Checked;
			RebuildModel();
		}
		
		
		void LbModelsSelectedIndexChanged(object sender, System.EventArgs e) {
			cur = null;
			if (lbModels.SelectedIndex == -1) return;
			RawPart p = parts[lbModels.SelectedIndex];
			
			numP1_X.Value = p.X1;   numP1_Y.Value = p.Y1;   numP1_Z.Value = p.Z1;
			numP2_X.Value = p.X2;   numP2_Y.Value = p.Y2;   numP2_Z.Value = p.Z2;
			numRotX.Value = p.RotX; numRotY.Value = p.RotY; numRotZ.Value = p.RotZ;
			
			numTexX.Value = p.TexX; numTexY.Value = p.TexY;
			
			txtName.Text = p.Name;
			txtXAnim.Text = p.XAnim;
			txtYAnim.Text = p.YAnim;
			txtZAnim.Text = p.ZAnim;
			
			cbWireframe.Checked = p.Wireframe;
			cbAlphaTesting.Checked = p.AlphaTesting;
			cbRotate.Checked = p.Rotated;
			
			cur = p;
		}
		
		void BtnAddClick(object sender, EventArgs e) {
			RawPart part = new RawPart();
			part.Name = "Part #" + (parts.Count + 1);
			parts.Add(part);
			lbModels.Items.Add(part.Name);
			RebuildModel();
		}
		
		void BtnDelClick(object sender, EventArgs e) {
			int i = lbModels.SelectedIndex;
			if (i == -1) return;
			
			parts.RemoveAt(i);
			lbModels.Items.RemoveAt(i);
			RebuildModel();
		}

		void TxtNameTextChanged(object sender, EventArgs e) {
			string name = txtName.Text;
			if (name == "") name = "----";
			
			if (cur != null) cur.Name = name;
			RebuildModel();
			
			int i = lbModels.SelectedIndex;
			if (i == -1) return;
			lbModels.Items[i] = name;
		}
		
		void TxtXAnimTextChanged(object sender, EventArgs e) {
			if (cur != null) cur.XAnim = txtXAnim.Text;
			RebuildModel();
		}
		
		void TxtYAnimTextChanged(object sender, EventArgs e) {
			if (cur != null) cur.YAnim = txtYAnim.Text;
			RebuildModel();
		}
		
		void TxtZAnimTextChanged(object sender, EventArgs e) {
			if (cur != null) cur.ZAnim = txtZAnim.Text;
			RebuildModel();
		}
	}
}
