using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinhTranTools
{
    public class Item
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public Item(string name, bool isChecked)
        {
            Name = name;
            IsChecked = isChecked;
        }
    }
}
