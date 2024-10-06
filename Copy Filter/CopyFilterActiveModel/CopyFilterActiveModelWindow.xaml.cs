using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace MinhTranTools.CopyFilterActiveModel
{
    public partial class CopyFilterActiveModelWindow : Window
    {
        private Document doc;
        private View selectView;
        private List<Item> listFilters;
        private List<Item> listDesViews;
        private List<string> sourceViewNames;

        public CopyFilterActiveModelWindow(Document doc)
        {
            InitializeComponent();
            this.doc = doc;

            List<View> allViews = GetAllViews(doc);
            List<string> sourceViewNames = new List<string>();
            foreach (View view in allViews)
            {
                string name = $"{view.Name} [{view.ViewType}]";
                sourceViewNames.Add(name);
            }

            // Insert views to Source View
            cbb_SourceView.ItemsSource = sourceViewNames;
            this.sourceViewNames = sourceViewNames;
            if (sourceViewNames.Count > 0) cbb_SourceView.SelectedValue = sourceViewNames[0];

            // Insert views to Destination Views
            var listDesViews = sourceViewNames.Select(x => new Item(x, false)).ToList();
            this.listDesViews = listDesViews;
            lv_DestinationViews.ItemsSource = listDesViews;
        }

        private List<View> GetAllViews(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> views = collector.OfCategory(BuiltInCategory.OST_Views).ToElements();
            List<View> allViews = new List<View>();
            foreach (View view in views)
            {
                allViews.Add(view);
            }
            return allViews;
        }

        private View GetViewByName(Document doc, string viewName)
        {
            List<View> allViews = GetAllViews(doc);
            foreach (View view in allViews)
            {
                string name = $"{view.Name} [{view.ViewType}]";
                if (name == viewName) return view;
            }
            return null;
        }

        private List<Element> GetFiltersInView(Document doc, View view)
        {
            List<Element> filters = new List<Element>();
            ICollection<ElementId> filterIds = view.GetFilters();
            foreach (ElementId id in filterIds)
            {
                var filter = doc.GetElement(id);
                filters.Add(filter);
            }
            return filters;
        }

        private Element GetFilterByName(Document doc, string filterName)
        {
            List<Element> filtersInView = GetFiltersInView(doc, selectView);
            foreach (Element filter in filtersInView)
            {
                if (filter.Name == filterName) return filter;
            }
            return null;
        }

        private bool CheckExistingFilter(Document doc, List<View> views, List<Element> filters)
        {
            // Create HashSet to store filters
            HashSet<string> filterNames = new HashSet<string>();

            foreach (Element filter in filters)
            {
                if (filter.Name != null) // Check if filter != null
                {
                    filterNames.Add(filter.Name);
                }
            }

            foreach (View view in views)
            {
                List<Element> filtersInView = GetFiltersInView(doc, view);

                // Ignore the view if there is no filter in it
                if (filtersInView.Count == 0)
                {
                    continue;
                }

                // Else
                foreach (Element filterInView in filtersInView)
                {
                    if (filterInView.Name != null && filterNames.Contains(filterInView.Name))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        private void CopyFilterToView(View view, Element filter)
        {
            using (Transaction t = new Transaction(doc, "Copy Filter Active Model"))
            {
                t.Start();
                bool filterStatus = selectView.GetFilterVisibility(filter.Id);
                OverrideGraphicSettings filterOverride = selectView.GetFilterOverrides(filter.Id);
                try
                {
                    view.SetFilterVisibility(filter.Id, filterStatus);
                    view.SetFilterOverrides(filter.Id, filterOverride);
                }
                catch { }
                t.Commit();
            }
        }

        private void cbb_SourceView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedViewName = cbb_SourceView.SelectedItem as string;
            View selectView = GetViewByName(doc, selectedViewName);
            this.selectView = selectView;

            List<Element> filtersInView = GetFiltersInView(doc, selectView);
            List<string> filterNames = new List<string>();
            foreach (Element filter in filtersInView)
            {
                string filterName = filter.Name;
                filterNames.Add(filterName);
            }

            var listFilters = filterNames.Select(x => new Item(x, false)).ToList();
            this.listFilters = listFilters;
            lv_Filters.ItemsSource = listFilters;
        }

        private void bt_Ok_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = GetCheckedItems(lv_Filters);
            List<Element> sourceFilters = new List<Element>();
            if (selectedItems != null && selectedItems.Count > 0)
            {
                // Get selected filters
                foreach (var item in selectedItems)
                {
                    string filterName = item.ToString();
                    Element filter = GetFilterByName(doc, filterName);
                    sourceFilters.Add(filter);
                }

                // Get destination views and Copy Filter
                var selectedDestinationViews = GetCheckedItems(lv_DestinationViews);
                if (selectedDestinationViews != null && selectedDestinationViews.Count > 0)
                {
                    List<View> destinationViews = new List<View>();
                    foreach (var item in selectedDestinationViews)
                    {
                        string destinationViewName = item.ToString();
                        View destinationView = GetViewByName(doc, destinationViewName);
                        destinationViews.Add(destinationView);
                    }

                    // Execute
                    using (TransactionGroup tg = new TransactionGroup(doc, "Copy Filter Active Model"))
                    {
                        tg.Start();
                        if (CheckExistingFilter(doc, destinationViews, sourceFilters))
                        {
                            OverrideConfirmationWindow confirmationWindow = new OverrideConfirmationWindow();

                            // Execute when user hit Yes button
                            if (confirmationWindow.ShowDialog() == true && confirmationWindow.UserResponse)
                            {
                                foreach (View view in destinationViews)
                                {
                                    foreach (Element filter in sourceFilters)
                                    {
                                        CopyFilterToView(view, filter);
                                    }
                                }
                            }

                            // Execute when user hit the X button
                            else if (confirmationWindow.Confirm == false)
                            {

                                return;
                            }

                            // Execute when user hit No button
                            else
                            {
                                foreach (View view in destinationViews)
                                {
                                    List<Element> filtersInView = GetFiltersInView(doc, view);

                                    // Create a hashset containing existing filters in destination views
                                    HashSet<string> existingFilterNames = new HashSet<string>();
                                    foreach (Element existingFilter in filtersInView)
                                    {
                                        existingFilterNames.Add(existingFilter.Name);
                                    }

                                    // Check and copy non-exisisting filters
                                    foreach (Element sourceFilter in sourceFilters)
                                    {
                                        if (!existingFilterNames.Contains(sourceFilter.Name))
                                        {
                                            CopyFilterToView(view, sourceFilter);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (View view in destinationViews)
                            {
                                foreach (Element filter in sourceFilters)
                                {
                                    CopyFilterToView(view, filter);
                                }
                            }
                        }

                        // Notify the result and close the program
                        MessageBox.Show("Copy Filters To Views Successfully !");
                        Close();
                        tg.Assimilate();
                    }

                }
                else
                {
                    MessageBox.Show("No destination views selected. Please select again!");
                    return;
                }


            }
            else
            {
                MessageBox.Show("No filters selected. Please select again!");
                return;
            }
        }

        private void ShiftToCheck(ListView lv, CheckBox checkBox)
        {
            var allItems = lv.Items;
            var seletedItems = lv.SelectedItems;
            var tempList = new List<Item>();

            if (checkBox != null)
            {
                foreach (var item in allItems)
                {
                    var wpfItem = (Item)item;
                    if (seletedItems.Contains(item))
                    {
                        if (checkBox.IsChecked == true)
                        {
                            wpfItem.IsChecked = true;
                        }
                        else wpfItem.IsChecked = false;
                    }

                    tempList.Add(wpfItem);
                }

                lv.ItemsSource = tempList;
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                // Check the checked box belongs to which list views
                if (lv_Filters.Items.Contains(checkBox.DataContext))
                {
                    // Click for lv_Filters
                    ShiftToCheck(lv_Filters, checkBox);
                }
                else if (lv_DestinationViews.Items.Contains(checkBox.DataContext))
                {
                    // Click for lv_DestinationViews
                    ShiftToCheck(lv_DestinationViews, checkBox);
                }
            }
        }

        private List<string> GetCheckedItems(ListView lv)
        {
            var names = new List<string>();
            foreach (Item item in lv.Items)
            {
                if (item.IsChecked)
                {
                    names.Add(item.Name);
                }
            }
            return names;
        }

        private void SelectAll(ListBox lbx, bool isChecked)
        {
            var tempList = new List<Item>();
            foreach (var item in lbx.Items)
            {
                (item as Item).IsChecked = isChecked;
                tempList.Add(item as Item);
            }
            lbx.ItemsSource = tempList;
        }

        private void bt_FilterAll_Click(object sender, RoutedEventArgs e)
        {
            SelectAll(lv_Filters, true);
        }

        private void bt_FilterNone_Click(object sender, RoutedEventArgs e)
        {
            SelectAll(lv_Filters, false);
        }

        private void bt_DesviewAll_Click(object sender, RoutedEventArgs e)
        {
            SelectAll(lv_DestinationViews, true);
        }

        private void bt_DesviewNone_Click(object sender, RoutedEventArgs e)
        {
            SelectAll(lv_DestinationViews, false);
        }

        private void bt_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Event handler for TextBox text change to filter the list
        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = FilterTextBox.Text.ToLower();
            // Filter the list based on the input
            var filteredNames = listFilters.Where(item => item.Name.ToLower().Contains(filter)).ToList();
            // Update ListBox with the filtered results
            lv_Filters.ItemsSource = filteredNames;
        }

        private void DesViewTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string viewName = DesViewTextBox.Text.ToLower();
            // Filter the list based on the input
            var desViewNames = listDesViews.Where(item => item.Name.ToLower().Contains(viewName)).ToList();
            // Update ListBox with the filtered results
            lv_DestinationViews.ItemsSource = desViewNames;
        }

        private void SourceViewTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string viewName = SourceViewTextBox.Text.ToLower();
            // Filter the list based on the input
            var sourceNames = sourceViewNames.Where(name => name.ToLower().Contains(viewName)).ToList();
            // Update ListBox with the filtered results
            cbb_SourceView.ItemsSource = sourceNames;
            if (sourceNames.Count > 0) cbb_SourceView.SelectedValue = sourceNames[0];
        }
    }
}