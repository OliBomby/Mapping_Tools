using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows.Controls;

namespace Mapping_Tools.Views {
    public partial class StandardView :UserControl {
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
                var response = await MainWindow.HttpClient.PostAsync("https://mappingtools.seira.moe/changelog/logs/", content);
                var responseString = await response.Content.ReadAsStringAsync();
                dynamic json = JsonConvert.DeserializeObject(responseString);

                foreach (dynamic dict in json) {
                    ChangelogList.Items.Add(new ChangelogItem {
                        ID = dict["_id"],
                        Title = dict["title"],
                        Text = dict["text"],
                        Date = dict["date"],
                        Author = dict["author"],
                        Type = dict["type"] });
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
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
            var selectedItem = (MyItem)recentList.SelectedItem;
            if (selectedItem != null)
                MainWindow.AppWindow.SetCurrentMap(selectedItem.Path);
        }
    }
}
