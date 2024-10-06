using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MinhTranTools.CopyFilterFromLinked;


namespace MinhTranTools
{
    [Transaction(TransactionMode.Manual)]
    internal class CopyFilterFromLinkedCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            CopyFilterFromLinkedWindow window = new CopyFilterFromLinkedWindow(doc);
            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
