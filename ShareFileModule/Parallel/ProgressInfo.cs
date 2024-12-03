using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareFile.Api.Powershell.Parallel
{
    class ProgressInfo
    {
        public int FileIndex { get; set; }
        public ProgressDoneDelegate ProgressTransferred { get; set; }
        public ProgressTotalDelegate ProgressTotal { get; set; }
        public long Transferred { get; set; }
        public long Total { get; set; }
        public bool Completed { get; set; }
    }
}
