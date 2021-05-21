using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mapping_Tools.Components {
    public class DragAndDropListBox<T> : ListBox where T : class {
        private Point _dragStartPoint;

        public DragAndDropListBox() {
            PreviewMouseMove += ListBox_PreviewMouseMove;

            var style = new Style(typeof(ListBoxItem), (Style)FindResource("MaterialDesignListBoxItem"));

            style.Setters.Add(new Setter(AllowDropProperty, true));

            style.Setters.Add(
                new EventSetter(
                    PreviewMouseLeftButtonDownEvent,
                    new MouseButtonEventHandler(ListBoxItem_PreviewMouseLeftButtonDown)));

            style.Setters.Add(
                new EventSetter(
                    DropEvent,
                    new DragEventHandler(ListBoxItem_Drop)));

            ItemContainerStyle = style;
        }

        private static TP FindVisualParent<TP>(DependencyObject child)
            where TP : DependencyObject {
            var parentObject = VisualTreeHelper.GetParent(child);
            switch (parentObject) {
                case null:
                    return null;
                case TP parent:
                    return parent;
                default:
                    return FindVisualParent<TP>(parentObject);
            }
        }

        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e) {
            var point = e.GetPosition(null);
            var diff = _dragStartPoint - point;
            if (e.LeftButton != MouseButtonState.Pressed ||
                !(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) &&
                !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return;
            var lbi = FindVisualParent<ListBoxItem>((DependencyObject) e.OriginalSource);
            if (lbi != null) DragDrop.DoDragDrop(lbi, lbi.DataContext, DragDropEffects.Move);
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ListBoxItem_Drop(object sender, DragEventArgs e) {
            if (!(sender is ListBoxItem item)) return;
            var source = e.Data.GetData(typeof(T)) as T;
            var target = item.DataContext as T;

            var sourceIndex = Items.IndexOf(source);
            var targetIndex = Items.IndexOf(target);

            Move(source, sourceIndex, targetIndex);
        }

        private void Move(T source, int sourceIndex, int targetIndex) {
            if (sourceIndex < targetIndex) {
                if (!(ItemsSource is IList<T> items)) return;
                items.Insert(targetIndex + 1, source);
                items.RemoveAt(sourceIndex);
            } else {
                if (!(ItemsSource is IList<T> items)) return;
                var removeIndex = sourceIndex + 1;
                if (items.Count + 1 <= removeIndex) return;
                items.Insert(targetIndex, source);
                items.RemoveAt(removeIndex);
            }
        }
    }
}