using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.Common.Shared.Utils.SqlUtils
{
    internal class OutputParameter
    {
        public object Value { get; set; }
        public string Name { get; set; }
        public Type Type { get; set; }

        public OutputParameter(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}
