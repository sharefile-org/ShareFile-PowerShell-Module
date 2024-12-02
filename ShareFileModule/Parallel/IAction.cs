using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareFile.Api.Powershell.Parallel
{
    /// <summary>
    /// IAction Interface
    /// </summary>
    interface IAction
    {
        /// <summary>
        /// Copy File Item method to upload/download files
        /// </summary>
        void CopyFileItem(ProgressInfo progressInfo);
        ActionType OpActionType { get; set; }
        string FileName { get; }
    }
}
