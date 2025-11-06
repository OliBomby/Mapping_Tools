using System;
using System.Collections.Generic;
using System.Net.Http;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Views.Standard;

[DontShowTitle]
public partial class StandardView {
    public static readonly string ToolName = "Get started";

    public static readonly string ToolDescription = $@"";

    public StandardView() {
        InitializeComponent();

        SetRecentList();
        SetChangelogList();
    }

    public class ChangelogItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string Date { get; set; }
        public string Author { get; set; }
        public string Type { get; set; }
    }

    private async void SetChangelogList() {
        try {
            string responseString;
            using (HttpResponseMessage response = await MainWindow.HttpClient.GetAsync("https://api.github.com/repos/OliBomby/Mapping_Tools/releases")) {
                responseString = await response.Content.ReadAsStringAsync();
            }
            dynamic json = JsonConvert.DeserializeObject(responseString);

            if (json == null) return;

            foreach (dynamic dict in json) {
                ChangelogList.Items.Add(new ChangelogItem {
                    Id = dict["id"],
                    Title = dict["name"],
                    Text = dict["body"],
                    Date = dict["published_at"],
                    Author = dict["author"]["login"],
                });
            }
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
        }
    }

    public void SetRecentList() {
        if (SettingsManager.GetRecentMaps().Count > 0) {
            foreach (string[] s in SettingsManager.GetRecentMaps()) {
                // Populate listview in the component
                RecentList.Items.Add(new MyItem { Path = s[0], Date = s[1] });
            }
        }
    }

    public class MyItem {
        public string Path { get; set; }

        public string Date { get; set; }
    }

    private void RecentList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var selectedItems = RecentList.SelectedItems;
        List<string> items = new List<string>();
        foreach (var item in selectedItems) {
            items.Add(((MyItem)item).Path);
        }

        if (items.Count > 0)
            MainWindow.AppWindow.SetCurrentMaps(items.ToArray());
    }
}