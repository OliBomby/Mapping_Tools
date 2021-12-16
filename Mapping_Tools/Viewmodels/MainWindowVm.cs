using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Mapping_Tools.Views;
using Mapping_Tools.Views.Preferences;
using Mapping_Tools.Views.Standard;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Mapping_Tools.Viewmodels {
    public class MainWindowVm : BindableBase {
        private ICollectionView navigationItemsView;

        private List<FrameworkElement> DefaultItems;
        private List<FrameworkElement> ToolItems;
        private List<FrameworkElement> FavoriteItems;

        public ViewCollection Views { get; set; }

        private string header;
        public string Header {
            get => header;
            set => Set(ref header, value);
        }

        private object view;
        public object View {
            get => view;
            set { 
                if (Set(ref view, value)) {
                    ViewChanged();
                }
            }
        }

        private bool drawerOpen;
        public bool DrawerOpen {
            get => drawerOpen;
            set => Set(ref drawerOpen, value);
        }

        private int selectedPageIndex;
        public int SelectedPageIndex {
            get => selectedPageIndex;
            set => Set(ref selectedPageIndex, value);
        }

        private ListBoxItem selectedPageItem;
        public ListBoxItem SelectedPageItem {
            get => selectedPageItem;
            set {
                // Detect if the selection change was by a mouse click by checking button state
                // This is pretty ghetto and should be changed
                if (Set(ref selectedPageItem, value) && Mouse.LeftButton == MouseButtonState.Pressed) {
                    GoToSelectedPage.Execute(null);
                }
            }
        }

        private string searchKeyword;
        public string SearchKeyword {
            get => searchKeyword;
            set {
                if (Set(ref searchKeyword, value)) {
                    navigationItemsView.Refresh();
                    navigationItemsView.MoveCurrentToFirst();
                    SelectedPageItem = navigationItemsView.CurrentItem as ListBoxItem;
                }
            }
        }

        private ScrollBarVisibility horizontalScrollBarVisibility;
        public ScrollBarVisibility HorizontalContentScrollBarVisibility {
            get => horizontalScrollBarVisibility;
            set => Set(ref horizontalScrollBarVisibility, value);
        }

        private ScrollBarVisibility verticalScrollBarVisibility;
        public ScrollBarVisibility VerticalContentScrollBarVisibility {
            get => verticalScrollBarVisibility;
            set => Set(ref verticalScrollBarVisibility, value);
        }

        private Visibility projectMenuVisibility;
        public Visibility ProjectMenuVisibility {
            get => projectMenuVisibility;
            set => Set(ref projectMenuVisibility, value);
        }

        private ObservableCollection<MenuItem> projectMenuItems;
        public ObservableCollection<MenuItem> ProjectMenuItems {
            get => projectMenuItems;
            set => Set(ref projectMenuItems, value);
        }

        private ObservableCollection<FrameworkElement> navigationItems;
        public ObservableCollection<FrameworkElement> NavigationItems {
            get => navigationItems;
            set => Set(ref navigationItems, value);
        }

        private string currentBeatmaps;
        public string CurrentBeatmaps {
            get => currentBeatmaps;
            set => Set(ref currentBeatmaps, value);
        }

        public MainWindowVm() {
            projectMenuItems = new ObservableCollection<MenuItem>();

            Views = new ViewCollection(); // Make a ViewCollection object

            SetCurrentView(typeof(StandardView)); // Generate Standard view model to show on startup

            GoToSelectedPage = new CommandImplementation(_ => {
                var item = selectedPageItem;
                if (item?.Content == null) return;
                string name = item.Tag.ToString();
                if (string.IsNullOrEmpty(name)) return;
                SetCurrentView(name);
                SearchKeyword = string.Empty;
            });

            SelectedPageUp = new CommandImplementation(_ => {
                navigationItemsView.MoveCurrentToPrevious();
                SelectedPageItem = navigationItemsView.CurrentItem as ListBoxItem;
            });

            SelectedPageDown = new CommandImplementation(_ => {
                navigationItemsView.MoveCurrentToNext();
                SelectedPageItem = navigationItemsView.CurrentItem as ListBoxItem;
            });

            ToggleNavigationDrawer = new CommandImplementation(_ => {
                DrawerOpen = !DrawerOpen;
            });

            GenerateNavigationItems();
            UpdateNavigationItems();
        }

        public CommandImplementation GoToSelectedPage { get; }
        public CommandImplementation SelectedPageUp { get; }
        public CommandImplementation SelectedPageDown { get; }
        public CommandImplementation ToggleNavigationDrawer { get; }

        private void GenerateDefaultItems() {
            DefaultItems = new List<FrameworkElement> {
                CreateNavigationItem(typeof(StandardView)),
                CreateNavigationItem(typeof(PreferencesView))
            };
        }

        private void GenerateToolItems() {
            var tools = ViewCollection.GetAllToolTypes()
                .Where(o => o.GetCustomAttribute<HiddenToolAttribute>() == null)
                .OrderBy(ViewCollection.GetName);
            ToolItems = tools.Select(o => (FrameworkElement)CreateNavigationItem(o, 2)).ToList();
        }

        private void GenerateFavoriteToolItems() {
            FavoriteItems = SettingsManager.Settings.FavoriteTools.Select(ViewCollection.GetType)
                .Where(o => o is not null && o.GetCustomAttribute<HiddenToolAttribute>() == null)
                .OrderBy(ViewCollection.GetName)
                .Select(o => (FrameworkElement)CreateNavigationItem(o, 2)).ToList();
        }

        private void GenerateNavigationItems() {
            GenerateDefaultItems();
            GenerateFavoriteToolItems();
            GenerateToolItems();
        }

        private void UpdateNavigationItems() {
            var items = DefaultItems.Concat(new[] { new Separator() });

            if (FavoriteItems.Count > 0) {
                items = items.Concat(FavoriteItems).Concat(new[] { new Separator() });
            }

            items = items.Concat(ToolItems);
            
            NavigationItems = new ObservableCollection<FrameworkElement>(items);
            navigationItemsView = CollectionViewSource.GetDefaultView(NavigationItems);
            navigationItemsView.Filter = SearchItemsFilter;
        }

        private ListBoxItem CreateNavigationItem(Type type, double verticalMargin=4) {
            var name = ViewCollection.GetName(type);
            var content = new TextBlock { Text = name, Margin = new Thickness(10, verticalMargin, 0, verticalMargin) };
            var item = new ListBoxItem { Tag = name, ToolTip = $"Open {name}.", Content = content};
            CreateContextMenu(item, name);
            return item;
        }
        
        private void CreateContextMenu(FrameworkElement item, string name) {
            var cm = new ContextMenu();
            var menuItem = new MenuItem { Header = "_Favorite", Tag = item };
            UpdateMenuItem(menuItem, SettingsManager.Settings.FavoriteTools.Contains(name));
            menuItem.Click += FavoriteItem_OnClick;
            cm.Items.Add(menuItem);
            item.ContextMenu = cm;
        }

        private void FavoriteItem_OnClick(object sender, RoutedEventArgs e) {
            if (sender is MenuItem { Tag: ListBoxItem { Tag: string name } } mi) {
                // Toggle favorite
                // Update context menu
                if (SettingsManager.Settings.FavoriteTools.Contains(name)) {
                    SettingsManager.Settings.FavoriteTools.Remove(name);
                    UpdateMenuItem(mi, false);
                } else {
                    SettingsManager.Settings.FavoriteTools.Add(name);
                    UpdateMenuItem(mi, true);
                }
                // Update favorite list in UI
                GenerateFavoriteToolItems();
                UpdateNavigationItems();
            }
        }

        private static void UpdateMenuItem(MenuItem mi, bool isFavorite) {
            mi.Icon = isFavorite ?
                new PackIcon { Kind = PackIconKind.Star } :
                new PackIcon { Kind = PackIconKind.StarBorder };
        }

        private bool SearchItemsFilter(object obj) {
            if (string.IsNullOrWhiteSpace(SearchKeyword)) {
                return true;
            }

            return obj is FrameworkElement item && 
                item.Tag != null && 
                item.Tag.ToString().ToLower().Contains(SearchKeyword!.ToLower());
        }

        private void SetCurrentView(string name) {
            if (string.IsNullOrEmpty(name)) return;
            try {
                SetCurrentView(Views.GetView(name));
            } catch (ArgumentException ex) {
                ex.Show();
            }
        }

        private void SetCurrentView(Type type) {
            if (type == null) return;
            try {
                SetCurrentView(Views.GetView(type));
            } catch (ArgumentException ex) {
                ex.Show();
            }
        }

        public void SetCurrentView(object view) {
            if (view == null)
                return;

            var type = view.GetType();

            Header = type.GetCustomAttribute<DontShowTitleAttribute>() == null ? $"Mapping Tools - {ViewCollection.GetName(type)}" : "Mapping Tools";

            VerticalContentScrollBarVisibility = type.GetCustomAttribute<VerticalContentScrollAttribute>() != null ?
                ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;

            HorizontalContentScrollBarVisibility = type.GetCustomAttribute<HorizontalContentScrollAttribute>() != null ?
                ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;

            if (View is MappingTool mt) {
                mt.Deactivate();
            }
            if (view is MappingTool nmt) {
                nmt.Activate();
            }

            View = view;
        }

        private void ViewChanged() {
            DrawerOpen = false;

            var isSavable = View.GetType().GetInterfaces().Any(x =>
                              x.IsGenericType &&
                              x.GetGenericTypeDefinition() == typeof(ISavable<>));

            ProjectMenuVisibility = Visibility.Collapsed;
            ProjectMenuItems.Clear();

            if (isSavable) {
                ProjectMenuVisibility = Visibility.Visible;

                ProjectMenuItems.Add(GetSaveProjectMenuItem());
                ProjectMenuItems.Add(GetLoadProjectMenuItem());
                ProjectMenuItems.Add(GetNewProjectMenuItem());
            }

            if (View is IHaveExtraProjectMenuItems havingExtraProjectMenuItems) {
                ProjectMenuVisibility = Visibility.Visible;

                foreach (var menuItem in havingExtraProjectMenuItems.GetMenuItems()) {
                    ProjectMenuItems.Add(menuItem);
                }
            }
        }
        private MenuItem GetSaveProjectMenuItem() {
            var menu = new MenuItem {
                Header = "_Save project",
                Icon = new PackIcon { Kind = PackIconKind.ContentSave },
                ToolTip = "Save tool settings to file."
            };
            menu.Click += SaveProject;

            return menu;
        }

        private MenuItem GetLoadProjectMenuItem() {
            var menu = new MenuItem {
                Header = "_Load project",
                Icon = new PackIcon { Kind = PackIconKind.Folder },
                ToolTip = "Load tool settings from file."
            };
            menu.Click += LoadProject;

            return menu;
        }

        private MenuItem GetNewProjectMenuItem() {
            var menu = new MenuItem {
                Header = "_New project",
                Icon = new PackIcon { Kind = PackIconKind.Rocket },
                ToolTip = "Load the default tool settings."
            };
            menu.Click += NewProject;

            return menu;
        }

        private void LoadProject(object sender, RoutedEventArgs e) {
            if (!ProjectManager.IsSavable(View))
                return;
            dynamic data = View;
            ProjectManager.LoadProject(data, true);
        }

        private void SaveProject(object sender, RoutedEventArgs e) {
            if (!ProjectManager.IsSavable(View))
                return;
            dynamic data = View;
            ProjectManager.SaveProject(data, true);
        }

        private void NewProject(object sender, RoutedEventArgs e) {
            if (!ProjectManager.IsSavable(View))
                return;
            dynamic data = View;
            ProjectManager.NewProject(data, true);
        }

    }
}
