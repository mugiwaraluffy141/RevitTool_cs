using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MinhTranTools.Sheet.PlaceViewOnSheetFromExcel;

namespace MinhTranTools
{
    [Transaction(TransactionMode.Manual)]
    internal class PlaceViewOnSheetCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            PlaceViewOnSheetWindow window = new PlaceViewOnSheetWindow(doc);
            window.ShowDialog();


            return Result.Succeeded;
        }
    }
}
