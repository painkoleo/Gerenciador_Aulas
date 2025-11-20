using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GerenciadorAulas.Helpers
{
    public static class MultiSelectTreeViewBehavior
    {
        #region IsMultiSelectEnabled Attached Property

        public static readonly DependencyProperty IsMultiSelectEnabledProperty =
            DependencyProperty.RegisterAttached("IsMultiSelectEnabled", typeof(bool), typeof(MultiSelectTreeViewBehavior),
                new PropertyMetadata(false, OnIsMultiSelectEnabledChanged));

        public static bool GetIsMultiSelectEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMultiSelectEnabledProperty);
        }

        public static void SetIsMultiSelectEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMultiSelectEnabledProperty, value);
        }

        private static void OnIsMultiSelectEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView)
            {
                if ((bool)e.NewValue)
                {
                    treeView.AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseDown), true);
                }
                else
                {
                    treeView.RemoveHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseDown));
                }
            }
        }

        #endregion

        #region SelectedItems Attached Property

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(MultiSelectTreeViewBehavior),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static IList? GetSelectedItems(DependencyObject obj)
        {
            return (IList?)obj.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject obj, IList? value)
        {
            obj.SetValue(SelectedItemsProperty, value);
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView)
            {
                if (e.OldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= OnSelectedItemsCollectionChanged;
                }
                if (e.NewValue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += OnSelectedItemsCollectionChanged;
                }
            }
        }

        private static void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // This handler can be used to react to changes in the bound SelectedItems collection from the ViewModel.
            // For example, if the ViewModel clears the collection, we might want to deselect all TreeViewItems.
            // However, for now, the selection logic is primarily driven by UI interaction.
        }

        #endregion

        #region IsItemSelected Attached Property (for TreeViewItem)

        public static readonly DependencyProperty IsItemSelectedProperty =
            DependencyProperty.RegisterAttached("IsItemSelected", typeof(bool), typeof(MultiSelectTreeViewBehavior),
                new PropertyMetadata(false));

        public static bool GetIsItemSelected(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsItemSelectedProperty);
        }

        public static void SetIsItemSelected(DependencyObject obj, bool value)
        {
            obj.SetValue(IsItemSelectedProperty, value);
        }

        #endregion

        #region Internal Logic

        private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is CheckBox)
            {
                return;
            }

            if (sender is TreeView treeView && e.ChangedButton == MouseButton.Left)
            {
                var clickedItem = FindTreeViewItem(e.OriginalSource as DependencyObject);

                if (clickedItem == null)
                {
                    // Clicked on empty space in TreeView, clear selection if Ctrl is not pressed
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        ClearSelection(treeView);
                    }
                    return;
                }

                var selectedItems = GetSelectedItems(treeView);
                if (selectedItems == null)
                {
                    selectedItems = new ObservableCollection<object>();
                    SetSelectedItems(treeView, selectedItems);
                }

                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    ToggleItemSelected(clickedItem, selectedItems);
                }
                else
                {
                    HandleSingleSelection(clickedItem, selectedItems);
                }
            }
        }

        private static TreeViewItem? FindTreeViewItem(DependencyObject? current)
        {
            while (current != null && !(current is TreeViewItem))
            {
                current = VisualTreeHelper.GetParent(current);
            }
            return current as TreeViewItem;
        }

        private static void ClearSelection(TreeView treeView)
        {
            var selectedItems = GetSelectedItems(treeView);
            if (selectedItems != null)
            {
                var itemsToDeselect = selectedItems.Cast<object>().ToList(); // Avoid modifying collection while iterating
                foreach (var item in itemsToDeselect)
                {
                    var treeViewItem = GetTreeViewItemForData(treeView, item);
                    if (treeViewItem != null)
                    {
                        SetIsItemSelected(treeViewItem, false);
                    }
                }
                selectedItems.Clear();
            }
        }

        private static void ToggleItemSelected(TreeViewItem clickedItem, IList selectedItems)
        {
            var dataItem = clickedItem.Header;

            if (selectedItems.Contains(dataItem))
            {
                selectedItems.Remove(dataItem);
                SetIsItemSelected(clickedItem, false);
            }
            else
            {
                selectedItems.Add(dataItem);
                SetIsItemSelected(clickedItem, true);
            }
        }

        private static void HandleSingleSelection(TreeViewItem clickedItem, IList selectedItems)
        {
            // Clear previous selections
            var treeView = FindVisualParent<TreeView>(clickedItem);
            if (treeView != null)
            {
                ClearSelection(treeView);
            }

            var dataItem = clickedItem.Header;
            if (!selectedItems.Contains(dataItem))
            {
                selectedItems.Add(dataItem);
            }
            SetIsItemSelected(clickedItem, true);
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T? parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }

        private static TreeViewItem? GetTreeViewItemForData(ItemsControl parent, object dataItem)
        {
            if (parent == null || dataItem == null)
            {
                return null;
            }

            foreach (var item in parent.Items)
            {
                if (item == dataItem)
                {
                    return parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                }

                if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
                {
                    if (treeViewItem.Header == dataItem)
                    {
                        return treeViewItem;
                    }
                    var found = GetTreeViewItemForData(treeViewItem, dataItem);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }
        #endregion
    }
}