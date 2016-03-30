using System.ComponentModel;

namespace IotUiApp.Utils
{
    class BackgroundTaskUtils
    {

        public delegate void MyTask();
        public delegate void MyTaskWithParam(string param);

        public static void RunBackgroundTask(MyTask task)
        {
            BackgroundWorker bw = new BackgroundWorker();

            // send task and process it in background
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                task();

            });
            bw.RunWorkerAsync();
        }

        public static void RunBackgroundTaskWithParam(string param, MyTaskWithParam task, MyTask completed)
        {
            BackgroundWorker bw = new BackgroundWorker();

            // send task and process it in background
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                task(param);

            });
            //when worker completes its task execute completed function
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                completed();
            });
            bw.RunWorkerAsync();
        }
    }
}
