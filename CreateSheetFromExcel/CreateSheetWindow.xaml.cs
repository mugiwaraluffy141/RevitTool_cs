using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ExcelDataReader;
using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Window = System.Windows.Window;
using Excel = Microsoft.Office.Interop.Excel;
using System.Windows;

namespace CreateSheet
{
    /// <summary>
    /// Interaction logic for CreateSheetWindow.xaml
    /// </summary>
    public partial class CreateSheetWindow : Window
    {
        private Document document;
        public CreateSheetWindow(Document doc)
        {
            InitializeComponent();
            document = doc;
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

        private List<Family> GetAllFamilyTitleBlocks(Document document)
        {
            List<Family> allTitleBlocks = new List<Family>();
            List<Family> allFamilies = new FilteredElementCollector(document)
                                        .OfClass(typeof(Family))
                                        .Cast<Family>()
                                        .ToList();
            foreach (Family family in allFamilies)
            {
                if (family.FamilyCategory.BuiltInCategory == BuiltInCategory.OST_TitleBlocks)
                {
                    allTitleBlocks.Add(family);
                };
            }
            if (allTitleBlocks.Count > 0) { return allTitleBlocks; }
            else return null;
        }

        private List<string> GetTitleBlockNames(List<Family> allTiTleBlocks)
        {
            List<string> titleBlockNames = new List<string>();
            foreach (Family family in allTiTleBlocks)
            {
                string name = family.Name;
                titleBlockNames.Add(name);
            }
            return titleBlockNames;
        }

        private ElementId GetTitleBlockIdByName (string familyName, string typeName)
        {
            List<Family> allTitleBlocks = GetAllFamilyTitleBlocks(document);
            foreach (Family family in allTitleBlocks) 
            {
                string name = family.Name;
                if (name == familyName)
                {
                    ISet<ElementId> familySymbolIds = family.GetFamilySymbolIds();
                    foreach (ElementId elementId in familySymbolIds)
                    {
                        FamilySymbol familyType = (FamilySymbol)document.GetElement(elementId);
                        string symbolName = familyType.Name;
                        if (symbolName == typeName) return elementId;
                    }
                }
            }
            return null;
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

                        // In case ignore first row

                        //var data = reader.AsDataSet(new ExcelDataSetConfiguration()

                        //{
                        //    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        //    {
                        //        UseHeaderRow = true // Firt row is a title
                        //    }
                        //});

                        if (data != null) excelData = data.Tables[0];
                    }
                }
                CloseExcelFile(filePath);
                CreateSheet(excelData);
            }
            else
            {
                MessageBox.Show("Select a file path or copy/paste the file path", "Message");
            }
        }

        private void CreateSheet(System.Data.DataTable dataTable)
        {
            using (Transaction t = new Transaction(document, "Create Sheet From Excel"))
            {
                t.Start();
                int rowCount = dataTable.Rows.Count;
                int columnCount = dataTable.Columns.Count;
                int count = 0;
                for (int i = 1;  i < rowCount; i++)
                {
                    try
                    {
                        DataRow row = dataTable.Rows[i];
                        string familyName = row[0].ToString();
                        string typeName = row[1].ToString();
                        ElementId titleBlockId = GetTitleBlockIdByName(familyName, typeName);
                        ViewSheet newSheet = ViewSheet.Create(document, titleBlockId);
                        for (int j = 2; j< columnCount; j++)
                        {
                            string header = dataTable.Columns[j].ColumnName;
                            string paramName = dataTable.Rows[0][header].ToString();
                            string paramValue = dataTable.Rows[i][header].ToString();
                            SetParameterValueByName(newSheet, paramName, paramValue);
                        }
                        count++;
                    }
                    catch { }
                }
                t.Commit();
                Close();
                MessageBox.Show($"{count} sheets have been created.", "Result");
            }
        }

        private void SetParameterValueByName(Element element, string paramName, string paramValue)
        {
            try
            {
                Autodesk.Revit.DB.Parameter param = element.LookupParameter(paramName);
                if (param != null && !param.IsReadOnly)
                {
                    var paraType = param.StorageType;
                    if (paraType == StorageType.Integer)
                    {
                        param.Set(int.Parse(paramValue));
                    }
                    else if (paraType == StorageType.Double)
                    {
                        param.Set(double.Parse(paramValue));
                    }
                    else if (paraType == StorageType.String)
                    {
                        param.Set(paramValue);
                    }
                }

            }
            catch { }

        }

        private void CloseExcelFile(string filePath)
        {
            Excel.Application app = new Excel.Application();
            if (app != null)
            {
                foreach (Workbook workbook in app.Workbooks)
                {
                    if (workbook.FullName == filePath)
                    {
                        workbook.Close(false);
                        break;
                    }
                }
                Marshal.ReleaseComObject(app);
            }
        }
    }
}
