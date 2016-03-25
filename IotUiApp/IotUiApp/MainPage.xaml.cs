/* license-start
 * 
 * Copyright (C) 2008 - 2015 Crispico Resonate, <http://www.crispico.com/>.
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation version 3.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details, at <http://www.gnu.org/licenses/>.
 * 
 * license-end
 */

using System;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System.ComponentModel;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IotUiApp
{

    public sealed partial class MainPage : Page
    {
        private static string audioTempCommand;

        public MainPage()
        {
            this.InitializeComponent();
            MyWebView.ScriptNotify += webView_ScriptNotify;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Uri u = new Uri("ms-appx-web:///Assets/views/page.html");
            MyWebView.Navigate(u);
        }



        void LoadCompleted(object sender, NavigationEventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            RunBackgroundTaskWithParam("1", UpdateValuesFromPartitionTask, DoesNothing);
            RunBackgroundTaskWithParam("2", UpdateValuesFromPartitionTask, DoesNothing);
            RunBackgroundTask(CheckAudioTemperatureCommandTask);

        }

        async void webView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            string value = e.Value;
            if (value.Contains("temperature="))
            {
                var tempValueCommand = value;
                SendCommandMechanism(tempValueCommand);
            }else if(value == "enable_speech")
            {
                SpeechRecognitionEngine.startSpeechRecognitionMechanism();
            }

        }
        public void SendCommandMechanism(string tempValueCommand)
        {
            RunBackgroundTaskWithParam(tempValueCommand, SendCloudToDevice.sendCommand, UpdateCommandTimestampTask);

        }


        public void InvokeJSScript(string scriptName, string[] parameters)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MyWebView.InvokeScriptAsync(scriptName, parameters);
            });

        }

        public delegate void MyTask();
        public delegate void MyTaskWithParam(string param);

        public void RunBackgroundTask(MyTask task)
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

        public void RunBackgroundTaskWithParam(string param, MyTaskWithParam task, MyTask completed)
        {
            BackgroundWorker bw = new BackgroundWorker();

            // send task and process it in background
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                task(param);

            });
            //when worker completes its task notify user inerface by invoking a javascript 
            //method that updates command timestamp in the ui
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                completed();
            });
            bw.RunWorkerAsync();
        }





        public void DoesNothing()
        {

        }

        public void CheckAudioTemperatureCommandTask()
        {
            while (true)
            {
                if (audioTempCommand != null)
                {
                    SendCommandMechanism(audioTempCommand);
                    audioTempCommand = null;
                }
            }
        }
        public void UpdateCommandTimestampTask()
        {
            InvokeJSScript("updateCommandTimestamp", new string[] { DateTime.Now.ToString("HH:mm:ss") });
        }

        public void UpdateValuesFromPartitionTask(string partition)
        {
            string message = ReadDeviceToCloud.getMessage(partition);
            ReceivedData receivedData;
            if (message != null)
            {
                receivedData = JsonConvert.DeserializeObject<ReceivedData>(message);
                InvokeJSScript("updateValues", new string[] { receivedData.temperature.ToString(), receivedData.humidity.ToString(), DateTime.Now.ToString("HH:mm:ss") });
                Debug.WriteLine("{0}  {1}  {2}", receivedData.heater, receivedData.temperature, receivedData.humidity);
            }
        }

        public static void setAudioTempCommand(string command)
        {
            audioTempCommand = command;
        }
    }
}
