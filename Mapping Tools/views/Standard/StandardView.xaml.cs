using System;
using System.Collections.Generic;
using System.Net.Http;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Views.Standard {
    [DontShowTitle]
    public partial class StandardView {
        public static readonly string ToolName = "Get started";

        public static readonly string ToolDescription = $@"";

        private static readonly Dictionary<string, string> jankReplacements = new Dictionary<string, string>() {
            { @"<br/>", "\n" },
            { @"<br>", "\n" }
        };

        public StandardView() {
            InitializeComponent();

            SetRecentList();
            SetChangelogList();
        }

        public class ChangelogItem
        {
            public string ID { get; set; }
            public string Title { get; set; }
            public string Text { get; set; }
            public string Date { get; set; }
            public string Author { get; set; }
            public string Type { get; set; }
        }

        private async void SetChangelogList() {
            try {
                var values = new Dictionary<string, string>();
                var content = new FormUrlEncodedContent(values);
                var responseString = "";
                using (HttpResponseMessage response = await MainWindow.HttpClient.PostAsync("https://mappingtools.seira.moe/changelog/logs/", content)) {
                    responseString = await response.Content.ReadAsStringAsync();
                }
                dynamic json = JsonConvert.DeserializeObject(responseString);

                foreach (dynamic dict in json) {
                    ChangelogList.Items.Add(new ChangelogItem {
                        ID = dict["_id"],
                        Title = dict["title"],
                        Text = JankParse((string)dict["text"], jankReplacements),
                        Date = dict["date"],
                        Author = dict["author"],
                        Type = dict["type"]
                    });
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private static string JankParse(string text, Dictionary<string, string> replacements) {
            string result = text;
            foreach (KeyValuePair<string, string> kvp in replacements) {
                result = result.Replace(kvp.Key, kvp.Value);
            }
            return result;
        }

        public void SetRecentList() {
            if (SettingsManager.GetRecentMaps().Count > 0) {
                foreach (string[] s in SettingsManager.GetRecentMaps()) {
                    // Populate listview in the component
                    recentList.Items.Add(new MyItem { Path = s[0], Date = s[1] });
                }
            }
        }

        public class MyItem {
            public string Path { get; set; }

            public string Date { get; set; }
        }

        private void RecentList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var selectedItems = recentList.SelectedItems;
            List<string> items = new List<string>();
            foreach (var item in selectedItems) {
                items.Add(((MyItem)item).Path);
            }

            if (items.Count > 0)
                MainWindow.AppWindow.SetCurrentMaps(items.ToArray());
        }
    }
}
