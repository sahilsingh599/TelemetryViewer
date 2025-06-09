using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryViewer.Models
{
    public class LapFileEntry
    {
        public string FilePath { get; set; }
        public string DisplayName { get; set; }

        public override string ToString() => DisplayName;
    }
}
