/* Copyright (c) 2019 Rick (rick 'at' gibbed 'dot' us)
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

using SAM.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using APITypes = SAM.API.Types;

namespace SAM.Game
{
    internal partial class Manager : Form
    {
        private readonly long _GameId;
        private readonly API.Client _SteamClient;

        private readonly WebClient _IconDownloader = new WebClient();

        private readonly List<Stats.AchievementInfo> _IconQueue = new List<Stats.AchievementInfo>();
        private readonly List<Stats.StatDefinition> _StatDefinitions = new List<Stats.StatDefinition>();

        private readonly List<Stats.AchievementDefinition> _AchievementDefinitions =
            new List<Stats.AchievementDefinition>();

        private readonly BindingList<Stats.StatInfo> _Statistics = new BindingList<Stats.StatInfo>();

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        private readonly API.Callbacks.UserStatsReceived _UserStatsReceivedCallback;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable
        private string logoDirLocal;
        //private API.Callback<APITypes.UserStatsStored> UserStatsStoredCallback;

        public Manager(long gameId, API.Client client)
        {
            logoDirLocal = string.Format(
               CultureInfo.InvariantCulture,
               "{0}/logocache/{1}",
               Path.GetDirectoryName(Application.ExecutablePath),
               gameId);
            System.IO.Directory.CreateDirectory(logoDirLocal);

            this.InitializeComponent();

            this._MainTabControl.SelectedTab = this._AchievementsTabPage;
            //this.statisticsList.Enabled = this.checkBox1.Checked;

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

            this._StatisticsDataGridView.DataSource = new BindingSource
            {
                DataSource = this._Statistics,
            };

            this._GameId = gameId;
            this._SteamClient = client;

            this._IconDownloader.DownloadDataCompleted += this.OnIconDownload;

            string name = this._SteamClient.SteamApps001.GetAppData((uint)this._GameId, "name");
            if (name != null)
            {
                gameName = name;
                base.Text += " | " + name;
            }
            else
            {
                gameName = this._GameId.ToString(CultureInfo.InvariantCulture);
                base.Text += " | " + gameName;
            }

            this._UserStatsReceivedCallback = client.CreateAndRegisterCallback<API.Callbacks.UserStatsReceived>();
            this._UserStatsReceivedCallback.OnRun += this.OnUserStatsReceived;

            //this.UserStatsStoredCallback = new API.Callback(1102, new API.Callback.CallbackFunction(this.OnUserStatsStored));
            this.RefreshStats();

            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
        }

        private readonly long _Auto;

        public Manager(long gameId,long auto ,API.Client client)
        {
            logoDirLocal = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/logocache/{1}",
                Path.GetDirectoryName(Application.ExecutablePath),
                gameId);
            System.IO.Directory.CreateDirectory(logoDirLocal);

            this.InitializeComponent();

            this._MainTabControl.SelectedTab = this._AchievementsTabPage;
            //this.statisticsList.Enabled = this.checkBox1.Checked;

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

            this._StatisticsDataGridView.DataSource = new BindingSource
            {
                DataSource = this._Statistics,
            };

            this._GameId = gameId;
            this._Auto = auto;
            this._SteamClient = client;

            this._IconDownloader.DownloadDataCompleted += this.OnIconDownload;

            string name = this._SteamClient.SteamApps001.GetAppData((uint)this._GameId, "name");
            if (name != null)
            {
                gameName = name;
                base.Text += " | " + name;
            }
            else
            {
                gameName = this._GameId.ToString(CultureInfo.InvariantCulture);
                base.Text += " | " + gameName;
            }

            this._UserStatsReceivedCallback = client.CreateAndRegisterCallback<API.Callbacks.UserStatsReceived>();
            this._UserStatsReceivedCallback.OnRun += this.OnUserStatsReceived;

            //this.UserStatsStoredCallback = new API.Callback(1102, new API.Callback.CallbackFunction(this.OnUserStatsStored));
            this.RefreshStats();

            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
        }
        string gameName;
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
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
                var info = e.UserState as Stats.AchievementInfo;

                var logoPathLocal = logoDirLocal + "/" + (info.IsAchieved == true ? info.IconNormal : info.IconLocked);

                try
                {
                    using (var stream = File.OpenWrite(logoPathLocal))
                    {
                        stream.Write(e.Result, 0, e.Result.Length);
                    }
                    LoadAchievementIconLocally(info);
                }
                catch (Exception)
                {
                }

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

            this._DownloadStatusLabel.Text = string.Format(
                CultureInfo.CurrentCulture,
                "下载 {0} 图标...",
                this._IconQueue.Count);
            this._DownloadStatusLabel.Visible = true;

            var info = this._IconQueue[0];
            this._IconQueue.RemoveAt(0);


            this._IconDownloader.DownloadDataAsync(
                new Uri(string.Format(
                    CultureInfo.InvariantCulture,
                    "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{0}/{1}",
                    this._GameId,
                    info.IsAchieved == true ? info.IconNormal : info.IconLocked)),
                info);
        }

        private static string TranslateError(int id)
        {
            switch (id)
            {
                case 2:
                {
                    return "generic error -- this usually means you don't own the game";
                }
            }

            return id.ToString(CultureInfo.InvariantCulture);
        }

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
                path = API.Steam.GetInstallPath();
                path = Path.Combine(path, "appcache");
                path = Path.Combine(path, "stats");
                path = Path.Combine(path, string.Format(
                    CultureInfo.InvariantCulture,
                    "UserGameStatsSchema_{0}.bin",
                    this._GameId));

                if (File.Exists(path) == false)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            var kv = KeyValue.LoadAsBinary(path);

            if (kv == null)
            {
                return false;
            }

            var currentLanguage = this._SteamClient.SteamApps008.GetCurrentGameLanguage();
            //var currentLanguage = "german";

            this._AchievementDefinitions.Clear();
            this._StatDefinitions.Clear();

            var stats = kv[this._GameId.ToString(CultureInfo.InvariantCulture)]["stats"];
            if (stats.Valid == false ||
                stats.Children == null)
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

                                    this._AchievementDefinitions.Add(new Stats.AchievementDefinition()
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
                this._GameStatusLabel.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    "检索统计信息时出错：{0}",
                    TranslateError(param.Result));
                this.EnableInput();
                return;
            }

            if (this.LoadUserGameStatsSchema() == false)
            {
                this._GameStatusLabel.Text = "未能加载架构。";
                this.EnableInput();
                return;
            }

            try
            {
                this.GetAchievements();
                this.GetStatistics();
            }
            catch (Exception e)
            {
                this._GameStatusLabel.Text = "处理统计信息检索时出错。";
                this.EnableInput();
                MessageBox.Show(
                    "Error when handling stats retrieval:\n" + e,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            this._GameStatusLabel.Text = string.Format(
                CultureInfo.CurrentCulture,
                "已检索到{0}项成就和{1}项统计信息。",
                this._AchievementListView.Items.Count,
                this._StatisticsDataGridView.Rows.Count);


            if(this._Auto == 1)
            {
                if(this._AchievementListView.Items.Count > 0)
                {
                    OnStore(_AutoStoreButton, new EventArgs());
                }
            }

            InitConfig();
            this.EnableInput();
        }
        string access_token,key; string appToken; List<string> uids;

        void InitConfig()
        {
            if (File.Exists(iniFilePath))
            {
                access_token = ReadIniValue("General", "Access_Token");
                key = ReadIniValue("General", "Key");
                appToken = ReadIniValue("General", "AppToken");
                uids = ReadIniValue("General", "UIDs")
                   .Split(',')
                   .Select(uid => uid.Trim())
                   .ToList();
            }
            else
            {
                CreateIniFile();
            }
        }
        private void RefreshStats()
        {
            this._AchievementListView.Items.Clear();
            this._StatisticsDataGridView.Rows.Clear();

            if (this._SteamClient.SteamUserStats.RequestCurrentStats() == false)
            {
                MessageBox.Show(this, "Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this._GameStatusLabel.Text = "正在检索统计信息。。。";
            this.DisableInput();
        }

        private bool _IsUpdatingAchievementList;

        private void GetAchievements()
        {
            this._IsUpdatingAchievementList = true;

            this._AchievementListView.Items.Clear();
            this._AchievementListView.BeginUpdate();
            InitConfig();
            string url = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?access_token="+access_token+"&key="+key+"&l=schinese&appid=" + this._GameId;
            this._GameStatusLabel.Text = "获取全球成就排行榜中";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            RootObject rootObject = null ;
            List<GetGlobalAchievementPercentagesForAppAchievement> achievements = null;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (Stream dataStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(dataStream);
                    string responseText = reader.ReadToEnd();

                    MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(responseText));
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(RootObject));
                    rootObject  = (RootObject)serializer.ReadObject(memoryStream);

                }
                response.Close();
            }
            catch (Exception ex)
            {
                this._GameStatusLabel.Text = "发生异常：" + ex.Message;
            }

            if(rootObject!= null)
            {
                url = "https://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/?gameid=" + this._GameId;
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    using (Stream dataStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(dataStream);
                        string responseText = reader.ReadToEnd();

                        MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(responseText));
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GetGlobalAchievementPercentagesForAppRoot));

                        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseText)))
                        {
                            GetGlobalAchievementPercentagesForAppRoot result = (GetGlobalAchievementPercentagesForAppRoot)serializer.ReadObject(stream);

                            GetGlobalAchievementPercentagesForAppAchievementPercentages achievementPercentages = result.AchievementPercentages;
                            achievements = achievementPercentages.Achievements;

                            foreach (GetGlobalAchievementPercentagesForAppAchievement achievement in achievements)
                            {
                                foreach (var item in rootObject.Game.AvailableGameStats.Achievements)
                                {
                                    if(item.Name == achievement.Name)
                                    {
                                        achievement.Name = item.DisplayName;
                                        break;
                                    }
                                }
                            }
                        }

                    }
                    response.Close();
                }
                catch (Exception ex)
                {
                    this._GameStatusLabel.Text = "发生异常：" + ex.Message;
                }

            }


            this._GameStatusLabel.Text = "获取完成";

            int index = 0;
            foreach (var def in this._AchievementDefinitions)
            {
                if (string.IsNullOrEmpty(def.Id) == true)
                {
                    continue;
                }

                bool isAchieved;
                if (this._SteamClient.SteamUserStats.GetAchievementState(def.Id, out isAchieved) == false)
                {
                    continue;
                }

                if (!this.IsMatchingSearchAndDisplaySettings(isAchieved, def.Name, def.Description))
                {
                    continue;
                }
                double sort = 0;
                foreach (GetGlobalAchievementPercentagesForAppAchievement achievement in achievements)
                {
                    if (def.Name == achievement.Name)
                    {
                        sort = achievement.Percent;
                        break;
                    }
                }

                var info = new Stats.AchievementInfo()
                {
                    Id = def.Id,
                    IsAchieved = isAchieved,
                    IconNormal = string.IsNullOrEmpty(def.IconNormal) ? null : def.IconNormal,
                    IconLocked = string.IsNullOrEmpty(def.IconLocked) ? def.IconNormal : def.IconLocked,
                    Permission = def.Permission,
                    Name = def.Name,
                    Description = def.Description,
                    SortIndex = (float)sort,
                };

                var item = new ListViewItem()
                {
                    Checked = isAchieved,
                    Tag = info,
                    Text = info.Name,
                    BackColor = (def.Permission & 3) == 0 ? Color.Black : Color.FromArgb(64, 0, 0),
                };

                info.Item = item;

                item.SubItems.Add(info.Description);
                item.SubItems.Add(info.SortIndex.ToString());

                info.ImageIndex = 0;

                this.AddAchievementToIconQueue(info, false);
                this._AchievementListView.Items.Add(item);
                //this.Achievements.Add(info.Id, info);
            }
            this._AchievementListView.EndUpdate();
            this._IsUpdatingAchievementList = false;

            this.DownloadNextIcon();
        }

        private void GetStatistics()
        {
            this._Statistics.Clear();
            foreach (var rdef in this._StatDefinitions)
            {
                if (string.IsNullOrEmpty(rdef.Id) == true)
                {
                    continue;
                }

                if (rdef is Stats.IntegerStatDefinition)
                {
                    var def = (Stats.IntegerStatDefinition)rdef;

                    int value;
                    if (this._SteamClient.SteamUserStats.GetStatValue(def.Id, out value))
                    {
                        this._Statistics.Add(new Stats.IntStatInfo()
                        {
                            Id = def.Id,
                            DisplayName = def.DisplayName,
                            IntValue = value,
                            OriginalValue = value,
                            IsIncrementOnly = def.IncrementOnly,
                            Permission = def.Permission,
                        });
                    }
                }
                else if (rdef is Stats.FloatStatDefinition)
                {
                    var def = (Stats.FloatStatDefinition)rdef;

                    float value;
                    if (this._SteamClient.SteamUserStats.GetStatValue(def.Id, out value))
                    {
                        this._Statistics.Add(new Stats.FloatStatInfo()
                        {
                            Id = def.Id,
                            DisplayName = def.DisplayName,
                            FloatValue = value,
                            OriginalValue = value,
                            IsIncrementOnly = def.IncrementOnly,
                            Permission = def.Permission,
                        });
                    }
                }
            }
        }
        private bool LoadAchievementIconLocally(Stats.AchievementInfo info)
        {
            var logoPathLocal = logoDirLocal + "/" + (info.IsAchieved == true ? info.IconNormal : info.IconLocked);

            if (File.Exists(logoPathLocal))
            {
                var stream = File.OpenRead(logoPathLocal);
                Bitmap bitmap = new Bitmap(stream);

                this.AddAchievementIcon(info, bitmap);
                this._AchievementListView.Update();

                return true;
            }

            return false;
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
                if (LoadAchievementIconLocally(info)) return;

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

            var achievements = new List<Stats.AchievementInfo>();
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                var achievementInfo = item.Tag as Stats.AchievementInfo;
                if (achievementInfo != null &&
                    achievementInfo.IsAchieved != item.Checked)
                {
                    achievementInfo.IsAchieved = item.Checked;
                    achievements.Add(item.Tag as Stats.AchievementInfo);
                }
            }

            if (achievements.Count == 0)
            {
                return 0;
            }

            foreach (Stats.AchievementInfo info in achievements)
            {
                if (this._SteamClient.SteamUserStats.SetAchievement(info.Id, info.IsAchieved) == false)
                {
                    MessageBox.Show(
                        this,
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "An error occurred while setting the state for {0}, aborting store.",
                            info.Id),
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

            foreach (Stats.StatInfo stat in statistics)
            {
                if (stat is Stats.IntStatInfo)
                {
                    var intStat = (Stats.IntStatInfo)stat;
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        intStat.Id,
                        intStat.IntValue) == false)
                    {
                        MessageBox.Show(
                            this,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "An error occurred while setting the value for {0}, aborting store.",
                                stat.Id),
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return -1;
                    }
                }
                else if (stat is Stats.FloatStatInfo)
                {
                    var floatStat = (Stats.FloatStatInfo)stat;
                    if (this._SteamClient.SteamUserStats.SetStatValue(
                        floatStat.Id,
                        floatStat.FloatValue) == false)
                    {
                        MessageBox.Show(
                            this,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "An error occurred while setting the value for {0}, aborting store.",
                                stat.Id),
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
            this._AutoStoreButton.Enabled = false;
            this._StoreButton.Enabled = false;
        }

        private void EnableInput()
        {
            this._ReloadButton.Enabled = true;
            this._AutoStoreButton.Enabled = true;
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
        Random random = new Random();

        bool isAuto = false;
        private void OnStore(object sender, EventArgs e)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
           
            int count = 0;
            foreach (ListViewItem item in this._AchievementListView.Items)
            {
                if (!item.Checked)
                {
                    count++;
                }
            }
            //MessageBox.Show(
            //    this,
            //    "总共有" + count,
            //    "Information",
            //    MessageBoxButtons.OK,
            //    MessageBoxIcon.Information);
            isAuto = true;
            // 启动BackgroundWorker执行任务
            backgroundWorker.RunWorkerAsync(count);
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int count = (int)e.Argument;

            for (int i = 0; i < count; i++)
            {
                int randomWaitTimeMs = random.Next(1, 31) * 60000;
                DateTime startTime = DateTime.Now;
                while ((DateTime.Now - startTime).TotalMilliseconds < randomWaitTimeMs)
                {
                    TimeSpan remainingTime = TimeSpan.FromMilliseconds(randomWaitTimeMs - (DateTime.Now - startTime).TotalMilliseconds);
                    this._GameStatusLabel.Text = "进度（" + (i + 1) + "/" + count + "） 下一个解锁时间 " + (int)remainingTime.TotalMinutes + " 分钟 " + remainingTime.Seconds + " 秒";
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(1000);
                }


                foreach (ListViewItem item in this._AchievementListView.Items)
                {
                    if (!item.Checked)
                    {
                        item.Checked = true;
                        string url = "https://wxpusher.zjiecode.com/api/send/message";
                        PushData pushData = new PushData
                        {
                            appToken = appToken,  //输入token
                            uids = uids, //用户ID
                            topicIds = new List<object>(),
                            summary = "解锁（" + (i + 1) + "/" + count + "）",
                            content = "<p><span style=\"color:#000000\"><span style=\"background-color:#ffffff\"><strong><span style=\"font-size:30px\">🎮" + gameName + "</span></strong><span style=\"font-size:16px\">的成就</span><strong><span style=\"font-size:30px\">" + item.Text + "</span></strong><span style=\"font-size:16px\"> 解锁成功<u><em>（" + (i + 1) + " / " + count + "）</em></u></span></span></span></p>",
                            contentType = 2,
                            verifyPay = false
                        };
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PushData));
                        var request = (HttpWebRequest)WebRequest.Create(url);
                        request.Method = "POST";
                        request.ContentType = "application/json";

                        try
                        {
                            string jsonData = "";
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                serializer.WriteObject(memoryStream, pushData);
                                jsonData = Encoding.UTF8.GetString(memoryStream.ToArray());
                                Console.WriteLine(jsonData);
                            }

                            using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                            {
                                streamWriter.Write(jsonData);
                                streamWriter.Flush();
                                streamWriter.Close();
                            }

                            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                            {
                                if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                                    {
                                        string responseText = reader.ReadToEnd();
                                        this._GameStatusLabel.Text = "响应内容：" + responseText;
                                    }
                                }
                                else
                                {
                                    this._GameStatusLabel.Text = "HTTP请求失败，状态码：" + response.StatusCode;
                                }
                            }
                        }
                        catch (WebException webEx)
                        {
                            this._GameStatusLabel.Text = "请求异常：" + webEx.Message;
                        }
                        break;
                    }
                }
                System.Threading.Thread.Sleep(100);
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

                if (i == count)
                {
                    break;
                }

            }
            this.Close();
            this.RefreshStats();
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // 后台任务完成后的处理
        }

        private void OnStatDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Context == DataGridViewDataErrorContexts.Commit)
            {
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
        }

        private void OnStatAgreementChecked(object sender, EventArgs e)
        {
            this._StatisticsDataGridView.Columns[1].ReadOnly = this._EnableStatsEditingCheckBox.Checked == false;
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

            var info = this._AchievementListView.Items[e.Index].Tag
                       as Stats.AchievementInfo;
            if (info == null)
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

        private bool IsMatchingSearchAndDisplaySettings(bool isLocked, string achievementName, string achievementDesc)
        {
            // display locked, unlocked or both
            bool lockStateMatch = (!_DisplayLockedOnlyButton.Checked && !_DisplayUnlockedOnlyButton.Checked) ||
                                (_DisplayLockedOnlyButton.Checked && isLocked) ||
                                (_DisplayUnlockedOnlyButton.Checked && !isLocked);
            // text filter on name / description
            bool findTxtMatch = true;
            if (lockStateMatch)
            {
                string searchString = _MatchingStringTextBox.Text.ToLowerInvariant();
                findTxtMatch = String.IsNullOrEmpty(searchString) || achievementName.ToLowerInvariant().Contains(searchString) || achievementDesc.ToLowerInvariant().Contains(searchString);
            }
            return lockStateMatch && findTxtMatch;
        }

        private void _DisplayUncheckedOnlyButton_Click(object sender, EventArgs e)
        {
            if ((sender as ToolStripButton).Checked)
            {
                _DisplayLockedOnlyButton.Checked = false;
                _DisplayUnlockedOnlyButton.ForeColor = Color.Blue;
                _DisplayLockedOnlyButton.ForeColor = Color.Black;
            }
            else
            {
                _DisplayUnlockedOnlyButton.ForeColor = Color.Black;
            }
            this.GetAchievements();
        }

        private void _DisplayCheckedOnlyButton_Click(object sender, EventArgs e)
        {
            if ((sender as ToolStripButton).Checked)
            {
                _DisplayUnlockedOnlyButton.Checked = false;
                _DisplayLockedOnlyButton.ForeColor = Color.Blue;
                _DisplayUnlockedOnlyButton.ForeColor = Color.Black;
            }
            else
            {
                _DisplayLockedOnlyButton.ForeColor = Color.Black;
            }
            this.GetAchievements();
        }

        private void OnFilterUpdate(object sender, KeyEventArgs e)
        {
            this.GetAchievements();
        }

        private void _StoreButton_Click(object sender, EventArgs e)
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
                string.Format(
                    CultureInfo.CurrentCulture,
                    "解锁了 {0} 个成就和 {1} 个统计数据。",
                    achievements,
                    stats),
                "Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            this.RefreshStats();
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(gameName);
            this.Close();
        }

        private void Manager_FormClosing(object sender, FormClosingEventArgs e)
        {
            string directoryPath = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(directoryPath, "data", this._GameId.ToString());
            if (isAuto)
            {
                if (!Directory.Exists(Path.Combine(directoryPath, "data")))
                {
                    Directory.CreateDirectory(Path.Combine(directoryPath, "data"));
                }
                bool isDone = true;
                foreach (ListViewItem item in this._AchievementListView.Items)
                {
                    if (!item.Checked)
                    {
                        isDone = false;
                        break;
                    }
                }
                if (!isDone)
                {
                    File.Create(filePath).Close();
                }
                else
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
        }

        static string iniFilePath = "config.ini";
        static string ReadIniValue(string section, string key)
        {
            if (!File.Exists(iniFilePath))
            {
                throw new FileNotFoundException("INI 文件不存在。");
            }

            string value = "";

            using (StreamReader reader = new StreamReader(iniFilePath))
            {
                string line;
                bool sectionFound = false;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        string currentSection = line.Substring(1, line.Length - 2);

                        if (string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
                        {
                            sectionFound = true;
                        }
                        else
                        {
                            sectionFound = false;
                        }
                    }
                    else if (sectionFound && line.Contains("="))
                    {
                        string[] parts = line.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length == 2)
                        {
                            string currentKey = parts[0].Trim();
                            string currentValue = parts[1].Trim();

                            if (string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
                            {
                                value = currentValue;
                                break;
                            }
                        }
                    }
                }
            }

            return value;
        }

        static void CreateIniFile()
        {
            using (StreamWriter writer = new StreamWriter(iniFilePath))
            {
                writer.WriteLine("[General]");
                writer.WriteLine("AppToken=");
                writer.WriteLine("UIDs=");
                writer.WriteLine("Access_Token=");
                writer.WriteLine("Key=");
            }

        }

        private void Manager_Load(object sender, EventArgs e)
        {
            this._AchievementListView.ListViewItemSorter = new ListViewColumnSorter();
            this._AchievementListView.ColumnClick += new ColumnClickEventHandler(ListViewHelper.ListView_ColumnClick);
        }
    }
}
[DataContract]
public class Achievement
{
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "defaultvalue")]
    public int DefaultValue { get; set; }

    [DataMember(Name = "displayName")]
    public string DisplayName { get; set; }

    [DataMember(Name = "hidden")]
    public int Hidden { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "icon")]
    public string Icon { get; set; }

    [DataMember(Name = "icongray")]
    public string IconGray { get; set; }
}

[DataContract]
public class AvailableGameStats
{
    [DataMember(Name = "achievements")]
    public List<Achievement> Achievements { get; set; }
}

[DataContract]
public class Game
{
    [DataMember(Name = "gameName")]
    public string GameName { get; set; }

    [DataMember(Name = "gameVersion")]
    public string GameVersion { get; set; }

    [DataMember(Name = "availableGameStats")]
    public AvailableGameStats AvailableGameStats { get; set; }
}

[DataContract]
public class RootObject
{
    [DataMember(Name = "game")]
    public Game Game { get; set; }
}

[DataContract]
public class GetGlobalAchievementPercentagesForAppAchievement
{
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "percent")]
    public double Percent { get; set; }
}

[DataContract]
public class GetGlobalAchievementPercentagesForAppAchievementPercentages
{
    [DataMember(Name = "achievements")]
    public List<GetGlobalAchievementPercentagesForAppAchievement> Achievements { get; set; }
}

[DataContract]
public class GetGlobalAchievementPercentagesForAppRoot
{
    [DataMember(Name = "achievementpercentages")]
    public GetGlobalAchievementPercentagesForAppAchievementPercentages AchievementPercentages { get; set; }
}