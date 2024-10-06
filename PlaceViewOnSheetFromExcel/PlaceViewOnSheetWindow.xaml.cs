using Autodesk.Revit.DB;
using ExcelDataReader;
using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Window = System.Windows.Window;


namespace MinhTranTools.Sheet.PlaceViewOnSheetFromExcel
{
    /// <summary>
    /// Interaction logic for PlaceViewOnSheetWindow.xaml
    /// </summary>
    
    public partial class PlaceViewOnSheetWindow : Window
    {
        private Document doc { get; set; }

        public PlaceViewOnSheetWindow(Document doc)
        {
            InitializeComponent();
            this.doc = doc;
        }

        private void bt_Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select Excel File";
            dialog.Filter = "Excel Files| *xls; *xlsx; *xlsm";
            if (dialog.ShowDialog() == true)
            {
                tb_FilePath.Text = dialog.FileName;
            }
        }

        private void bt_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void bt_Ok_Click(object sender, RoutedEventArgs e)
        {
            string filePath = tb_FilePath.Text;


            if (File.Exists(filePath))
            {

                if (filePath.Contains("\"")) filePath = filePath.Replace("\"", "");

                System.Data.DataTable excelData = new System.Data.DataTable();
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(fileStream))
                    {
                        var data = reader.AsDataSet();
                        if (data != null) excelData = data.Tables[0];
                    }
                }

                //Continue Here
                var viewTemplateInExcel = new List<string>();
                int columnCount = excelData.Columns.Count;
                for (int i = 1; i < columnCount; i++)
                {
                    string viewName = excelData.Rows[1][i].ToString();
                    viewTemplateInExcel.Add(viewName);
                }

                var templateSheet = GetSheetBySheetNumber(doc, excelData.Rows[1][0].ToString());
                if  (templateSheet != null)
                {
                    // Inputs for execution
                    var sheetInExcel = GetSheetFromExcel(doc, excelData);
                    var viewInExcel = GetViewFromExcel(doc, excelData);

                    var templateViewLocation = GetTemplateViewportLocation(doc, templateSheet, viewTemplateInExcel);
                    var templateViewType = GetTemplateViewportType(doc, templateSheet, viewTemplateInExcel);
                    var templateviewLocationList = Enumerable.Repeat(templateViewLocation, sheetInExcel.Count).ToList();
                    var templateViewTypeList = Enumerable.Repeat(templateViewType, sheetInExcel.Count).ToList();

                    // Test input


                    // Execute
                    bool isExecuted = PlaceView(doc, sheetInExcel, viewInExcel, templateviewLocationList, templateViewTypeList);

                    // Check the execution and return the result
                    if (isExecuted)
                    {
                        MessageBox.Show("Place View On Sheet Successfully!", "Result");
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Unsuccessfully! Please check your Excel file again!", "Error");
                        Close();
                    }

                }
                else
                {
                    MessageBox.Show("Unsuccessfully! Please check your Excel file again!", "Error");
                    Close();
                }
                

            }
            else
            {
                MessageBox.Show("No file path selected. Please select again!", "Error");
            }

        }

        private List<ViewSheet> GetAllSheetInDoc(Document doc)
        {
            List<ViewSheet> elements = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Sheets).WhereElementIsNotElementType().OfType<ViewSheet>().ToList();
            return elements;
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

        private ViewSheet GetSheetBySheetNumber(Document doc, string sheetNumber)
        {
            var allSheets = GetAllSheetInDoc(doc);
            var sheet = allSheets.FirstOrDefault(s => s.SheetNumber == sheetNumber);
            return sheet;
        }

        private List<ViewSheet> GetSheetFromExcel(Document doc, System.Data.DataTable dataTable)
        {
            List<ViewSheet> sheetInExcel = new List<ViewSheet>();
            int rowCount = dataTable.Rows.Count;
            for (int i = 2; i < rowCount; i++)
            {
                DataRow row = dataTable.Rows[i];
                string sheetNumber = row[0].ToString();
                var sheet = GetSheetBySheetNumber(doc, sheetNumber);
                if (sheet != null)
                {
                    sheetInExcel.Add(sheet);
                }
            }

            if (sheetInExcel.Count > 0)
            {
                return sheetInExcel;
            }
            else
            {
                return new List<ViewSheet>();
            }
        }

        private List<List<View>> GetViewFromExcel(Document doc, System.Data.DataTable dataTable)
        {
            var allViews = GetAllViews(doc);
            var viewsInExcel = new List<List<View>>();
            int rowCount = dataTable.Rows.Count;
            int columnCount = dataTable.Columns.Count;

            for (int i = 2; i < rowCount; i++)
            {
                DataRow row = dataTable.Rows[i];
                var tempList = new List<View>();

                for (int j = 1; j < columnCount; j++)
                {
                    bool viewFound = false;

                    foreach (View view in allViews)
                    {
                        if (row[j].ToString() == view.Name)
                        {
                            tempList.Add(view);
                            viewFound = true;
                            break;
                        }
                    }

                    if (!viewFound)
                    {
                        tempList.Add(null);
                    }
                }

                viewsInExcel.Add(tempList);
            }

            return viewsInExcel;
        }

        private List<XYZ> GetTemplateViewportLocation(Document doc, ViewSheet templateSheet, List<string> viewTemplateInExcel)
        {
            var viewportLocation = new List<XYZ>();
            var viewportOnSheet = templateSheet.GetAllViewports().Select(id => doc.GetElement(id)).Cast<Viewport>();

            foreach (string viewTemplateName in viewTemplateInExcel)
            {
                bool viewFound = false;

                foreach (Viewport viewport in viewportOnSheet)
                {
                    ElementId viewId = viewport.ViewId;
                    string viewName = doc.GetElement(viewId).Name;

                    if (viewTemplateName == viewName)
                    {
                        viewportLocation.Add(viewport.GetBoxCenter());
                        viewFound = true;
                        break;
                    }
                }

                if (!viewFound)
                {
                    viewportLocation.Add(null);
                }
            }

            return viewportLocation;
        }

        private List<ElementId> GetTemplateViewportType(Document doc, ViewSheet templateSheet, List<string> viewTemplateInExcel)
        {
            var viewportType = new List<ElementId>();
            var viewportOnSheet = templateSheet.GetAllViewports().Select(id => doc.GetElement(id));
            foreach (string viewName in viewTemplateInExcel)
            {
                bool viewFound = false;
                foreach (Viewport viewport in viewportOnSheet)
                {
                    ElementId viewId = viewport.ViewId;
                    if (doc.GetElement(viewId).Name == viewName)
                    {
                        viewportType.Add(viewport.GetTypeId());
                        viewFound = true;
                        break;
                    }
                }
                if (!viewFound)
                {
                    viewportType.Add(null);
                }
            }
            return viewportType;
        }


        private bool PlaceView(Document doc, List<ViewSheet> sheetInExcel, List<List<View>> viewInExcel, List<List<XYZ>> templateviewLocationList, List<List<ElementId>> templateViewTypeList)
        {
            bool anyPlacedSuccessfully = false; // Chỉ cần có 1 lần đặt view thành công thì giá trị này sẽ là true

            using (TransactionGroup tg = new TransactionGroup(doc, "Place View On Sheet"))
            {
                tg.Start();
                for (int i = 0; i < sheetInExcel.Count; i++)
                {
                    var sheet = sheetInExcel[i];
                    var viewList = viewInExcel[i];
                    var locationList = templateviewLocationList[i];
                    var viewportTypeList = templateViewTypeList[i];

                    using (Transaction t = new Transaction(doc, "Place Views On Sheet"))
                    {
                        t.Start();

                        for (int j = 0; j < viewList.Count; j++)
                        {
                            var view = viewList[j];
                            var location = locationList[j];
                            var viewport = viewportTypeList[j];

                            try
                            {
                                // Nếu view, location hoặc viewport là null, bỏ qua
                                if (view == null || location == null || viewport == null)
                                {
                                    continue; // Bỏ qua trường hợp không hợp lệ
                                }

                                // Thực hiện việc đặt view lên sheet
                                Viewport.Create(doc, sheet.Id, view.Id, location)
                                        .get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM)
                                        .Set(viewport);

                                anyPlacedSuccessfully = true; // Đánh dấu có ít nhất một lần đặt thành công
                            }
                            catch
                            {
                                // Bỏ qua lỗi nhưng không thay đổi biến anyPlacedSuccessfully
                            }
                        }

                        t.Commit();
                    }
                }
                tg.Assimilate();
            }

            return anyPlacedSuccessfully;
        }


    }
}
