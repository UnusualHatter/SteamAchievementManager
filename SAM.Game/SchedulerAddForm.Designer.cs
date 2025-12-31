namespace SAM.Game
{
    partial class SchedulerAddForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelGame;
        private System.Windows.Forms.Label labelAchievement;
        private System.Windows.Forms.ComboBox comboAchievements;
        private System.Windows.Forms.Label labelTime;
        private System.Windows.Forms.DateTimePicker dtpUnlockDate;
        private System.Windows.Forms.DateTimePicker dtpUnlockTime;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.labelGame = new System.Windows.Forms.Label();
            this.labelAchievement = new System.Windows.Forms.Label();
            this.comboAchievements = new System.Windows.Forms.ComboBox();
            this.labelTime = new System.Windows.Forms.Label();
            this.dtpUnlockDate = new System.Windows.Forms.DateTimePicker();
            this.dtpUnlockTime = new System.Windows.Forms.DateTimePicker();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();

            this.labelGame.AutoSize = true;
            this.labelGame.Location = new System.Drawing.Point(12, 15);
            this.labelGame.Name = "labelGame";
            this.labelGame.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.labelGame.Size = new System.Drawing.Size(100, 15);
            this.labelGame.TabIndex = 0;
            this.labelGame.Text = "Game Name";

            this.labelAchievement.AutoSize = true;
            this.labelAchievement.Location = new System.Drawing.Point(12, 45);
            this.labelAchievement.Name = "labelAchievement";
            this.labelAchievement.Size = new System.Drawing.Size(75, 13);
            this.labelAchievement.TabIndex = 1;
            this.labelAchievement.Text = "Achievement:";

            this.comboAchievements.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAchievements.FormattingEnabled = true;
            this.comboAchievements.Location = new System.Drawing.Point(100, 42);
            this.comboAchievements.Name = "comboAchievements";
            this.comboAchievements.Size = new System.Drawing.Size(260, 21);
            this.comboAchievements.TabIndex = 2;

            this.labelTime.AutoSize = true;
            this.labelTime.Location = new System.Drawing.Point(12, 80);
            this.labelTime.Name = "labelTime";
            this.labelTime.Size = new System.Drawing.Size(70, 13);
            this.labelTime.TabIndex = 3;
            this.labelTime.Text = "Unlock Time:";

            this.dtpUnlockDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpUnlockDate.Location = new System.Drawing.Point(100, 77);
            this.dtpUnlockDate.Name = "dtpUnlockDate";
            this.dtpUnlockDate.Size = new System.Drawing.Size(120, 20);
            this.dtpUnlockDate.TabIndex = 4;

            this.dtpUnlockTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpUnlockTime.ShowUpDown = true;
            this.dtpUnlockTime.Location = new System.Drawing.Point(230, 77);
            this.dtpUnlockTime.Name = "dtpUnlockTime";
            this.dtpUnlockTime.Size = new System.Drawing.Size(130, 20);
            this.dtpUnlockTime.TabIndex = 5;

            this.btnOk.Location = new System.Drawing.Point(204, 120);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 6;
            this.btnOk.Text = "Schedule";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);

            this.btnCancel.Location = new System.Drawing.Point(285, 120);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 161);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.dtpUnlockTime);
            this.Controls.Add(this.dtpUnlockDate);
            this.Controls.Add(this.labelTime);
            this.Controls.Add(this.comboAchievements);
            this.Controls.Add(this.labelAchievement);
            this.Controls.Add(this.labelGame);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SchedulerAddForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Schedule Achievement";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
