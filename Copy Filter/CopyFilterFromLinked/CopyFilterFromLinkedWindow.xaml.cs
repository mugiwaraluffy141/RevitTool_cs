using Autodesk.Revit.DB;
using MinhTranTools.CopyFilterActiveModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MinhTranTools.CopyFilterFromLinked
{
    public partial class CopyFilterFromLinkedWindow : Window
    {
        private Document activeDoc;
        private Document selectedLinkedDoc;
        private View selectView;
        private List<Item> listFilters;
        private List<Item> listDesViews;
        private List<string> sourceViewNames;
        private List<string> linkName;


        public CopyFilterFromLinkedWindow(Document doc)
        {

            InitializeComponent();
            activeDoc = doc;

            // Get all views in active document
            List<View> allViewsInActive = GetAllViews(doc);
            List<string> desViewNames = new List<string>();
            foreach (View view in allViewsInActive)
            {
                string name = $"{view.Name} [{view.ViewType}]";
                desViewNames.Add(name);
            }

            // Insert views to Destination Views
            var listDesViews = desViewNames.Select(x => new Item(x, false)).ToList();
            this.listDesViews = listDesViews;
            lv_DestinationViews.ItemsSource = listDesViews;

            // Insert linked documents to cbb_LinkedModel
            List<string> linkName = GetLinkDoc.GetLinkedDocName(doc);
            cbb_LinkedModel.ItemsSource = linkName;
            this.linkName = linkName;
            if (linkName.Count > 0) cbb_LinkedModel.SelectedIndex = 0;

        }

        private void cbb_LinkedModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<Document> linkDoc = GetLinkDoc.GetLinkedDoc(activeDoc);
            int selectedIndex = cbb_LinkedModel.SelectedIndex;
            Document selectedLinkedDoc = linkDoc[selectedIndex];
            this.selectedLinkedDoc = selectedLinkedDoc;

            // Get all views in linked document
            List<View> allViews = GetAllViews(selectedLinkedDoc);
            List<string> sourceViewNames = new List<string>();
            foreach (View view in allViews)
            {
                string name = $"{view.Name} [{view.ViewType}]";
                sourceViewNames.Add(name);
            }

            // Insert views to Source View
            cbb_SourceView.ItemsSource = sourceViewNames;
            this.sourceViewNames = sourceViewNames;
            if (sourceViewNames.Count > 0) cbb_SourceView.SelectedIndex = 0;
        }

        private List<View> GetAllViews(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> views = collector.OfCategory(BuiltInCategory.OST_Views).ToElements();
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

        public List<Element> GetAllFilterInModel(Document doc)
        {
            List<Element> filterList = new List<Element>();
            IList<Element> allFilters = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).ToElements();
            foreach (Element filter in allFilters)
            {
                filterList.Add(filter);
            }

            return filterList;
        }

        private Element CreateFilter(Document doc, Element sourceFilter)
        {
            ParameterFilterElement filter = sourceFilter as ParameterFilterElement;

            string filterName = filter.Name;
            ICollection<ElementId> filterCategories = filter.GetCategories();
            LogicalAndFilter filterRule = (LogicalAndFilter)filter.GetElementFilter();

            try
            {
                using (Transaction t = new Transaction(doc, "Create Filter"))
                {
                    t.Start();
                    Element newFilter = ParameterFilterElement.Create(doc, filterName, filterCategories, filterRule);
                    t.Commit();
                    return newFilter;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private void CopyFilterToViewFromLinked(Document doc, View view, Element sourceFilter, Element newFilter)
        {
            using (Transaction t = new Transaction(doc, "Copy Filter From Linked Model"))
            {
                t.Start();
                bool filterStatus = selectView.GetFilterVisibility(sourceFilter.Id);
                OverrideGraphicSettings filterOverride = selectView.GetFilterOverrides(sourceFilter.Id);
                try
                {
                    view.SetFilterVisibility(newFilter.Id, filterStatus);
                    view.SetFilterOverrides(newFilter.Id, filterOverride);
                    //MessageBox.Show(newFilter.Id.GetType().ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    //MessageBox.Show("Error in CopyFilterToViewFromLinked method");
                }
                t.Commit();
            }
        }

        private void cbb_SourceView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedViewName = cbb_SourceView.SelectedItem as string;
            View selectView = GetViewByName(selectedLinkedDoc, selectedViewName);
            this.selectView = selectView;

            List<Element> filtersInView = GetFiltersInView(selectedLinkedDoc, selectView);
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
                    Element filter = GetFilterByName(selectedLinkedDoc, filterName);
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
                        View destinationView = GetViewByName(activeDoc, destinationViewName);
                        destinationViews.Add(destinationView);
                    }

                    //Execute
                    using (TransactionGroup tg = new TransactionGroup(activeDoc, "Copy Filter From Linked Model"))
                    {
                        tg.Start();

                        // Create similar filters only if they don't exist
                        List<Element> allFiltersInDoc = GetAllFilterInModel(activeDoc);
                        HashSet<string> filterInDocNames = new HashSet<string>(allFiltersInDoc.Select(f => f.Name));

                        List<Element> newFilters = new List<Element>();
                        foreach (Element filter in sourceFilters)
                        {
                            // Check if the filter already exists in activeDoc
                            if (!filterInDocNames.Contains(filter.Name))
                            {
                                // If not, create a new filter
                                Element newFilter = CreateFilter(activeDoc, filter);
                                newFilters.Add(newFilter);
                            }
                            else
                            {
                                // If filter exists, use the existing one
                                Element existingFilter = allFiltersInDoc.FirstOrDefault(f => f.Name == filter.Name);
                                if (existingFilter != null)
                                {
                                    newFilters.Add(existingFilter);
                                }
                            }
                        }


                        if (CheckExistingFilter(activeDoc, destinationViews, sourceFilters))
                        {
                            OverrideConfirmationWindow confirmationWindow = new OverrideConfirmationWindow();

                            // Execute when user hit Yes button
                            if (confirmationWindow.ShowDialog() == true && confirmationWindow.UserResponse)
                            {
                                try
                                {
                                    foreach (View view in destinationViews)
                                    {
                                        foreach (Element sourceFilter in sourceFilters)
                                        {
                                            foreach (Element newFilter in newFilters)
                                            {
                                                if (sourceFilter.Name == newFilter.Name)
                                                {
                                                    CopyFilterToViewFromLinked(activeDoc, view, sourceFilter, newFilter);
                                                }
                                            }

                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
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
                                bool allFilterExist = true;

                                foreach (View view in destinationViews)
                                {
                                    List<Element> filtersInView = GetFiltersInView(activeDoc, view);

                                    // Create a hashset containing existing filters in destination views
                                    HashSet<string> existingFilterNames = new HashSet<string>();
                                    foreach (Element existingFilter in filtersInView)
                                    {
                                        existingFilterNames.Add(existingFilter.Name);
                                    }

                                    // Check and copy non-existing filters
                                    
                                    foreach (Element sourceFilter in sourceFilters)
                                    {
                                        if (!existingFilterNames.Contains(sourceFilter.Name))
                                        {
                                            foreach (Element newFilter in newFilters)
                                            {
                                                if (sourceFilter.Name == newFilter.Name)
                                                {
                                                    CopyFilterToViewFromLinked(activeDoc, view, sourceFilter, newFilter);
                                                    allFilterExist = false;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (allFilterExist)
                                {
                                    MessageBox.Show("All filters with the same properties already existing in the view. Please select different filters !","Notification");
                                    return;
                                }
                            }

                        }
                        else
                        {
                            try
                            {
                                foreach (View view in destinationViews)
                                {
                                    foreach (Element sourceFilter in sourceFilters)
                                    {
                                        foreach (Element newFilter in newFilters)
                                        {
                                            if (sourceFilter.Name == newFilter.Name)
                                            {
                                                CopyFilterToViewFromLinked(activeDoc, view, sourceFilter, newFilter);
                                            }
                                        }

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }



                        // Notify the result and close the program
                        MessageBox.Show("Copy Filters To Views Successfully !","Result");
                        Close();
                        tg.Assimilate();
                    }

                }
                else
                {
                    MessageBox.Show("No destination views selected. Please select again!","Warning");
                    return;
                }


            }
            else
            {
                MessageBox.Show("No filters selected. Please select again!","Warning");
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

        private void bt_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

        private void LinkedModelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string linkedName = LinkedModelTextBox.Text.ToLower();
            // Filter the list based on the input
            var linkedNames = linkName.Where(name => name.ToLower().Contains(linkedName)).ToList();
            // Update ListBox with the filtered results
            cbb_LinkedModel.ItemsSource = linkedNames;
            if (linkName.Count > 0) cbb_LinkedModel.SelectedValue = linkedNames[0];
        }

    }
}
