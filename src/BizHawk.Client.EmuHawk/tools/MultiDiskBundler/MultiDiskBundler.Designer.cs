﻿namespace BizHawk.Client.EmuHawk
{
	partial class MultiDiskBundler
	{
		/// <summary>SystemLabel
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
            this.MultiDiskMenuStrip = new System.Windows.Forms.MenuStrip();
            this.SaveRunButton = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.grpName = new System.Windows.Forms.GroupBox();
            this.BrowseBtn = new System.Windows.Forms.Button();
            this.NameBox = new System.Windows.Forms.TextBox();
            this.FileSelectorPanel = new System.Windows.Forms.Panel();
            this.AddButton = new System.Windows.Forms.Button();
            this.SystemDropDown = new System.Windows.Forms.ComboBox();
            this.SystemLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btnRemove = new System.Windows.Forms.Button();
            this.SaveButton = new System.Windows.Forms.Button();
            this.grpName.SuspendLayout();
            this.SuspendLayout();
            // 
            // MultiDiskMenuStrip
            // 
            this.MultiDiskMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.MultiDiskMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MultiDiskMenuStrip.Name = "MultiDiskMenuStrip";
            this.MultiDiskMenuStrip.Size = new System.Drawing.Size(675, 24);
            this.MultiDiskMenuStrip.TabIndex = 0;
            this.MultiDiskMenuStrip.Text = "menuStrip1";
            // 
            // SaveRunButton
            // 
            this.SaveRunButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveRunButton.Enabled = false;
            this.SaveRunButton.Location = new System.Drawing.Point(424, 405);
            this.SaveRunButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.SaveRunButton.Name = "SaveRunButton";
            this.SaveRunButton.Size = new System.Drawing.Size(113, 28);
            this.SaveRunButton.TabIndex = 9;
            this.SaveRunButton.Text = "Save and &Run";
            this.SaveRunButton.UseVisualStyleBackColor = true;
            this.SaveRunButton.Click += new System.EventHandler(this.SaveRunButton_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(545, 405);
            this.CancelBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(113, 28);
            this.CancelBtn.TabIndex = 10;
            this.CancelBtn.Text = "&Close";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // grpName
            // 
            this.grpName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpName.Controls.Add(this.BrowseBtn);
            this.grpName.Controls.Add(this.NameBox);
            this.grpName.Location = new System.Drawing.Point(11, 34);
            this.grpName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.grpName.Name = "grpName";
            this.grpName.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.grpName.Size = new System.Drawing.Size(648, 55);
            this.grpName.TabIndex = 11;
            this.grpName.TabStop = false;
            this.grpName.Text = "Output Bundle Path";
            // 
            // BrowseBtn
            // 
            this.BrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BrowseBtn.Location = new System.Drawing.Point(556, 22);
            this.BrowseBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.BrowseBtn.Name = "BrowseBtn";
            this.BrowseBtn.Size = new System.Drawing.Size(84, 28);
            this.BrowseBtn.TabIndex = 14;
            this.BrowseBtn.Text = "Browse...";
            this.BrowseBtn.UseVisualStyleBackColor = true;
            this.BrowseBtn.Click += new System.EventHandler(this.BrowseBtn_Click);
            // 
            // NameBox
            // 
            this.NameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NameBox.Location = new System.Drawing.Point(8, 23);
            this.NameBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.NameBox.Name = "NameBox";
            this.NameBox.Size = new System.Drawing.Size(539, 22);
            this.NameBox.TabIndex = 0;
            this.NameBox.TextChanged += new System.EventHandler(this.NameBox_TextChanged);
            // 
            // FileSelectorPanel
            // 
            this.FileSelectorPanel.AllowDrop = true;
            this.FileSelectorPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FileSelectorPanel.AutoScroll = true;
            this.FileSelectorPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.FileSelectorPanel.Location = new System.Drawing.Point(11, 124);
            this.FileSelectorPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.FileSelectorPanel.Name = "FileSelectorPanel";
            this.FileSelectorPanel.Size = new System.Drawing.Size(647, 263);
            this.FileSelectorPanel.TabIndex = 12;
            this.FileSelectorPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDrop);
            this.FileSelectorPanel.DragEnter += new System.Windows.Forms.DragEventHandler(this.OnDragEnter);
            // 
            // AddButton
            // 
            this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AddButton.Location = new System.Drawing.Point(11, 405);
            this.AddButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(80, 28);
            this.AddButton.TabIndex = 13;
            this.AddButton.Text = "Add";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // SystemDropDown
            // 
            this.SystemDropDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SystemDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SystemDropDown.FormattingEnabled = true;
            this.SystemDropDown.Location = new System.Drawing.Point(540, 92);
            this.SystemDropDown.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.SystemDropDown.Name = "SystemDropDown";
            this.SystemDropDown.Size = new System.Drawing.Size(117, 24);
            this.SystemDropDown.TabIndex = 14;
            this.SystemDropDown.SelectedIndexChanged += new System.EventHandler(this.SystemDropDown_SelectedIndexChanged);
            // 
            // SystemLabel
            // 
            this.SystemLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SystemLabel.Location = new System.Drawing.Point(473, 96);
            this.SystemLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.SystemLabel.Name = "SystemLabel";
            this.SystemLabel.Text = "System:";
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRemove.Location = new System.Drawing.Point(99, 405);
            this.btnRemove.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(80, 28);
            this.btnRemove.TabIndex = 16;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // SaveButton
            // 
            this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveButton.Enabled = false;
            this.SaveButton.Location = new System.Drawing.Point(303, 405);
            this.SaveButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(113, 28);
            this.SaveButton.TabIndex = 18;
            this.SaveButton.Text = "&Save";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // MultiDiskBundler
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(675, 448);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.SystemLabel);
            this.Controls.Add(this.SystemDropDown);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.FileSelectorPanel);
            this.Controls.Add(this.grpName);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.SaveRunButton);
            this.Controls.Add(this.MultiDiskMenuStrip);
            this.MainMenuStrip = this.MultiDiskMenuStrip;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "MultiDiskBundler";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Load += new System.EventHandler(this.MultiDiskBundler_Load);
            this.grpName.ResumeLayout(false);
            this.grpName.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip MultiDiskMenuStrip;
		private System.Windows.Forms.Button SaveRunButton;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.GroupBox grpName;
		private System.Windows.Forms.TextBox NameBox;
		private System.Windows.Forms.Panel FileSelectorPanel;
		private System.Windows.Forms.Button AddButton;
		private System.Windows.Forms.Button BrowseBtn;
		private System.Windows.Forms.ComboBox SystemDropDown;
		private BizHawk.WinForms.Controls.LocLabelEx SystemLabel;
		private System.Windows.Forms.Button btnRemove;
		private System.Windows.Forms.Button SaveButton;
	}
}
