using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayVisualStudioIntegration.UI
{
    public class OptionsPageGrid : DialogPage, IOptions
    {
        [Category("Replay")]
        [DisplayName("Replay.exe Location")]
        [Description("The fully qualified path to Replay.exe")]
        public string ReplayLocation { get; set; } = @"C:\Program Files\Replay\Replay.exe";
    }
}
