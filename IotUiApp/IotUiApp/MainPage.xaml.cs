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

        public MainPage()
        {
            this.InitializeComponent();
            MyWebView.ScriptNotify += webView_ScriptNotify;
        }

        async void webView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            string value = e.Value;
            if (value.Contains("temperature="))
            {
                var tempValueCommand = value;

                BackgroundWorker bw = new BackgroundWorker();

                // send command and process it in background
                bw.DoWork += new DoWorkEventHandler(
                delegate (object o, DoWorkEventArgs args)
                {
                    SendCloudToDevice.sendCommand(tempValueCommand);

                });

                //when worker completes its task notify user inerface by invoking a javascript 
                //method that updates command timestamp in the ui
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                delegate (object o, RunWorkerCompletedEventArgs args)
                {
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MyWebView.InvokeScriptAsync("updateCommandTimestamp", new string[] {DateTime.Now.ToString("HH:mm:ss") });
                    });
                });

                bw.RunWorkerAsync();
            }

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Uri u = new Uri("ms-appx-web:///Assets/views/page.html");
            MyWebView.Navigate(u);
        }

        private void getMessageFromPartition(string partition)
        {
            string message = ReadDeviceToCloud.getMessage(partition);
            ReceivedData receivedData;
            if (message != null)

            {
                receivedData = JsonConvert.DeserializeObject<ReceivedData>(message);
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MyWebView.InvokeScriptAsync("updateValues", new string[] { receivedData.temperature.ToString(), receivedData.humidity.ToString(), DateTime.Now.ToString("HH:mm:ss") });
                });
                Debug.WriteLine("{0}  {1}  {2}", receivedData.heater, receivedData.temperature, receivedData.humidity);
            }
        }


        void LoadCompleted(object sender, NavigationEventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();


            // process received messages from both parititons in background 
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                getMessageFromPartition("1");

            });

            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                getMessageFromPartition("0");

            });

            bw.RunWorkerAsync();

        }

    }
}
