using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SAM.Game.Stats;

namespace SAM.Game
{
    internal partial class SchedulerAddForm : Form
    {
        private readonly List<AchievementInfo> _Achievements;
        public ScheduledAchievement Result { get; private set; }

        public SchedulerAddForm(string gameName, uint gameId, List<AchievementInfo> achievements)
        {
            InitializeComponent();
            this.Text = "Add Schedule - " + gameName;
            this._Achievements = achievements;
            
            this.labelGame.Text = gameName + " (" + gameId + ")";
            
            this.comboAchievements.DisplayMember = "DisplayName";
            foreach (var ach in achievements.OrderBy(a => a.Name))
            {
                this.comboAchievements.Items.Add(ach);
            }

            if (this.comboAchievements.Items.Count > 0)
                this.comboAchievements.SelectedIndex = 0;

            this.dtpUnlockDate.Value = DateTime.Now;
            this.dtpUnlockTime.Value = DateTime.Now.AddMinutes(5);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (this.comboAchievements.SelectedItem is not AchievementInfo ach)
            {
                MessageBox.Show(this, "Please select an achievement.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var date = this.dtpUnlockDate.Value.Date;
            var time = this.dtpUnlockTime.Value.TimeOfDay;
            var unlockTime = date + time;

            if (unlockTime <= DateTime.Now)
            {
                 MessageBox.Show(this, "Please select a future time.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 return;
            }

            this.Result = new ScheduledAchievement
            {
                AchievementId = ach.Id,
                AchievementName = ach.Name,
                UnlockTime = unlockTime,
                IsUnlocked = false
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
