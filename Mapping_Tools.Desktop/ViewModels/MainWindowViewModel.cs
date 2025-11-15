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
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Mapping_Tools.Application;
using Mapping_Tools.Desktop.Models;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    private readonly NavigationService navigationService;
    private ViewModelBase? currentViewModel;

    public ViewModelBase? CurrentViewModel {
        get => currentViewModel;
        set
        {
            // Capture local variable to prevent currentViewModel from changing before DisposeCurrentAsync is called
            var previousViewModel = currentViewModel;
            Task.Run(() => navigationService.DisposeCurrentAsync(previousViewModel));
            
            this.RaiseAndSetIfChanged(ref currentViewModel, value);
        }
    }
    
    private bool isBusy;

    public bool IsBusy
    {
        get => isBusy;
        private set => this.RaiseAndSetIfChanged(ref isBusy, value);
    }

    public ReactiveCommand<Unit, Unit>? GoHomeCommand { get; }
    public ReactiveCommand<Unit, Unit>? GoSettingsCommand { get; }
    
    public MainWindowViewModel() : this(null!, null!) { }

    public MainWindowViewModel(NavigationService navigationService, IAppLifecycle appLifecycle) {
        this.navigationService = navigationService;
        this.navigationService.OnNavigate += vm => {
            CurrentViewModel = vm;
            ViewChanged();
        };
        
        // Ensure current view model is disposed on app exit
        appLifecycle.UICleanup.Register(() => navigationService.DisposeCurrentAsync(CurrentViewModel).GetAwaiter().GetResult());

        // Generate Standard view model to show on startup
        Task.Run(() => this.navigationService.NavigateAsync<HomeViewModel>());
        
        GoHomeCommand = ReactiveCommand.CreateFromTask(NavigateAsync<HomeViewModel>);
        GoSettingsCommand = ReactiveCommand.CreateFromTask(NavigateAsync<SettingsViewModel>);

        projectMenuItems = [];

        GoToSelectedPage = ReactiveCommand.Create(() => {
            var item = selectedPageItem;
            if (item?.Content == null) return;
            string? name = item.Tag!.ToString();
            if (string.IsNullOrEmpty(name)) return;
            Task.Run(() => this.navigationService.NavigateAsync(name));
            SearchKeyword = string.Empty;
        });

        SelectedPageUp = ReactiveCommand.Create(() => {
            // SelectedPageItem = navigationItemsView.CurrentItem as ListBoxItem;
            SelectedPageItem?.Focus();
        });

        SelectedPageDown = ReactiveCommand.Create(() => {
            // SelectedPageItem = navigationItemsView.CurrentItem as ListBoxItem;
            SelectedPageItem?.Focus();
        });

        ClearSearchBox = ReactiveCommand.Create(() => {
            SearchKeyword = string.Empty;
        });

        OpenNavigationDrawer = ReactiveCommand.Create(() => {
            DrawerOpen = true;
            SearchFocused = false;
            SearchFocused = true;
        });

        GenerateNavigationItems();
        UpdateNavigationItems();

        DrawerOpen = true;
        SearchFocused = true;
    }
    
    private async Task NavigateAsync<T>() where T : ViewModelBase {
        IsBusy = true;
        await navigationService.NavigateAsync<T>();
        IsBusy = false;
    }
    
    
    private readonly ObservableCollection<Control> allNavigationItems = [];

    public ObservableCollection<Control> NavigationItems { get; } = [];

    private List<Control> defaultItems;
    private List<Control> toolItems;
    private List<Control> favoriteItems;

    private string header;
    public string Header {
        get => header;
        set => this.RaiseAndSetIfChanged(ref header, value);
    }

    private bool drawerOpen;
    public bool DrawerOpen {
        get => drawerOpen;
        set => this.RaiseAndSetIfChanged(ref drawerOpen, value);
    }

    private bool searchFocused;
    public bool SearchFocused {
        get => searchFocused;
        set => this.RaiseAndSetIfChanged(ref searchFocused, value);
    }

    private int selectedPageIndex;
    public int SelectedPageIndex {
        get => selectedPageIndex;
        set => this.RaiseAndSetIfChanged(ref selectedPageIndex, value);
    }

    private ListBoxItem? selectedPageItem;
    public ListBoxItem? SelectedPageItem {
        get => selectedPageItem;
        set => this.RaiseAndSetIfChanged(ref selectedPageItem, value);
    }

    private string searchKeyword;
    public string SearchKeyword {
        get => searchKeyword;
        set {
            if (searchKeyword == value) {
                return;
            }

            this.RaisePropertyChanging();
            searchKeyword = value;
            this.RaisePropertyChanged();
            ApplyFilter();
        }
    }

    private ScrollBarVisibility horizontalScrollBarVisibility;
    public ScrollBarVisibility HorizontalContentScrollBarVisibility {
        get => horizontalScrollBarVisibility;
        set => this.RaiseAndSetIfChanged(ref horizontalScrollBarVisibility, value);
    }

    private ScrollBarVisibility verticalScrollBarVisibility;
    public ScrollBarVisibility VerticalContentScrollBarVisibility {
        get => verticalScrollBarVisibility;
        set => this.RaiseAndSetIfChanged(ref verticalScrollBarVisibility, value);
    }

    private bool projectMenuVisibility;
    public bool ProjectMenuVisibility {
        get => projectMenuVisibility;
        set => this.RaiseAndSetIfChanged(ref projectMenuVisibility, value);
    }

    private ObservableCollection<MenuItem> projectMenuItems;
    public ObservableCollection<MenuItem> ProjectMenuItems {
        get => projectMenuItems;
        set => this.RaiseAndSetIfChanged(ref projectMenuItems, value);
    }

    private string currentBeatmaps;
    public string CurrentBeatmaps {
        get => currentBeatmaps;
        set => this.RaiseAndSetIfChanged(ref currentBeatmaps, value);
    }

    public ReactiveCommand<Unit, Unit>? GoToSelectedPage { get; }
    public ReactiveCommand<Unit, Unit>? SelectedPageUp { get; }
    public ReactiveCommand<Unit, Unit>? SelectedPageDown { get; }
    public ReactiveCommand<Unit, Unit>? ClearSearchBox { get; }
    public ReactiveCommand<Unit, Unit>? OpenNavigationDrawer { get; }

    private void GenerateDefaultItems() {
        defaultItems = [
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
        toolItems = [];
    }

    private void GenerateFavoriteToolItems() {
        // var tools = ViewCollection.GetAllToolTypes()
            // .Where(o => o.GetCustomAttribute<HiddenToolAttribute>() == null &&
                        // SettingsManager.Settings.FavoriteTools.Contains(ViewCollection.GetName(o)))
            // .OrderBy(ViewCollection.GetName);
        // favoriteItems = tools.Select(o => (Control)CreateNavigationItem(o, 2)).ToList();
        favoriteItems = [];
    }

    private void GenerateNavigationItems() {
        GenerateDefaultItems();
        GenerateFavoriteToolItems();
        GenerateToolItems();
    }

    private void UpdateNavigationItems() {
        var items = defaultItems.Concat([new Separator()]);

        if (favoriteItems.Count > 0) {
            items = items.Concat(favoriteItems).Concat([new Separator()]);
        }

        items = items.Concat(toolItems);

        allNavigationItems.Clear();
        foreach (var item in items)
            allNavigationItems.Add(item);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        NavigationItems.Clear();

        foreach (var item in allNavigationItems.Where(SearchItemsFilter))
            NavigationItems.Add(item);

        SelectedPageIndex = 0;
    }

    private ListBoxItem CreateNavigationItem(Type type, double verticalMargin=4) {
        var name = type.Name;
        var content = new TextBlock { Text = name, Margin = new Thickness(10, verticalMargin, 0, verticalMargin) };
        var item = new ListBoxItem { Tag = name, Content = content};
        ToolTip.SetTip(item, $"Open {name}.");
        CreateContextMenu(item, name);
        item.PointerPressed += ItemOnPreviewMouseLeftButtonDown;
        return item;
    }

    private void ItemOnPreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e) {
        if (sender is not ListBoxItem item)
            return;

        selectedPageItem = item;
        GoToSelectedPage?.Execute();
        e.Handled = true;
    }

    private void CreateContextMenu(Control item, string name) {
        var cm = new ContextMenu();
        var menuItem = new MenuItem { Tag = item };
        // UpdateMenuItem(menuItem, SettingsManager.Settings.FavoriteTools.Contains(name));
        UpdateMenuItem(menuItem, false);
        menuItem.Click += FavoriteItem_OnClick;
        cm.Items.Add(menuItem);
        item.ContextMenu = cm;
    }

    private void FavoriteItem_OnClick(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem { Tag: ListBoxItem { Tag: string name } } mi) {
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
    }

    private static void UpdateMenuItem(MenuItem mi, bool isFavorite) {
        mi.Icon = isFavorite ?
            new MaterialIcon { Kind = MaterialIconKind.Star } :
            new MaterialIcon { Kind = MaterialIconKind.StarBorder };
        mi.Header = isFavorite ? @"_Unfavorite" : @"_Favorite";
    }

    private bool SearchItemsFilter(object obj) {
        if (string.IsNullOrWhiteSpace(SearchKeyword)) {
            return true;
        }

        return obj is Control { Tag: not null } item &&
               item.Tag!.ToString()!.Contains(SearchKeyword, StringComparison.CurrentCultureIgnoreCase);
    }

    private void ViewChanged() {
        //DrawerOpen = false;

        if (currentViewModel == null)
            return;

        ProjectMenuVisibility = false;
        ProjectMenuItems.Clear();

        var isSavable = NavigationService.TryGetModelType(currentViewModel, out _);
        if (isSavable) {
            ProjectMenuVisibility = true;

            AddProjectMenuItem(GetSaveProjectMenuItem());
            AddProjectMenuItem(GetLoadProjectMenuItem());
            AddProjectMenuItem(GetNewProjectMenuItem());
        }

        if (currentViewModel is IHaveExtraProjectMenuItems havingExtraProjectMenuItems) {
            ProjectMenuVisibility = true;

            foreach (var menuItem in havingExtraProjectMenuItems.GetMenuItems()) {
                AddProjectMenuItem(menuItem);
            }
        }

        var type = currentViewModel.GetType();
        Header = type.GetCustomAttribute<DontShowTitleAttribute>() == null ? $"Mapping Tools - {navigationService.GetName(type)}" : "Mapping Tools";

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