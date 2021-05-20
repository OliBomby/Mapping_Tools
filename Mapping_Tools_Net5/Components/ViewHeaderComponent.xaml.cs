using System;
using Mapping_Tools.Classes.SystemTools;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Views;

namespace Mapping_Tools.Components {
    /// <summary>
    /// Interaction logic for ViewHeaderComponent.xaml
    /// </summary>
    public partial class ViewHeaderComponent {
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ParentControlProperty =
            DependencyProperty.Register(nameof(ParentControl), typeof(UserControl), typeof(ViewHeaderComponent));

        /// <summary>
        /// 
        /// </summary>
        public UserControl ParentControl {
            get => (UserControl)GetValue(ParentControlProperty);
            set => SetValue(ParentControlProperty, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public ViewHeaderComponent() {
            InitializeComponent();
        }

        /// <summary>Invoked whenever the effective value of any dependency property on this <see cref="T:System.Windows.FrameworkElement" /> has been updated. The specific dependency property that changed is reported in the arguments parameter. Overrides <see cref="M:System.Windows.DependencyObject.OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs)" />.</summary>
        /// <param name="e">The event data that describes the property that changed, as well as old and new values.</param>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            Console.WriteLine(@"PROPERTY CHANGED!");
            try {
                var type = e.NewValue.GetType();
                if (type.GetCustomAttribute<DontShowTitleAttribute>() != null) {
                    MainPanel.Visibility = Visibility.Collapsed;
                } else {
                    MainPanel.Visibility = Visibility.Visible;

                    QuickRunIcon.Visibility = typeof(IQuickRun).IsAssignableFrom(type) ? Visibility.Visible : Visibility.Collapsed;

                    var name = ViewCollection.GetName(type);
                    var description = ViewCollection.GetDescription(type);

                    DescriptionIcon.Visibility =
                        string.IsNullOrEmpty(description) ? Visibility.Collapsed : Visibility.Visible;

                    HeaderTextBlock.Text = name;
                    DescriptionTextBlock.Text = description;
                } 
            } catch (Exception exception) {
                Console.WriteLine(exception);
            }
        }
    }
}
