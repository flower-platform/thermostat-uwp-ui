using IotUiApp.Utils;
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;

namespace IotUiApp.SpeechRecognition
{
    class ContinuousSpeechRecognitionEngine
    {
        private static SpeechRecognizer m_recognizer;
        // Keep track of whether the recognizer is currently listening for user input
        private static bool m_isListening = false;
        private static int s_continuousRecognitionAutoStopSilenceTimeout = 100;
        private static uint s_maxRecognitionResultAlternates = 3;

        /*
            Starts continuous recognition sessions for prolonged audio input from the user
        */
        public static void InitContinuousSpeechRecognition()
        {
            m_recognizer = new SpeechRecognizer();
            m_recognizer.Timeouts.BabbleTimeout = System.TimeSpan.FromSeconds(120.0);
            m_recognizer.Timeouts.EndSilenceTimeout = System.TimeSpan.FromSeconds(120.0);
            m_recognizer.Timeouts.InitialSilenceTimeout = System.TimeSpan.FromSeconds(120.0);
            Debug.WriteLine("print1");
            SpeechRecognitionTopicConstraint topicConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "Development");
            m_recognizer.Constraints.Add(topicConstraint);

            IAsyncOperation<SpeechRecognitionCompilationResult> asyncResult = m_recognizer.CompileConstraintsAsync();
            asyncResult.Completed += CompileConstraintsCompletedHandler;
            Debug.WriteLine("print2");
        }

        private static void CompileConstraintsCompletedHandler(IAsyncOperation<SpeechRecognitionCompilationResult> asyncInfo, AsyncStatus asyncStatus)
        {
            Debug.WriteLine("print3");
            SpeechRecognitionCompilationResult result = asyncInfo.GetResults();

            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                Debug.WriteLine("Grammar Compilation Failed: " + result.Status.ToString());

            }

            m_recognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout = System.TimeSpan.FromSeconds(s_continuousRecognitionAutoStopSilenceTimeout);
            m_recognizer.ContinuousRecognitionSession.Completed += OnContinuousRecognitionSessionCompletedHandler;
            m_recognizer.ContinuousRecognitionSession.ResultGenerated += OnContinuousRecognitionSessionResultGeneratedHandler;

            StartListening();
            Debug.WriteLine("print4");
        }



        /// <summary>
        /// Handle events fired when error conditions occur, such as the microphone becoming unavailable, or if
        /// some transient issues occur.
        /// </summary>
        /// <param name="sender">The continuous recognition session</param>
        /// <param name="args">The state of the recognizer</param>
        private static void OnContinuousRecognitionSessionCompletedHandler(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {

            if (args.Status == SpeechRecognitionResultStatus.Unknown)
            {
                m_isListening = false;
                UiUtils.ShowNotification("Recorder has stop due to an unknown problem that caused recognition or compilation to fail.You might want to restart the application." + args.Status + " * ***" + m_recognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout.ToString());
            }
            else if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                m_isListening = false;
                UiUtils.ShowNotification("Recorder has stop due to its timeout settings.Start recorder again by clicking `Enable Speech Service` " + args.Status + " * ***" + m_recognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout.ToString());
            }


        }

        /// <summary>
        /// Handle events fired when a result is generated in the continuous recognition mode.
        /// </summary>
        /// <param name="sender">The Recognition session that generated this result</param>
        /// <param name="args">Details about the recognized speech</param>
        private static void OnContinuousRecognitionSessionResultGeneratedHandler(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            SpeechRecognitionResult result = args.Result;
            if (result.Confidence != SpeechRecognitionConfidence.Rejected)
            {
                var altResults = result.GetAlternates(s_maxRecognitionResultAlternates);
                uint idx = 0;
                foreach (var curentAltResult in altResults)
                {
                    if (curentAltResult.Confidence == SpeechRecognitionConfidence.Rejected)
                    {
                        break;
                    }
                    int num;
                    string speechResult = curentAltResult.Text.Remove(curentAltResult.Text.Length - 1);
                    bool isNumber = Int32.TryParse(speechResult, out num);
                    if (isNumber)
                    {

                        MainPage.SetAudioTempCommand(speechResult);
                    }
                    else
                    {
                        UiUtils.ShowNotification("Your message could not be parsed as number. Please specify a number!");
                    }
                    idx++;
                }
            }
            else
            {
                UiUtils.ShowNotification("Sorry, could not get that. Can you repeat?");
            }

        }

        private static void StartListening()
        {

            if (m_isListening == false)
            {

                m_isListening = true;

                try
                {
                    Debug.WriteLine("print5");
                    m_recognizer.ContinuousRecognitionSession.StartAsync();
                    Debug.WriteLine("print6");
                }
                catch (Exception ex)
                {
                    const int privacyPolicyHResult = unchecked((int)0x80045509);
                    const int networkNotAvailable = unchecked((int)0x80045504);

                    if (ex.HResult == privacyPolicyHResult)
                    {
                        UiUtils.ShowNotification("You will need to accept the speech privacy policy in order to use speech recognition in this app. Consider activating `Get to know me` in 'Settings->Privacy->Speech, inking & typing`");

                        //MainPage.ShowNotification("You will need to accept the speech privacy policy in order to use speech recognition in this app. Consider activating `Get to know me` in 'Settings->Privacy->Speech, inking & typing`");
                    }
                    else if (ex.HResult == networkNotAvailable)
                    {
                        UiUtils.ShowNotification("The network connection is not available");
                    }
                    else {
                        var t = ex.Message;
                    }
                }
            }

        }

        public static void StopSpeechRecognitionMechanism()
        {
            m_recognizer.ContinuousRecognitionSession.CancelAsync();
            m_isListening = false;
        }
    }
}
