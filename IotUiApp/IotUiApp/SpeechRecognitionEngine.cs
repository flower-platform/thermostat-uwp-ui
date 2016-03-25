using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.SpeechSynthesis;
using System.Diagnostics;

namespace IotUiApp
{
    class SpeechRecognitionEngine
    {
        private static SpeechRecognizer m_recognizer;
        // Keep track of whether the recognizer is currently listening for user input
        private static bool m_isListening = false;
        private static int s_continuousRecognitionAutoStopSilenceTimeout = 3600;
        private static uint s_maxRecognitionResultAlternates = 3;
        private static async void init()
        {
            m_recognizer = new SpeechRecognizer();
            SpeechRecognitionTopicConstraint topicConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "Development");
            m_recognizer.Constraints.Add(topicConstraint);
            SpeechRecognitionCompilationResult result = await m_recognizer.CompileConstraintsAsync();   // Required

            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                Debug.WriteLine("Grammar Compilation Failed: " + result.Status.ToString());

            }

            m_recognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout = System.TimeSpan.FromSeconds(s_continuousRecognitionAutoStopSilenceTimeout);
            m_recognizer.ContinuousRecognitionSession.Completed += OnContinuousRecognitionSessionCompletedHandler;
            m_recognizer.ContinuousRecognitionSession.ResultGenerated += OnContinuousRecognitionSessionResultGeneratedHandler;
        }

        /// <summary>
        /// Handle events fired when error conditions occur, such as the microphone becoming unavailable, or if
        /// some transient issues occur.
        /// </summary>
        /// <param name="sender">The continuous recognition session</param>
        /// <param name="args">The state of the recognizer</param>
        private static async void OnContinuousRecognitionSessionCompletedHandler(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                m_isListening = false;
                //TO DO ANCA - notifica faptul ca s a terminat inegistrarea

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
                    //TO DO ANCA - verifica notifica daca nu e numar valid

                    MainPage.setAudioTempCommand("temperature=" + curentAltResult.Text.Remove(curentAltResult.Text.Length - 1) + "\0");
                    idx++;
                }
            }
            else
            {
                //TO DO ANCA - notifica faptul ca nu s-a inteles
            }

        }

        private static async void starListening()
        {
            var recognition = new Object();

            if (m_isListening == false)
            {

                m_isListening = true;

                try
                {
                    await m_recognizer.ContinuousRecognitionSession.StartAsync();
                }
                catch (Exception ex)
                {
                    const int privacyPolicyHResult = unchecked((int)0x80045509);
                    const int networkNotAvailable = unchecked((int)0x80045504);

                    if (ex.HResult == privacyPolicyHResult)
                    {
                        // User has not accepted the speech privacy policy
                        //this.PromptForSpeechRecognitionPermission();
                        //new MessageDialog("You will need to accept the speech privacy policy in order to use speech recognition in this app.").ShowAsync();
                    }
                    else if (ex.HResult == networkNotAvailable)
                    {
                        Debug.WriteLine("bau");
                    }
                    else
                    {
                        //this.Dispatcher.BeginInvoke(delegate { MessageBox.Show(ex.Message); });
                    }
                }
            }

        }

        public static void startSpeechRecognitionMechanism()
        {
            init();
            starListening();
        }
    }
}
