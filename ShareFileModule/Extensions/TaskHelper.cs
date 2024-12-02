using System.Threading.Tasks;
using System.Windows.Threading;

namespace ShareFile.Api.Powershell.Extensions
{
    public static class TaskHelper
    {
        public static void RunSynchronously(this Task task)
        {
            var frame = new DispatcherFrame();
            using (task)
            {
                task.ContinueWith(t => frame.Continue = false);

                frame.Continue = true;
                Dispatcher.PushFrame(frame);

                if (task.Exception != null)
                {
                    throw task.Exception;
                }
            }
        }

        public static T RunSynchronously<T>(Task<T> task)
        {
            var frame = new DispatcherFrame();
            using (task)
            {
                task.ContinueWith(t => frame.Continue = false);

                frame.Continue = true;
                Dispatcher.PushFrame(frame);
                return task.Result;
            }
        }
    }
}