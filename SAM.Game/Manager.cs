/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Windows.Forms;
using static SAM.Game.InvariantShorthand;
using APITypes = SAM.API.Types;
using Microsoft.Win32;

using System.Threading.Tasks;
using System.Diagnostics;

namespace SAM.Game
{
    internal partial class Manager : Form
    {
        private readonly long _GameId;
        private readonly API.Client _SteamClient;

        private readonly WebClient _IconDownloader = new();

        private readonly List<Stats.AchievementInfo> _IconQueue = new();
        private readonly List<Stats.StatDefinition> _StatDefinitions = new();

        private readonly List<Stats.AchievementDefinition> _AchievementDefinitions = new();

        private readonly BindingList<Stats.StatInfo> _Statistics = new();
        private readonly System.ComponentModel.BindingList<ScheduledAchievement> _SchedulerList = new();

        private readonly API.Callbacks.UserStatsReceived _UserStatsReceivedCallback;


        private readonly System.Threading.ReaderWriterLockSlim _scheduledAchievementsLock = new();

        public Manager(long gameId, API.Client client)
        {
            this.InitializeComponent();
            this._ScheduleButton.Click += ScheduleButton_Click;

            this._importButton.Click += ImportScheduleButton_Click;
            this._exportButton.Click += ExportScheduleButton_Click;
            this._clearImportedButton.Click += ClearImportedButton_Click;
            this._jsonReportButton.Click += JsonReportButton_Click;
            
            
            
            this._AchievementListView.OwnerDraw = false;

            this._MainTabControl.SelectedTab = this._AchievementsTabPage;


            this._AchievementImageList.Images.Add("Blank", new Bitmap(64, 64));

            this._StatisticsDataGridView.AutoGenerateColumns = false;

            this._StatisticsDataGridView.Columns.Add("name", "Name");
            this._StatisticsDataGridView.Columns[0].ReadOnly = true;
            this._StatisticsDataGridView.Columns[0].Width = 200;
            this._StatisticsDataGridView.Columns[0].DataPropertyName = "DisplayName";

            this._StatisticsDataGridView.Columns.Add("value", "Value");
            this._StatisticsDataGridView.Columns[1].ReadOnly = this._EnableStatsEditingCheckBox.Checked == false;
            this._StatisticsDataGridView.Columns[1].Width = 90;
            this._StatisticsDataGridView.Columns[1].DataPropertyName = "Value";

            this._StatisticsDataGridView.Columns.Add("extra", "Extra");
            this._StatisticsDataGridView.Columns[2].ReadOnly = true;
            this._StatisticsDataGridView.Columns[2].Width = 200;
            this._StatisticsDataGridView.Columns[2].DataPropertyName = "Extra";

            this._StatisticsDataGridView.DataSource = new BindingSource()
            {
                DataSource = this._Statistics,
            };

            this._GameId = gameId;
            this._SteamClient = client;

            this._IconDownloader.DownloadDataCompleted += this.OnIconDownload;

            string name = this._SteamClient.SteamApps001.GetAppData((uint)this._GameId, "name");
            if (name != null)
            {
                base.Text += " | " + name;
            }
            else
            {
                base.Text += " | " + this._GameId.ToString(CultureInfo.InvariantCulture);
            }

            this._UserStatsReceivedCallback = client.CreateAndRegisterCallback<API.Callbacks.UserStatsReceived>();
            this._UserStatsReceivedCallback.OnRun += this.OnUserStatsReceived;





            this.RefreshStats();



            this._SchedulerDataGridView.DataSource = this._SchedulerList;
            this.LoadSchedules();
            this._SchedulerTimer.Enabled = true;



            this.Resize += this.OnManagerResize;



            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    string valueName = $"SAM_Game_{this._GameId}";
                    this._TrayRunOnStartupMenuItem.Checked = key.GetValue(valueName) != null;
                }
            }
            catch {}
        }

        private void AddAchievementIcon(Stats.AchievementInfo info, Image icon)
        {
            if (icon == null)
            {
                info.ImageIndex = 0;
            }
            else
            {
                info.ImageIndex = this._AchievementImageList.Images.Count;
                this._AchievementImageList.Images.Add(info.IsAchieved == true ? info.IconNormal : info.IconLocked, icon);
            }
        }

        private void OnIconDownload(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error == null && e.Cancelled == false)
            {
                var info = (Stats.AchievementInfo)e.UserState;

                Bitmap bitmap;
                try
                {
                    using (MemoryStream stream = new())
                    {
                        stream.Write(e.Result, 0, e.Result.Length);
                        bitmap = new(stream);
                    }
                }
                catch (Exception)
                {
                    bitmap = null;
                }

                this.AddAchievementIcon(info, bitmap);
                this._AchievementListView.Update();
            }

            this.DownloadNextIcon();
        }

        private void DownloadNextIcon()
        {
            if (this._IconQueue.Count == 0)
            {
                this._DownloadStatusLabel.Visible = false;
                return;
            }

            if (this._IconDownloader.IsBusy == true)
            {
                return;
            }

            this._DownloadStatusLabel.Text = $"Downloading {this._IconQueue.Count} icons...";
            this._DownloadStatusLabel.Visible = true;

            var info = this._IconQueue[0];
            this._IconQueue.RemoveAt(0);


            this._IconDownloader.DownloadDataAsync(
                new Uri(_($"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{this._GameId}/{(info.IsAchieved == true ? info.IconNormal : info.IconLocked)}")),
                info);
        }

        private static string TranslateError(int id) => id switch
        {
            2 => "generic error -- this usually means you don't own the game",
            _ => _($"{id}"),
        };

        private static string GetLocalizedString(KeyValue kv, string language, string defaultValue)
        {
            var name = kv[language].AsString("");
            if (string.IsNullOrEmpty(name) == false)
            {
                return name;
            }

            if (language != "english")
            {
                name = kv["english"].AsString("");
                if (string.IsNullOrEmpty(name) == false)
                {
                    return name;
                }
            }

            name = kv.AsString("");
            if (string.IsNullOrEmpty(name) == false)
            {
                return name;
            }

            return defaultValue;
        }

        private bool LoadUserGameStatsSchema()
        {
            string path;
            try
            {
                string fileName = _($"UserGameStatsSchema_{this._GameId}.bin");
                path = API.Steam.GetInstallPath();
                path = Path.Combine(path, "appcache", "stats", fileName);
                if (File.Exists(path) == false)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            var kv = KeyValue.LoadAsBinary(path);
            if (kv == null)
            {
                return false;
            }

            var currentLanguage = this._SteamClient.SteamApps008.GetCurrentGameLanguage();

            this._AchievementDefinitions.Clear();
            this._StatDefinitions.Clear();

            var stats = kv[this._GameId.ToString(CultureInfo.InvariantCulture)]["stats"];
            if (stats.Valid == false || stats.Children == null)
            {
                return false;
            }

            foreach (var stat in stats.Children)
            {
                if (stat.Valid == false)
                {
                    continue;
                }

                var rawType = stat["type_int"].Valid
                                  ? stat["type_int"].AsInteger(0)
                                  : stat["type"].AsInteger(0);
                var type = (APITypes.UserStatType)rawType;
                switch (type)
                {
                    case APITypes.UserStatType.Invalid:
                    {
                        break;
                    }

                    case APITypes.UserStatType.Integer:
                    {
                        var id = stat["name"].AsString("");
                        string name = GetLocalizedString(stat["display"]["name"], currentLanguage, id);

                        this._StatDefinitions.Add(new Stats.IntegerStatDefinition()
                        {
                            Id = stat["name"].AsString(""),
                            DisplayName = name,
                            MinValue = stat["min"].AsInteger(int.MinValue),
                            MaxValue = stat["max"].AsInteger(int.MaxValue),
                            MaxChange = stat["maxchange"].AsInteger(0),
                            IncrementOnly = stat["incrementonly"].AsBoolean(false),
                            SetByTrustedGameServer = stat["bSetByTrustedGS"].AsBoolean(false),
                            DefaultValue = stat["default"].AsInteger(0),
                            Permission = stat["permission"].AsInteger(0),
                        });
                        break;
                    }

                    case APITypes.UserStatType.Float:
                    case APITypes.UserStatType.AverageRate:
                    {
                        var id = stat["name"].AsString("");
                        string name = GetLocalizedString(stat["display"]["name"], currentLanguage, id);

                        this._StatDefinitions.Add(new Stats.FloatStatDefinition()
                        {
                            Id = stat["name"].AsString(""),
                            DisplayName = name,
                            MinValue = stat["min"].AsFloat(float.MinValue),
                            MaxValue = stat["max"].AsFloat(float.MaxValue),
                            MaxChange = stat["maxchange"].AsFloat(0.0f),
                            IncrementOnly = stat["incrementonly"].AsBoolean(false),
                            DefaultValue = stat["default"].AsFloat(0.0f),
                            Permission = stat["permission"].AsInteger(0),
                        });
                        break;
                    }

                    case APITypes.UserStatType.Achievements:
                    case APITypes.UserStatType.GroupAchievements:
                    {
                        if (stat.Children != null)
                        {
                            foreach (var bits in stat.Children.Where(
                                b => string.Compare(b.Name, "bits", StringComparison.InvariantCultureIgnoreCase) == 0))
                            {
                                if (bits.Valid == false ||
                                    bits.Children == null)
                                {
                                    continue;
                                }

                                foreach (var bit in bits.Children)
                                {
                                    string id = bit["name"].AsString("");
                                    string name = GetLocalizedString(bit["display"]["name"], currentLanguage, id);
                                    string desc = GetLocalizedString(bit["display"]["desc"], currentLanguage, "");

                                    this._AchievementDefinitions.Add(new()
                                    {
                                        Id = id,
                                        Name = name,
                                        Description = desc,
                                        IconNormal = bit["display"]["icon"].AsString(""),
                                        IconLocked = bit["display"]["icon_gray"].AsString(""),
                                        IsHidden = bit["display"]["hidden"].AsBoolean(false),
                                        Permission = bit["permission"].AsInteger(0),
                                    });
                                }
                            }
                        }

                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException("invalid stat type");
                    }
                }
            }

            return true;
        }

        private void OnUserStatsReceived(APITypes.UserStatsReceived param)
        {
            if (param.Result != 1)
            {
                this._GameStatusLabel.Text = $"Error while retrieving stats: {TranslateError(param.Result)}";
                this.EnableInput();
                return;
            }

            if (this.LoadUserGameStatsSchema() == false)
            {
                this._GameStatusLabel.Text = "Failed to load schema.";
                this.EnableInput();
                return;
            }

            try
            {
                this.GetAchievements();
            }
            catch (Exception e)
            {
                this._GameStatusLabel.Text = "Error when handling achievements retrieval.";
                this.EnableInput();
                MessageBox.Show(
                    "Error when handling achievements retrieval:\n" + e,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                this.GetStatistics();
            }
            catch (Exception e)
            {
                this._GameStatusLabel.Text = "Error when handling stats retrieval.";
                this.EnableInput();
                MessageBox.Show(
                    "Error when handling stats retrieval:\n" + e,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            this._GameStatusLabel.Text = $"Retrieved {this._AchievementListView.Items.Count} achievements and {this._StatisticsDataGridView.Rows.Count} statistics.";
            this.EnableInput();
        }

        private void RefreshStats()
        {
            this._AchievementListView.Items.Clear();
            this._StatisticsDataGridView.Rows.Clear();

            var steamId = this._SteamClient.SteamUser.GetSteamId();



            var callHandle = this._SteamClient.SteamUserStats.RequestUserStats(steamId);
            if (callHandle == API.CallHandle.Invalid)
            {
                MessageBox.Show(this, "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this._GameStatusLabel.Text = "Retrieving stat information...";
            this.DisableInput();
        }

        private bool _IsUpdatingAchievementList;

        private void GetAchievements()
        {
            var textSearch = this._MatchingStringTextBox.Text.Length > 0
                ? this._MatchingStringTextBox.Text
                : null;

            this._IsUpdatingAchievementList = true;

            this._AchievementListView.Items.Clear();
            this._AchievementListView.BeginUpdate();


            bool wantLocked = this._DisplayLockedOnlyButton.Checked == true;
            bool wantUnlocked = this._DisplayUnlockedOnlyButton.Checked == true;

            foreach (var def in this._AchievementDefinitions)
            {
                if (string.IsNullOrEmpty(def.Id) == true)
                {
                    continue;
                }

                if (this._SteamClient.SteamUserStats.GetAchievementAndUnlockTime(
                    def.Id,
                    out bool isAchieved,
                    out var unlockTime) == false)
                {
                    continue;
                }

                bool wanted = (wantLocked == false && wantUnlocked == false) || isAchieved switch
                {
                    true => wantUnlocked,
                    false => wantLocked,
                };
                if (wanted == false)
                {
                    continue;
                }

                if (textSearch != null)
                {
                    if (def.Name.IndexOf(textSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                        def.Description.IndexOf(textSearch, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                Stats.AchievementInfo info = new()
                {
                    Id = def.Id,
                    IsAchieved = isAchieved,
                    UnlockTime = isAchieved == true && unlockTime > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(unlockTime).LocalDateTime
                        : null,
                    IconNormal = string.IsNullOrEmpty(def.IconNormal) ? null : def.IconNormal,
                    IconLocked = string.IsNullOrEmpty(def.IconLocked) ? def.IconNormal : def.IconLocked,
                    Permission = def.Permission,
                    Name = def.Name,
                    Description = def.Description,
                };

                ListViewItem item = new()
                {
                    Checked = isAchieved,
                    Tag = info,
                    Text = info.Name,
                    BackColor = (def.Permission & 3) == 0 ? Color.Black : Color.FromArgb(64, 0, 0),
                };

                info.Item = item;

                if (item.Text.StartsWith("#", StringComparison.InvariantCulture) == true)
                {
                    item.Text = info.Id;
                    item.SubItems.Add("");
                }
                else
                {
                    item.SubItems.Add(info.Description);
                }

            
            var scheduled = this._SchedulerList.FirstOrDefault(s => s.AchievementId == def.Id && !s.IsUnlocked);
                string unlockTimeText = "";
                string statusText = "";

                if (isAchieved)
                {
                    statusText = "Unlocked";
                    if (unlockTime > 0)
                    {
                         unlockTimeText = DateTimeOffset.FromUnixTimeSeconds(unlockTime).LocalDateTime.ToString();
                    }
                }
                else
                {
                    if (scheduled != null)
                    {
                        statusText = "Scheduled";
                        unlockTimeText = scheduled.UnlockTime.ToString();
                    }
                    else
                    {
                        statusText = "Locked";
                    }
                }

                item.SubItems.Add(unlockTimeText);
                item.SubItems.Add(statusText);


                info.ImageIndex = 0;

                this.AddAchievementToIconQueue(info, false);
                this._AchievementListView.Items.Add(item);
            }

            this._AchievementListView.EndUpdate();
            this._IsUpdatingAchievementList = false;

            this.DownloadNextIcon();
        }

        private void GetStatistics()
        {
            this._Statistics.Clear();
            foreach (var stat in this._StatDefinitions)
            {
                if (string.IsNullOrEmpty(stat.Id) == true)
                {
                    continue;
                }

                if (stat is Stats.IntegerStatDefinition intStat)
                {
                    if (this._SteamClient.SteamUserStats.GetStatValue(intStat.Id, out int value) == false)
                    {
                        continue;
                    }
                    this._Statistics.Add(new Stats.IntStatInfo()
                    {
                        Id = intStat.Id,
                        DisplayName = intStat.DisplayName,
                        IntValue = value,
                        OriginalValue = value,
                        IsIncrementOnly = intStat.IncrementOnly,
                        Permission = intStat.Permission,
                    });
                }
                else if (stat is Stats.FloatStatDefinition floatStat)
                {
                    if (this._SteamClient.SteamUserStats.GetStatValue(floatStat.Id, out float value) == false)
                    {
                        continue;
                    }
                    this._Statistics.Add(new Stats.FloatStatInfo()
                    {
                        Id = floatStat.Id,
                        DisplayName = floatStat.DisplayName,
                        FloatValue = value,
                        OriginalValue = value,
                        IsIncrementOnly = floatStat.IncrementOnly,
                        Permission = floatStat.Permission,
                    });
                }
            }
        }

        private void AddAchievementToIconQueue(Stats.AchievementInfo info, bool startDownload)
        {
            int imageIndex = this._AchievementImageList.Images.IndexOfKey(
                info.IsAchieved == true ? info.IconNormal : info.IconLocked);

            if (imageIndex >= 0)
            {
                info.ImageIndex = imageIndex;
            }
            else
            {
                this._IconQueue.Add(info);

                if (startDownload == true)
                {
                    this.DownloadNextIcon();
                }
            }
        }

        private int StoreAchievements()
        {
            if (this._AchievementListView.Items.Count == 0)
            {
                return 0;
            }

            List<Stats.AchievementInfo> achievements = new();
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                if (item.Tag is not Stats.AchievementInfo achievementInfo ||
                    achievementInfo.IsAchieved == item.Checked)
                {
                    continue;
                }

                achievementInfo.IsAchieved = item.Checked;
                achievements.Add(achievementInfo);
            }

            if (achievements.Count == 0)
            {
                return 0;
            }

            foreach (var info in achievements)
            {
                if (this._SteamClient.SteamUserStats.SetAchievement(info.Id, info.IsAchieved) == false)
                {
                    MessageBox.Show(
                        this,
                        $"An error occurred while setting the state for {info.Id}, aborting store.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return -1;
                }
            }

            return achievements.Count;
        }

        private int StoreStatistics()
        {
            if (this._Statistics.Count == 0)
            {
                return 0;
            }

            var statistics = this._Statistics.Where(stat => stat.IsModified == true).ToList();
            if (statistics.Count == 0)
            {
                return 0;
            }

            foreach (var stat in statistics)
            {
                if (stat is Stats.IntStatInfo intStat)
                {
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        intStat.Id,
                        intStat.IntValue) == false)
                    {
                        MessageBox.Show(
                            this,
                            $"An error occurred while setting the value for {stat.Id}, aborting store.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return -1;
                    }
                }
                else if (stat is Stats.FloatStatInfo floatStat)
                {
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        floatStat.Id,
                        floatStat.FloatValue) == false)
                    {
                        MessageBox.Show(
                            this,
                            $"An error occurred while setting the value for {stat.Id}, aborting store.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return -1;
                    }
                }
                else
                {
                    throw new InvalidOperationException("unsupported stat type");
                }
            }

            return statistics.Count;
        }

        private void DisableInput()
        {
            this._ReloadButton.Enabled = false;
            this._StoreButton.Enabled = false;
        }

        private void EnableInput()
        {
            this._ReloadButton.Enabled = true;
            this._StoreButton.Enabled = true;
        }

        private void OnTimer(object sender, EventArgs e)
        {
            this._CallbackTimer.Enabled = false;
            this._SteamClient.RunCallbacks(false);
            this._CallbackTimer.Enabled = true;
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            this.RefreshStats();
        }

        private void OnLockAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = false;
            }
        }

        private void OnInvertAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = !item.Checked;
            }
        }

        private void OnUnlockAll(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                item.Checked = true;
            }
        }

        private bool Store()
        {
            if (this._SteamClient.SteamUserStats.StoreStats() == false)
            {
                MessageBox.Show(
                    this,
                    "An error occurred while storing, aborting.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void OnStore(object sender, EventArgs e)
        {
            int achievements = this.StoreAchievements();
            if (achievements < 0)
            {
                this.RefreshStats();
                return;
            }

            int stats = this.StoreStatistics();
            if (stats < 0)
            {
                this.RefreshStats();
                return;
            }

            if (this.Store() == false)
            {
                this.RefreshStats();
                return;
            }

            MessageBox.Show(
                this,
                $"Stored {achievements} achievements and {stats} statistics.",
                "Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            this.RefreshStats();
        }

        private void OnStatDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Context != DataGridViewDataErrorContexts.Commit)
            {
                return;
            }

            var view = (DataGridView)sender;
            if (e.Exception is Stats.StatIsProtectedException)
            {
                e.ThrowException = false;
                e.Cancel = true;
                view.Rows[e.RowIndex].ErrorText = "Stat is protected! -- you can't modify it";
            }
            else
            {
                e.ThrowException = false;
                e.Cancel = true;
                view.Rows[e.RowIndex].ErrorText = "Invalid value";
            }
        }

        private void OnStatAgreementChecked(object sender, EventArgs e)
        {
            this._StatisticsDataGridView.Columns[1].ReadOnly = this._EnableStatsEditingCheckBox.Checked == false;
        }

        private void LoadSchedules()
        {
            var all = SchedulerStorage.Load();
            this._SchedulerList.Clear();
            foreach (var s in all.Where(x => x.GameId == this._GameId))
            {
                this._SchedulerList.Add(s);
            }
        }

        private void SaveSchedules()
        {
            var existing = SchedulerStorage.Load().Where(x => x.GameId != this._GameId).ToList();
            existing.AddRange(this._SchedulerList);
            SchedulerStorage.Save(existing);
        }

        private void OnAddSchedule(object sender, EventArgs e)
        {

            List<Stats.AchievementInfo> available = new List<Stats.AchievementInfo>();
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
               if (item.Tag is Stats.AchievementInfo info)
               {
                   available.Add(info);
               }
            }

            using (var dlg = new SchedulerAddForm(this.Text, (uint)this._GameId, available))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var result = dlg.Result;
                    result.GameId = (uint)this._GameId;
                    this._SchedulerList.Add(result);
                    this.SaveSchedules();
                }
            }
        }

        private void OnRemoveSchedule(object sender, EventArgs e)
        {
            if (this._SchedulerDataGridView.SelectedRows.Count == 0) return;
            foreach (DataGridViewRow row in this._SchedulerDataGridView.SelectedRows)
            {
                if (row.DataBoundItem is ScheduledAchievement item)
                {
                    this._SchedulerList.Remove(item);
                }
            }
            this.SaveSchedules();
        }

        private void OnClearSchedules(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all schedules for this game?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                this._SchedulerList.Clear();
                this.SaveSchedules();
            }
        }

        private void OnUnlockScheduleNow(object sender, EventArgs e)
        {
             if (this._SchedulerDataGridView.SelectedRows.Count == 0) return;
            foreach (DataGridViewRow row in this._SchedulerDataGridView.SelectedRows)
            {
                 if (row.DataBoundItem is ScheduledAchievement item && !item.IsUnlocked)
                 {
                     if (this._SteamClient.SteamUserStats.SetAchievement(item.AchievementId, true))
                     {
                         item.IsUnlocked = true;
                         item.UnlockTime = DateTime.Now; // Update timestamp
                     }
                 }
            }
            this._SteamClient.SteamUserStats.StoreStats();
            this._SchedulerDataGridView.Refresh();
            this.SaveSchedules();
            this.RefreshStats();
        }

         private void OnSchedulerTimer(object sender, EventArgs e)
        {
            bool anyUnlocked = false;
            var now = DateTime.Now;
            foreach (var item in this._SchedulerList)
            {
                if (!item.IsUnlocked && item.UnlockTime <= now)
                {
                     if (this._SteamClient.SteamUserStats.SetAchievement(item.AchievementId, true))
                     {
                         item.IsUnlocked = true;
                         anyUnlocked = true;

                         this._TrayIcon.Visible = true;
                         this._TrayIcon.ShowBalloonTip(3000, "Achievement Unlocked", $"{item.AchievementName} unlocked!", ToolTipIcon.Info);
                     }
                }
            }

            if (anyUnlocked)
            {
                this._SteamClient.SteamUserStats.StoreStats();
                this.SaveSchedules();
                this._SchedulerDataGridView.Refresh();
                this.RefreshStats();
            }
        }

        private void OnManagerResize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this._TrayIcon.Visible = true;
            }
        }

        private void OnTrayDoubleClick(object sender, EventArgs e)
        {
             this.Show();
             this.WindowState = FormWindowState.Normal;
             this._TrayIcon.Visible = false;
        }

        private void OnTrayOpen(object sender, EventArgs e)
        {
             this.Show();
             this.WindowState = FormWindowState.Normal;
             this._TrayIcon.Visible = false;
        }

        private void OnTrayExit(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OnStatCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var view = (DataGridView)sender;
            view.Rows[e.RowIndex].ErrorText = "";
        }

        private void OnResetAllStats(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Are you absolutely sure you want to reset stats?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.No)
            {
                return;
            }

            bool achievementsToo = DialogResult.Yes == MessageBox.Show(
                "Do you want to reset achievements too?",
                "Question",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (MessageBox.Show(
                "Really really sure?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error) == DialogResult.No)
            {
                return;
            }

            if (this._SteamClient.SteamUserStats.ResetAllStats(achievementsToo) == false)
            {
                MessageBox.Show(this, "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.RefreshStats();
        }

        private void OnCheckAchievement(object sender, ItemCheckEventArgs e)
        {
            if (sender != this._AchievementListView)
            {
                return;
            }

            if (this._IsUpdatingAchievementList == true)
            {
                return;
            }

            if (this._AchievementListView.Items[e.Index].Tag is not Stats.AchievementInfo info)
            {
                return;
            }

            if ((info.Permission & 3) != 0)
            {
                MessageBox.Show(
                    this,
                    "Sorry, but this is a protected achievement and cannot be managed with Steam Achievement Manager.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                e.NewValue = e.CurrentValue;
            }
        }

        private void OnDisplayUncheckedOnly(object sender, EventArgs e)
        {
            if ((sender as ToolStripButton).Checked == true)
            {
                this._DisplayLockedOnlyButton.Checked = false;
            }

            this.GetAchievements();
        }

        private void OnDisplayCheckedOnly(object sender, EventArgs e)
        {
            if ((sender as ToolStripButton).Checked == true)
            {
                this._DisplayUnlockedOnlyButton.Checked = false;
            }

            this.GetAchievements();
        }

        private void OnFilterUpdate(object sender, KeyEventArgs e)
        {
            this.GetAchievements();
        }
        


        private void OnTrayRunOnStartup(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    string valueName = $"SAM_Game_{this._GameId}";
                    if (this._TrayRunOnStartupMenuItem.Checked)
                    {
                        string path = Application.ExecutablePath;
                        string value = $"\"{path}\" {this._GameId}";
                        key.SetValue(valueName, value);
                    }
                    else
                    {
                        key.DeleteValue(valueName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to update registry: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this._TrayRunOnStartupMenuItem.Checked = !this._TrayRunOnStartupMenuItem.Checked;
            }
        }
        private void ScheduleButton_Click(object sender, EventArgs e) 
        { 
            MessageBox.Show("Agendador em desenvolvimento!"); 
        }

        internal void OnScheduleSelected(object sender, EventArgs e) { }
        internal void OnCheckAchievement(object sender, System.Windows.Forms.ItemCheckedEventArgs e) { }
        internal void OnDrawColumnHeader(object sender, System.Windows.Forms.DrawListViewColumnHeaderEventArgs e) 
        { 
            e.DrawDefault = true; 
        }

        internal void OnDrawItem(object sender, System.Windows.Forms.DrawListViewItemEventArgs e) 
        { 
            e.DrawDefault = true; 
        }

        internal void OnDrawSubItem(object sender, System.Windows.Forms.DrawListViewSubItemEventArgs e) 
        { 
            e.DrawDefault = true; 
        }

        private async void ExportScheduleButton_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "SAM Schedule Files (*.sam)|*.sam|JSON Files (*.json)|*.json",
                FileName = $"SAM-Schedule-{DateTime.Now:yyyyMMdd-HHmm}.sam",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                _scheduledAchievementsLock.EnterWriteLock();
                var exportList = _SchedulerList.Where(x => !x.IsUnlocked).ToList();

                var exportData = new
                {
                    Metadata = new { Version = "1.0", ExportedAt = DateTime.Now, Total = exportList.Count },
                    ScheduledAchievements = exportList
                };

                using (StreamWriter writer = File.CreateText(sfd.FileName))
                {
                    await writer.WriteAsync(JsonConvert.SerializeObject(exportData, new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }));
                }

                _TrayIcon.ShowBalloonTip(5000, "SAM Scheduler", $"✅ Exportado {exportList.Count} agendamentos para:\n{sfd.FileName}", ToolTipIcon.Info);
                _GameStatusLabel.Text = $"Exportado: {exportList.Count} itens → {Path.GetFileName(sfd.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro no export: {ex.Message}\n\nDetalhes: {ex}", "Export Falhou", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _scheduledAchievementsLock.ExitWriteLock(); }
        }

        private async void ImportScheduleButton_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "SAM Schedule Files (*.sam)|*.sam|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                string json = "";
                using (var reader = File.OpenText(ofd.FileName))
                {
                    json = await reader.ReadToEndAsync();
                }

                var token = JToken.Parse(json);

                List<ScheduledAchievement> imported = new List<ScheduledAchievement>();

                if (token is JObject obj)
                {
                    var property = obj.Properties().FirstOrDefault(p => string.Equals(p.Name, "ScheduledAchievements", StringComparison.OrdinalIgnoreCase) 
                                                                     || string.Equals(p.Name, "achievements", StringComparison.OrdinalIgnoreCase));

                    if (property != null)
                    {
                        imported = property.Value.ToObject<List<ScheduledAchievement>>();

                        if (imported.Any() && imported.All(x => x.UnlockTime == default))
                        {
                            var serializer = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
                            imported = property.Value.ToObject<List<ScheduledAchievement>>(serializer);
                        }
                    }
                }
                else if (token is JArray)
                {
                    imported = token.ToObject<List<ScheduledAchievement>>();

                    if (imported.Any() && imported.All(x => x.UnlockTime == default))
                    {
                        var serializer = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
                        imported = token.ToObject<List<ScheduledAchievement>>(serializer);
                    }
                }

                int merged = 0, skipped = 0;
                _scheduledAchievementsLock.EnterWriteLock();

                foreach (var item in imported.Where(x => !x.IsUnlocked))
                {

                    item.GameId = (uint)this._GameId;

                    if (!_SchedulerList.Any(x => x.AchievementId == item.AchievementId))
                    {
                        if (item.CreatedAt == default) item.CreatedAt = DateTime.Now;
                        _SchedulerList.Add(item);
                        merged++;
                    }
                    else skipped++;
                }
                
                _scheduledAchievementsLock.ExitWriteLock();
                
                SaveSchedules();
                
                _TrayIcon.ShowBalloonTip(5000, "SAM Scheduler",
                    $"✅ Importado: {merged} novos\n⚠️ Duplicados ignorados: {skipped}", ToolTipIcon.Info);

                _GameStatusLabel.Text = $"Importado: {merged} | Duplicados: {skipped}";
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"❌ Arquivo inválido (JSON corrompido):\n{ex.Message}", "Import Falhou", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro: {ex.Message}", "Import Falhou", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearImportedButton_Click(object sender, EventArgs e)
        {
             if (MessageBox.Show("Remover TODOS os agendamentos não-desbloqueados?\nIsso NÃO afeta conquistas já desbloqueadas.",
            "Confirmar Limpeza", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            _scheduledAchievementsLock.EnterWriteLock();
            
            var toRemove = _SchedulerList.Where(x => !x.IsUnlocked).ToList();
            foreach(var item in toRemove)
            {
                _SchedulerList.Remove(item);
            }
            int pendingCount = toRemove.Count;

            _scheduledAchievementsLock.ExitWriteLock();

            SaveSchedules();
            
            _GameStatusLabel.Text = $"🗑️ Removidos {pendingCount} agendamentos pendentes";
        }

        private void JsonReportButton_Click(object sender, EventArgs e)
        {
            string jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAM", "ScheduledAchievements.json");

            if (!File.Exists(jsonPath))
            {
                MessageBox.Show("Nenhum agendamento salvo ainda!", "Relatório", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{jsonPath}\"");

            if (MessageBox.Show("Abrir JSON no Bloco de Notas também?", "Relatório", MessageBoxButtons.YesNo) == DialogResult.Yes)
                Process.Start("notepad.exe", jsonPath);
        }
    }
}
