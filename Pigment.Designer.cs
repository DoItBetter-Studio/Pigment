using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Glyphborn.Pigment
{
	partial class Pigment
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			ComponentResourceManager resources = new ComponentResourceManager(typeof(Pigment));
			SuspendLayout();
			// 
			// Pigment
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new Size(800, 450);
			Icon = (Icon)resources.GetObject("$this.Icon");
			StartPosition = FormStartPosition.CenterScreen;
			Name = "Pigment";
			Text = "Pigment";
			Load += Pigment_Load;
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
	}
}