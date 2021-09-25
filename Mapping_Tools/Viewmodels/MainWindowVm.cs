using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Mapping_Tools.Views;
using Mapping_Tools.Views.Preferences;
using Mapping_Tools.Views.Standard;
using MaterialDesignThemes.Wpf;
using System;
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
        private readonly ICollectionView navigationItemsView;

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
                if (Set(ref selectedPageItem, value)) {
                    if (value?.Content == null)
                        return;

                    var toolName = value.Tag.ToString();
                    SetCurrentView(toolName);
                }
            }
        }

        private string searchKeyword;
        public string SearchKeyword {
            get => searchKeyword;
            set {
                if (Set(ref searchKeyword, value)) {
                    navigationItemsView.Refresh();
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

            NavigationItems = GenerateNavigationItems();

            navigationItemsView = CollectionViewSource.GetDefaultView(NavigationItems);
            navigationItemsView.Filter = SearchItemsFilter;

            GoToSearchResult = new CommandImplementation(_ => {
                var name = NavigationItems.FirstOrDefault(o => SearchItemsFilter(o))?.Tag?.ToString();
                if (string.IsNullOrEmpty(name)) return;
                SetCurrentView(name);
                SearchKeyword = string.Empty;
            });

            ToggleNavigationDrawer = new CommandImplementation(p => {
                DrawerOpen = !DrawerOpen;
            });
        }

        public CommandImplementation GoToSearchResult { get; }
        public CommandImplementation ToggleNavigationDrawer { get; }

        private ObservableCollection<FrameworkElement> GenerateNavigationItems() {
            var tools = ViewCollection.GetAllToolTypes()
                .Where(o => o.GetCustomAttribute<HiddenToolAttribute>() == null)
                .OrderBy(o => ViewCollection.GetName(o));

            var collection = new ObservableCollection<FrameworkElement>();

            collection.Add(CreateNavigationItem(typeof(StandardView)));
            collection.Add(CreateNavigationItem(typeof(PreferencesView)));
            collection.Add(new Separator());

            foreach (var tool in tools) {
                collection.Add(CreateNavigationItem(tool, 2));
            }

            return collection;
        }

        private ListBoxItem CreateNavigationItem(Type type, double verticalMargin=4) {
            var name = ViewCollection.GetName(type);
            var content = new TextBlock { Text = name, Margin = new Thickness(10, verticalMargin, 0, verticalMargin) };
            var item = new ListBoxItem { Tag = name, ToolTip = $"Open {name}.", Content = content };
            return item;
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
