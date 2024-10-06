using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MinhTranTools.View_Template;

namespace MinhTranTools
{
    [Transaction (TransactionMode.Manual)]
    internal class CopyViewTemplateCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            CopyViewTemplateWindow window = new CopyViewTemplateWindow (doc);
            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
