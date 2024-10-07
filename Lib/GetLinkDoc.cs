using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinhTranTools
{

    public class GetLinkDoc
    {
        public static List<Document> GetLinkedDoc(Document doc)
        {
            List<RevitLinkInstance> linkInstances = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().ToList();
            List<Document> linkDoc = new List<Document>();
            if (linkInstances.Count > 0)
            {
                foreach (RevitLinkInstance link in linkInstances)
                {
                    linkDoc.Add(link.GetLinkDocument());
                }
            }
            else { return null; }
            return linkDoc;
        }

        public static List<string> GetLinkedDocName(Document doc)
        {
            List<RevitLinkInstance> linkInstances = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().ToList();
            List<string> linkName = new List<string>();

            if (linkInstances.Count > 0)
            {
                foreach (RevitLinkInstance link in linkInstances)
                {
                    linkName.Add(link.Name);
                }
            } else { return null; }

            return linkName;
        }
    }
}
