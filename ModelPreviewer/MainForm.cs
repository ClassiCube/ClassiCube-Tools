using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace ModelPreviewer {
	
	public partial class MainForm : Form {
		
		bool loaded = false;
		int texId, floorTexId;
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
		
		void Import(Stream src) {
			parts = ModelFormat.Import(src);
			lbModels.Items.Clear();
			
			foreach (RawPart part in parts) {
				lbModels.Items.Add(part.Name);
			}
			
			lbModels.SelectedIndex = 0;
			RebuildModel();
		}
		
		void RendererLoad(object sender, EventArgs e) {
			try {
				byte[] data = Encoding.ASCII.GetBytes(ModelFormat.HumanoidRaw);
				Import(new MemoryStream(data));
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
				
				PaintFloor();
				PaintGridLines();
				PaintAxisLines();
				
				GL.Enable(EnableCap.DepthTest);
				GL.Enable(EnableCap.AlphaTest);
				
				if (p.Model != null) {
					p.Render(delta, (float)(ticksAccumulator / ticksPeriod));
				}
				
				if (cur != null && cur.AxisLines) {
					Gfx.Texturing = false;
					Vector3 b = new Vector3(cur.RotX / 16.0f, cur.RotY / 16.0f, cur.RotZ / 16.0f);
					
					Vector3 x = p.Model.RotatePoint(p, cur, b + new Vector3(1, 0, 0));
					Vector3 y = p.Model.RotatePoint(p, cur, b + new Vector3(0, 1, 0));
					Vector3 z = p.Model.RotatePoint(p, cur, b + new Vector3(0, 0, -1));
					
					b = p.Model.RotatePoint(p, cur, b);				
					GL.LineWidth(3f);
					GL.Begin(BeginMode.Lines);
					
					GL.Color3(1f, 0f, 0f); GL.Vertex3(b); GL.Vertex3(x);
					GL.Color3(0f, 1f, 0f); GL.Vertex3(b); GL.Vertex3(y);					
					GL.Color3(0f, 0f, 1f); GL.Vertex3(b); GL.Vertex3(z);
					
					GL.End();
					GL.LineWidth(1f);
					GL.Color3(1f, 1f, 1f);
				}
				renderer.SwapBuffers();
			} catch (Exception ex) {
				Program.ShowError(ex);
			}
		}
		
		
		void PaintFloor() {
			Gfx.Texturing = true;
			Gfx.BindTexture(floorTexId);
			GL.Begin(BeginMode.Quads);
			GL.Color3(0.7f, 0.7f, 0.7f);
			
			GL.TexCoord2(0f, 0f); GL.Vertex3(-2f, -0.1f, -2f);
			GL.TexCoord2(2f, 0f); GL.Vertex3(2f, -0.1f, -2f);
			GL.TexCoord2(2f, 2f); GL.Vertex3(2f, -0.1f, 2f);
			GL.TexCoord2(0f, 2f); GL.Vertex3(-2f, -0.1f, 2f);
			
			GL.Color3(1f, 1f, 1f);
			GL.End();
			Gfx.Texturing = false;
		}
		
		bool showXGrid = true, showYGrid = true, showZGrid = true;
		void PaintGridLines() {
			GL.Begin(BeginMode.Lines);
			GL.Color3(0.0f, 0.0f, 0.0f);
			
			const int extent = 32;
			float beg = -extent / 16.0f, end = extent / 16.0f;
			
			for (int i = -extent; i <= extent; i++) {
				float cur = i / 16.0f;
				
				// Y plane gridlines
				if (showYGrid) {
					GL.Vertex3(cur, 0, beg); GL.Vertex3(cur, 0, end);
					GL.Vertex3(beg, 0, cur); GL.Vertex3(end, 0, cur);
				}

				// X plane gridlines
				if (showXGrid) {
					GL.Vertex3(cur, 0, 0); GL.Vertex3(cur, end, 0);
					if (cur >= 0) { GL.Vertex3(beg, cur, 0); GL.Vertex3(end, cur, 0); }
				}
				
				// Z plane gridlines
				if (showZGrid) {
					if (cur >= 0) { GL.Vertex3(0, cur, beg); GL.Vertex3(0, cur, end); }
					GL.Vertex3(0, 0, cur); GL.Vertex3(0, end, cur);
				}
			}
			
			GL.End();
		}
		
		void PaintAxisLines() {
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
				Import(fs);
			}
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
			texId = Gfx.CreateTexture(path, out IModel._64x64);
			p.Model.texId = texId;
		}
		
		void BtnGridTexClick(object sender, EventArgs e) {
			string path = DoOpenFileDialog("Accepted File Types (*.png)|*.png|Images (*.png)|*.png");
			if (path == null) return;

			Gfx.DeleteTexture(ref floorTexId);
			bool ignored;
			floorTexId = Gfx.CreateTexture(path, out ignored);
		}
		
		void BtnXGridClick(object sender, EventArgs e) {
			showXGrid = !showXGrid;
			btnXGrid.CheckState = showXGrid ? CheckState.Checked : CheckState.Unchecked;
		}
		
		void BtnYGridClick(object sender, System.EventArgs e) {
			showYGrid = !showYGrid;
			btnYGrid.CheckState = showYGrid ? CheckState.Checked : CheckState.Unchecked;
		}
		
		void BtnZGridClick(object sender, System.EventArgs e) {
			showZGrid = !showZGrid;
			btnZGrid.CheckState = showZGrid ? CheckState.Checked : CheckState.Unchecked;
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
		
		void CbAxisLinesCheckedChanged(object sender, System.EventArgs e) {
			if (cur != null) cur.AxisLines = cbAxisLines.Checked;
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
			cbAxisLines.Checked = p.AxisLines;
			
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
