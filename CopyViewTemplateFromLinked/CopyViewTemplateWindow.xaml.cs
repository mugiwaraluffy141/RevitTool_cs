using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;




namespace MinhTranTools.View_Template
{
    public partial class CopyViewTemplateWindow : Window
    {
        private Document activeDoc;
        private Document selectedLinkedDoc;
        private List<string> linkName;
        private List<Item> listTemplates;

        public CopyViewTemplateWindow(Document doc)
        {
            InitializeComponent();
            activeDoc = doc;

            // Insert linked documents to cbb_LinkedModel
            List<string> linkName = GetLinkDoc.GetLinkedDocName(doc);
            cbb_LinkedModel.ItemsSource = linkName;
            this.linkName = linkName;
            if (linkName.Count > 0) cbb_LinkedModel.SelectedIndex = 0;
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

        private List<View> GetViewTemplate(Document doc)
        {
            List<View> listViews = GetAllViews(doc);
            List<View> allViewTempaltes = new List<View>();
            foreach (View view in listViews)
            {
                if (view.IsTemplate)
                {
                    allViewTempaltes.Add(view);
                }
            }
            return allViewTempaltes;
        }

        private View GetViewTemplateByName(Document doc, string templateName)
        {
            List<View> allViewsInModel = GetAllViews(doc);
            List<View> allViewTemplatesInModel = GetViewTemplate(doc);

            foreach (View viewTemplate in allViewTemplatesInModel)
            {
                string name = viewTemplate.Name;
                if (name == templateName)
                {
                    return viewTemplate;
                }
            }
            MessageBox.Show($"There is no view template with {templateName} name in the model.");
            return null;
        }

        private void cbb_LinkedModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<Document> linkDoc = GetLinkDoc.GetLinkedDoc(activeDoc);
            int selectedIndex = cbb_LinkedModel.SelectedIndex;
            Document selectedLinkedDoc = linkDoc[selectedIndex];
            this.selectedLinkedDoc = selectedLinkedDoc;

            List<View> allViewsInDoc = GetAllViews(selectedLinkedDoc);
            List<View> allViewTemplatesInDoc = GetViewTemplate(selectedLinkedDoc);

            List<string> viewTemplateNames = new List<string>();
            foreach(View viewTemplate in allViewTemplatesInDoc)
            {
                string name = viewTemplate.Name;
                viewTemplateNames.Add(name);
            }

            var listTemplates = viewTemplateNames.Select(x => new Item(x, false)).ToList();
            this.listTemplates = listTemplates;
            lv_ViewTemaPlates.ItemsSource = listTemplates;
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
                if (lv_ViewTemaPlates.Items.Contains(checkBox.DataContext))
                {
                    ShiftToCheck(lv_ViewTemaPlates, checkBox);
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

        private void bt_All_Click(object sender, RoutedEventArgs e)
        {
            SelectAll(lv_ViewTemaPlates, true);
        }

        private void bt_None_Click(object sender, RoutedEventArgs e)
        {
            SelectAll(lv_ViewTemaPlates, false);
        }

        private void bt_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyTemplates(List<View> validTemplates, List<View> invalidTemplates = null)
        {
            ICollection<ElementId> copiedElementIds = null;
            ICollection<ElementId> elementIds = validTemplates.Select(vt => vt.Id).ToHashSet();

            using (Transaction t = new Transaction(activeDoc, "Copy View Templates"))
            {
                try
                {
                    t.Start();
                    copiedElementIds = ElementTransformUtils.CopyElements(selectedLinkedDoc, elementIds, activeDoc, null, null);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }

            StringBuilder messageBuilder = new StringBuilder();
            if (copiedElementIds?.Any() == true)
            {
                messageBuilder.AppendLine("The view templates below have been copied:");
                foreach (var elementId in copiedElementIds)
                {
                    var element = activeDoc.GetElement(elementId);
                    if (element != null) messageBuilder.AppendLine(element.Name);
                }
            }
            else
            {
                MessageBox.Show("Copy View Templates Failed!", "Result");
                return;
            }

            if (invalidTemplates?.Any() == true)
            {
                messageBuilder.AppendLine("-------------------------------------------------------------------------------------------");
                messageBuilder.AppendLine("The view templates below have not been copied because they already exist:");
                foreach (var invalid in invalidTemplates)
                {
                    messageBuilder.AppendLine(invalid.Name);
                }
            }

            MessageBox.Show(messageBuilder.ToString(), "Result");
            Close();
        }

        private void bt_Ok_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = GetCheckedItems(lv_ViewTemaPlates);
            var viewTemplatesInLinked = selectedItem
                .Select(item => GetViewTemplateByName(selectedLinkedDoc, item.ToString()))
                .Where(template => template != null)
                .ToList();

            if (!viewTemplatesInLinked.Any())
            {
                MessageBox.Show("No view templates selected. Please select again.", "Error");
                return;
            }

            var viewTemplatesInActive = GetViewTemplate(activeDoc);
            if (viewTemplatesInActive == null || !viewTemplatesInActive.Any())
            {
                CopyTemplates(viewTemplatesInLinked);
                return;
            }

            var validViewTemplates = viewTemplatesInLinked
                .Where(linkedTemplate => !viewTemplatesInActive.Any(activeTemplate => activeTemplate.Name == linkedTemplate.Name))
                .ToList();

            var invalidViewTemplates = viewTemplatesInLinked
                .Where(linkedTemplate => viewTemplatesInActive.Any(activeTemplate => activeTemplate.Name == linkedTemplate.Name))
                .ToList();

            if (!validViewTemplates.Any())
            {
                MessageBox.Show("All the selected view templates already exist in the active model. Please select again!", "Result");
                return;
            }

            CopyTemplates(validViewTemplates, invalidViewTemplates);
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

        private void ViewTemplateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = ViewTemplateTextBox.Text.ToLower();
            // Filter the list based on the input
            var filteredNames = listTemplates.Where(item => item.Name.ToLower().Contains(filter)).ToList();
            // Update ListBox with the filtered results
            lv_ViewTemaPlates.ItemsSource = filteredNames;
        }
    }
}
