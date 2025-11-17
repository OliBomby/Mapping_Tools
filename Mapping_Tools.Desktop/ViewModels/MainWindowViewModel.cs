using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Mapping_Tools.Application.Types;
using Mapping_Tools.Desktop.Helpers;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Types;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Mapping_Tools.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
    private readonly NavigationService _navigationService;
    private readonly UserSettingsService _userSettingsService;
    private readonly INotificationService _notificationService;

    private List<NavigationItem> _defaultItems = null!;
    private List<NavigationItem> _toolItems = null!;
    private List<NavigationItem> _favoriteItems = null!;
    
    private readonly ObservableCollection<NavigationItem> _allNavigationItems = [];

    public ObservableCollection<NavigationItem> NavigationItems { get; } = [];
    
    [Reactive]
    private ViewModelBase? _currentViewModel;
    
    [Reactive]
    private bool _isBusy;

    [Reactive]
    private string _header = "Mapping Tools";

    [Reactive]
    private bool _drawerOpen = true;

    [Reactive]
    private bool _searchFocused = true;

    [Reactive]
    private int _selectedPageIndex;

    [Reactive]
    private NavigationItem? _selectedPageItem;

    [Reactive]
    private string _searchKeyword = string.Empty;

    [Reactive]
    private ScrollBarVisibility _horizontalContentScrollBarVisibility;

    [Reactive]
    private ScrollBarVisibility _verticalContentScrollBarVisibility;

    [Reactive]
    private bool _projectMenuVisibility;

    [Reactive]
    private ObservableCollection<MenuItem> _projectMenuItems = [];

    // Notification drawer state
    [Reactive]
    private bool _notificationsDrawerOpen;

    [Reactive]
    private bool _hasUnreadNotifications;

    public NotificationsViewModel NotificationsViewModel { get; }
    
    public UserSettings UserSettings => _userSettingsService.Settings;

    public ReactiveCommand<Unit, Unit>? GoToSelectedPage { get; }
    public ReactiveCommand<Unit, Unit>? SelectedPageUp { get; }
    public ReactiveCommand<Unit, Unit>? SelectedPageDown { get; }
    public ReactiveCommand<Unit, Unit>? ClearSearchBox { get; }
    public ReactiveCommand<Unit, Unit>? OpenNavigationDrawer { get; }
    public ReactiveCommand<Unit, Unit>? OpenNotificationsDrawer { get; }
    
    public MainWindowViewModel() : this(null!, null!, null!, null!, null!) { }

    public MainWindowViewModel(
        NavigationService navigationService,
        IAppLifecycle appLifecycle,
        UserSettingsService userSettingsService,
        INotificationService notificationService,
        NotificationsViewModel notificationsViewModel
        ) {
        _navigationService = navigationService;
        _userSettingsService = userSettingsService;
        _notificationService = notificationService;
        NotificationsViewModel = notificationsViewModel;

        // Wire notifications: mark unread when a new notification arrives and drawer is closed
        HasUnreadNotifications = _notificationService.GetNotifications().Any();
        _notificationService.NotificationAdded += (_, _) => {
            if (!NotificationsDrawerOpen) {
                HasUnreadNotifications = true;
            }
        };

        // Subscribe to navigation events and change the current view model accordingly
        _navigationService.OnNavigate += vm => CurrentViewModel = vm;

        PropertyChanging += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentViewModel):
                    Task.Run(() => navigationService.DisposeCurrentAsync(_currentViewModel));
                    break;
                case nameof(NotificationsDrawerOpen):
                    // When opening notifications drawer, mark notifications as read
                    if (NotificationsDrawerOpen) {
                        HasUnreadNotifications = false;
                    }
                    break;
            }
        };

        PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentViewModel):
                    ViewChanged();
                    break;
                case nameof(SearchKeyword):
                    ApplyFilter();
                    break;
            }
        };
        
        GoToSelectedPage = ReactiveCommand.Create(() => {
            var item = NavigationItems.Count == 1 ? NavigationItems[0] : _selectedPageItem;
            if (item == null) return;
            item.ClickCommand?.Execute(null);
            SearchKeyword = string.Empty;
        });

        SelectedPageUp = ReactiveCommand.Create(() => {
            if (NavigationItems.Count == 0) return;
            // Look for previous selectable item
            var index = SelectedPageIndex;
            do {
                index--;
                if (index < 0) {
                    index = NavigationItems.Count - 1;
                }
            } while (!NavigationItems[index].IsSelectable && index != SelectedPageIndex);
            SelectedPageIndex = index;
        });

        SelectedPageDown = ReactiveCommand.Create(() => {
            if (NavigationItems.Count == 0) return;
            // Look for next selectable item
            var index = SelectedPageIndex;
            do {
                index++;
                if (index >= NavigationItems.Count) {
                    index = 0;
                }
            } while (!NavigationItems[index].IsSelectable && index != SelectedPageIndex);
            SelectedPageIndex = index;
        });

        ClearSearchBox = ReactiveCommand.Create(() => {
            SearchKeyword = string.Empty;
        });

        OpenNavigationDrawer = ReactiveCommand.Create(() => {
            if (DrawerOpen && SearchFocused) {
                DrawerOpen = false;
                SearchFocused = false;
            } else {
                DrawerOpen = true;
                SearchFocused = false;
                SearchFocused = true;
            }
        });

        OpenNotificationsDrawer = ReactiveCommand.Create(() => {
            NotificationsDrawerOpen = !NotificationsDrawerOpen;
        });

        GenerateNavigationItems();
        UpdateNavigationItems();
        
        // Ensure current view model is disposed on app exit
        appLifecycle.UICleanup.Register(() => navigationService.DisposeCurrentAsync(CurrentViewModel).GetAwaiter().GetResult());

        // Generate Standard view model to show on startup
        Task.Run(() => _navigationService.NavigateAsync<HomeViewModel>());
    }
    
    public void SetCurrentBeatmaps(string[] paths) {
        _userSettingsService.SetCurrentBeatmaps(paths);
    }
    
    private void NavigateTo(string name) {
        IsBusy = true;
        Task.Run(() => _navigationService.NavigateAsync(name));
        IsBusy = false;
    }

    private void GenerateDefaultItems() {
        _defaultItems = [
            CreateNavigationItem(typeof(HomeViewModel)),
            CreateNavigationItem(typeof(SettingsViewModel)),
        ];
    }

    private void GenerateToolItems() {
        // var tools = ViewCollection.GetAllToolTypes()
        //     .Where(o => o.GetCustomAttribute<HiddenToolAttribute>() == null &&
        //                 !SettingsManager.Settings.FavoriteTools.Contains(ViewCollection.GetName(o)))
        //     .OrderBy(ViewCollection.GetName);
        // toolItems = tools.Select(o => (Control)CreateNavigationItem(o, 2)).ToList();
        _toolItems = [];
    }

    private void GenerateFavoriteToolItems() {
        // var tools = ViewCollection.GetAllToolTypes()
            // .Where(o => o.GetCustomAttribute<HiddenToolAttribute>() == null &&
                        // SettingsManager.Settings.FavoriteTools.Contains(ViewCollection.GetName(o)))
            // .OrderBy(ViewCollection.GetName);
        // favoriteItems = tools.Select(o => (Control)CreateNavigationItem(o, 2)).ToList();
        _favoriteItems = [];
    }

    private void GenerateNavigationItems() {
        GenerateDefaultItems();
        GenerateFavoriteToolItems();
        GenerateToolItems();
    }

    private void UpdateNavigationItems() {
        var items = _defaultItems.Concat([new SeparatorItem()]);

        if (_favoriteItems.Count > 0) {
            items = items.Concat(_favoriteItems).Concat([new SeparatorItem()]);
        }

        items = items.Concat(_toolItems);

        _allNavigationItems.Clear();
        foreach (var item in items)
            _allNavigationItems.Add(item);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        NavigationItems.Clear();

        foreach (var item in _allNavigationItems.Where(SearchItemsFilter))
            NavigationItems.Add(item);

        SelectedPageIndex = 0;
    }

    private NavigationItem CreateNavigationItem(Type type, double verticalMargin=4) {
        var name = type.Name;
        return new NormalItem {
            Text = name,
            Margin = new Thickness(10, verticalMargin, 0, verticalMargin),
            ToolTip = $"Open {name}.",
            ClickCommand = ReactiveCommand.Create(() => NavigateTo(name)),
            ContextMenu = CreateContextMenu(name),
        };
    }

    private ContextMenu CreateContextMenu(string name) {
        var cm = new ContextMenu();
        var menuItem = new MenuItem { Tag = name };
        // UpdateMenuItem(menuItem, SettingsManager.Settings.FavoriteTools.Contains(name));
        UpdateMenuItem(menuItem, false);
        menuItem.Click += FavoriteItem_OnClick;
        cm.Items.Add(menuItem);
        return cm;
    }

    private void FavoriteItem_OnClick(object? sender, RoutedEventArgs e) {
        if (sender is not MenuItem { Tag: string name } mi)
            return;

        // Toggle favorite
        // Update context menu
        // if (SettingsManager.Settings.FavoriteTools.Contains(name)) {
        //     SettingsManager.Settings.FavoriteTools.Remove(name);
        //     UpdateMenuItem(mi, false);
        // } else {
        //     SettingsManager.Settings.FavoriteTools.Add(name);
        //     UpdateMenuItem(mi, true);
        // }
        // Update favorite list in UI
        GenerateFavoriteToolItems();
        GenerateToolItems();
        UpdateNavigationItems();
    }

    private static void UpdateMenuItem(MenuItem mi, bool isFavorite) {
        mi.Icon = isFavorite ?
            new MaterialIcon { Kind = MaterialIconKind.Star } :
            new MaterialIcon { Kind = MaterialIconKind.StarBorder };
        mi.Header = isFavorite ? "_Unfavorite" : "_Favorite";
    }

    private bool SearchItemsFilter(NavigationItem obj) {
        if (string.IsNullOrWhiteSpace(SearchKeyword)) {
            return true;
        }

        return obj is NormalItem item &&
               item.Text.Contains(SearchKeyword, StringComparison.CurrentCultureIgnoreCase);
    }

    private void ViewChanged() {
        //DrawerOpen = false;

        if (_currentViewModel == null)
            return;

        ProjectMenuVisibility = false;
        ProjectMenuItems.Clear();

        var isSavable = NavigationService.TryGetModelType(_currentViewModel, out _);
        if (isSavable) {
            ProjectMenuVisibility = true;

            AddProjectMenuItem(GetSaveProjectMenuItem());
            AddProjectMenuItem(GetLoadProjectMenuItem());
            AddProjectMenuItem(GetNewProjectMenuItem());
        }

        if (_currentViewModel is IHaveExtraProjectMenuItems havingExtraProjectMenuItems) {
            ProjectMenuVisibility = true;

            foreach (var menuItem in havingExtraProjectMenuItems.GetMenuItems()) {
                AddProjectMenuItem(menuItem);
            }
        }

        var type = _currentViewModel.GetType();
        Header = type.GetCustomAttribute<DontShowTitleAttribute>() == null ? $"Mapping Tools - {_navigationService.GetName(type)}" : "Mapping Tools";

        VerticalContentScrollBarVisibility = type.GetCustomAttribute<VerticalContentScrollAttribute>() != null ?
            ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        HorizontalContentScrollBarVisibility = type.GetCustomAttribute<HorizontalContentScrollAttribute>() != null ?
            ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
    }

    private void AddProjectMenuItem(MenuItem item) {
        // Set the foreground color to DynamicResource MaterialDesignBody
        // so that it will change color when the theme changes
        item[!ContentPresenter.ForegroundProperty] = new DynamicResourceExtension("MaterialDesignBody");

        ProjectMenuItems.Add(item);
    }

    private MenuItem GetSaveProjectMenuItem() {
        var menu = new MenuItem {
            Header = "_Save project",
            Icon = new MaterialIcon { Kind = MaterialIconKind.ContentSave },
        };
        ToolTip.SetTip(menu, "Save tool settings to file.");
        menu.Click += SaveProject;

        return menu;
    }

    private MenuItem GetLoadProjectMenuItem() {
        var menu = new MenuItem {
            Header = "_Load project",
            Icon = new MaterialIcon { Kind = MaterialIconKind.Folder },
        };
        ToolTip.SetTip(menu, "Load tool settings from file.");
        menu.Click += LoadProject;

        return menu;
    }

    private MenuItem GetNewProjectMenuItem() {
        var menu = new MenuItem {
            Header = "_New project",
            Icon = new MaterialIcon { Kind = MaterialIconKind.Rocket },
        };
        ToolTip.SetTip(menu, "Load the default tool settings.");
        menu.Click += NewProject;

        return menu;
    }

    private void LoadProject(object? sender, RoutedEventArgs e) {
        // if (!ProjectManager.IsSavable(View))
        //     return;
        // dynamic data = View;
        // ProjectManager.LoadProject(data, true);
    }

    private void SaveProject(object? sender, RoutedEventArgs e) {
        // if (!ProjectManager.IsSavable(View))
            // return;
        // dynamic data = View;
        // ProjectManager.SaveProjectDialog(data);
    }

    private void NewProject(object? sender, RoutedEventArgs e) {
        // if (!ProjectManager.IsSavable(View))
            // return;
        // dynamic data = View;
        // ProjectManager.NewProject(data, true);
    }
}