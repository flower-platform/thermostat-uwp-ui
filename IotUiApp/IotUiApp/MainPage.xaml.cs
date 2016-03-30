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
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using IotUiApp.Utils;
using IotUiApp.IOTHubCommunication;
using IotUiApp.SpeechRecognition;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IotUiApp
{

    public sealed partial class MainPage : Page
    {
        private static string audioTempCommand;

        public MainPage()
        {
            this.InitializeComponent();
            MyWebView.ScriptNotify += WebView_ScriptNotify;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Uri u = new Uri("ms-appx-web:///Assets/views/page.html");
            MyWebView.Navigate(u);
        }
        
        void LoadCompleted(object sender, NavigationEventArgs e)
        {
            //when load completed run some tasks in background

            BackgroundTaskUtils.RunBackgroundTaskWithParam("1", UpdateValuesFromPartitionTask, DoesNothing);
            BackgroundTaskUtils.RunBackgroundTaskWithParam("0", UpdateValuesFromPartitionTask, DoesNothing);
            BackgroundTaskUtils.RunBackgroundTask(CheckAudioTemperatureCommandTask);

        }

        /*
            invoked when javascript invokes C#            
        */
        async void WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            string value = e.Value;
            //invokes corresponding process depending on what value is sent by javascript
            if (value.Contains("temperature="))
            {
                var tempValueCommand = value;
                BackgroundTaskUtils.RunBackgroundTaskWithParam(tempValueCommand, SendCloudToDevice.SendCommand, UpdateCommandTimestampTask);
            }
            else if(value == "enable_speech")
            {
                SpeechRecognitionEngine.RecognizeSpeech();
                //uncomment row below and comment row above for continuous speech mechanism
                //ContinuousSpeechRecognitionEngine.InitContinuousSpeechRecognition();
            }
            else if (value == "disable_speech")
            {
                ContinuousSpeechRecognitionEngine.StopSpeechRecognitionMechanism();
            }

        }

        
        public  void InvokeJSScript(string scriptName, string[] parameters)
        {
            //retrieves the UI thread and invokes javascript on this thread
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MyWebView.InvokeScriptAsync(scriptName, parameters);
            });

        }
 

        public void DoesNothing()
        {

        }

        /*
            Continuous checks if an audio command was sent and
            if it was, invokes the send command mechanism.
        */
        public void CheckAudioTemperatureCommandTask()
        {
            while (true)
            {
                if (audioTempCommand != null)
                {
                    InvokeJSScript("setTemperature", new string[] { audioTempCommand});
                    audioTempCommand = null;
                }
            }
        }
        public void UpdateCommandTimestampTask()
        {
            InvokeJSScript("updateCommandTimestamp", new string[] { DateTime.Now.ToString("HH:mm:ss") });
        }
        /*
             Queries one of the event hub endpoint partition and updates the WebView with the values received
        */
        public void UpdateValuesFromPartitionTask(string partition)
        {

            while (true)
            {
                string message = ReadDeviceToCloud.GetMessage(partition);
                ReceivedData receivedData;
                if (message != null)
                {
                    receivedData = JsonConvert.DeserializeObject<ReceivedData>(message);
                    InvokeJSScript("updateValues", new string[] { receivedData.temperature.ToString(), receivedData.presetTemperature.ToString(), receivedData.humidity.ToString(), DateTime.Now.ToString("HH:mm:ss") });
                    Debug.WriteLine("{0}  {1}  {2}", receivedData.heater, receivedData.temperature, receivedData.humidity);
                }
            }
        }

        public static void SetAudioTempCommand(string command)
        {
            audioTempCommand = command;
        }

    }
}
