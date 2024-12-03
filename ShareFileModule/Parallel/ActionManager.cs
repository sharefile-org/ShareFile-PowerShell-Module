using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShareFile.Api.Powershell.Parallel
{
    delegate void ProgressDoneDelegate(int index, long done);
    delegate void ProgressTotalDelegate(int index, long total);

    /// <summary>
    /// ActionManager class to start parallel copying operations
    /// </summary>
    class ActionManager
    {
        private Queue<IAction> ActionsQueue;

        private ProgressDoneDelegate ProgressDone;
        private ProgressTotalDelegate ProgressTotal;

        private PSCmdlet CmdLetObj;
        private Dictionary<int, ProgressInfo> ProgressInfoList;

        private object LockTransferred;
        private object LockTotal;

        private string folderName;

        /// <summary>
        /// Initialize
        /// </summary>
        public ActionManager(PSCmdlet cmdLetObj, string name)
        {
            this.ActionsQueue = new Queue<IAction>();

            this.ProgressDone = new ProgressDoneDelegate(UpdateCurrent);
            this.ProgressTotal = new ProgressTotalDelegate(UpdateTotal);
            this.LockTransferred = new object();
            this.LockTotal = new object();

            this.CmdLetObj = cmdLetObj;

            this.folderName = name;

            this.ProgressInfoList = new Dictionary<int, ProgressInfo>();
        }

        /// <summary>
        /// Add action to list
        /// </summary>
        /// <param name="action">Action type: UploadAction or DownloadAction</param>
        internal void AddAction(IAction action)
        {
            this.ActionsQueue.Enqueue(action);
        }

        /// <summary>
        /// Execute all actions list in parallel (threads)
        /// </summary>
        internal void Execute()
        {
            int remainingCounter = ActionsQueue.Count;

            if (remainingCounter > 0)
            {
                int maxParallelThreads = System.Environment.ProcessorCount * 2;
                int runningThreads = 0;
                int threadIndex = 1;

                while (remainingCounter > 0)
                {
                    // if actions queue is not empty and current running threads are less than the allowed max parallel threads count
                    if (ActionsQueue.Count > 0 && runningThreads < maxParallelThreads)
                    {
                        Interlocked.Increment(ref runningThreads);
                        IAction downloadAction = ActionsQueue.Dequeue();
                        Task t = Task.Factory.StartNew(async () =>
                        {
                            for (int i=1; i <= 7; i++)
                            {
                                try
                                {
                                    ProgressInfo fileProgressInfo = new ProgressInfo();
                                    fileProgressInfo.ProgressTransferred = this.ProgressDone;
                                    fileProgressInfo.ProgressTotal = this.ProgressTotal;
                                    fileProgressInfo.FileIndex = threadIndex;

                                    ProgressInfoList.Add(threadIndex++, fileProgressInfo);
                                    if (i > 1)
                                    {
                                        // This means that this is a retry. In that case force the operation
                                        // Otherwise file already exists error is thrown
                                        downloadAction.OpActionType = ActionType.Force;
                                        downloadAction.CopyFileItem(fileProgressInfo);
                                    }
                                    else
                                    {
                                        downloadAction.CopyFileItem(fileProgressInfo);
                                    }
                                    // Task completed, break out of the loop.            
                                    Log.Logger.Instance.Debug("Action on Filename : "+downloadAction.FileName+" got completed in "+i+" try");
                                    break;
                                }
                                catch (AggregateException tce)
                                {
                                    // This means ShareFile Client Api has cancelled the task.
                                    // Retry the operation.
                                    Log.Logger.Instance.Error(tce.Message + " Upload/Download action for " + downloadAction.FileName + "\n" + tce.StackTrace);

                                    if (i == 7)
                                    {
                                        // No need to wait
                                        break;
                                    }
                                    // Wait for sometime before retrying the operation
                                    double timeToWait = Math.Pow(2, i);
                                    await Task.Delay(TimeSpan.FromSeconds(timeToWait));
                                }
                                catch (Exception e)
                                {
                                    // This means operation failed due to some other reasons.
                                    // No need to retry, break out of the loop.
                                    Log.Logger.Instance.Error(e.Message + "\n" + e.StackTrace);
                                    break;
                                }
                            }                            
                            Interlocked.Decrement(ref remainingCounter);
                            Interlocked.Decrement(ref runningThreads);
                        });
                    }

                    UpdateProgress();
                    Thread.Sleep(1000);
                }


                UpdateProgress();
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Get total number of actions
        /// </summary>
        public int TotalActions
        {
            get
            {
                return ActionsQueue.Count;
            }
        }

        private void UpdateCurrent(int index, long d)
        {
            lock (LockTransferred)
            {
                ProgressInfoList[index].Transferred = d;
            }
        }

        private void UpdateTotal(int index, long t)
        {
            lock (LockTotal)
            {
                ProgressInfoList[index].Total = t;
            }
        }

        private void UpdateProgress()
        {
            try
            {
                long total = 0, done = 0;
                int length = this.ProgressInfoList.Count;

                for (int i = 0; i < length; i++)
                {
                    ProgressInfo p = this.ProgressInfoList.Values.ElementAt(i);
                    total += p.Total;
                    done += p.Transferred;
                }

                if (total == 0)
                {
                    return;
                }

                int percentComplete = (int)(done * 100 / total);
                ProgressRecord progress = new ProgressRecord(1,
                    string.Format("Copying '{0}'", this.folderName),
                    string.Format("{0}% of {1}", percentComplete, GetSize(total)));

                progress.PercentComplete = percentComplete;
                //progress.CurrentOperation = "Downloading files";
                //progress.StatusDescription = done + "/" + total;

                //progress.StatusDescription = string.Format("{0}% of {1}", percentComplete, GetSize(total));

                this.CmdLetObj.WriteProgress(progress);
            }
            catch { }
        }

        private string GetSize(long total)
        {
            if (total > 1048576)
            {
                return string.Format("{0} MB", this.GetSizeRound(total, 1048576));
            }
            else if (total > 1024)
            {
                return string.Format("{0} KB", this.GetSizeRound(total, 1024));
            }
            else
            {
                return "1KB";
            }
        }

        private string GetSizeRound(long total, long divider)
        {
            return Math.Round((double)total / divider, 2).ToString();
        }
    }
}
