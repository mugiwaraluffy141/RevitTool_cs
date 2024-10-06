using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MinhTranTools.CopyFilterActiveModel;


namespace MinhTranTools
{
    [Transaction(TransactionMode.Manual)]
    internal class CopyFilterActiveModelCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            CopyFilterActiveModelWindow window = new CopyFilterActiveModelWindow(doc);
            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}