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
using Windows.Media.SpeechRecognition;
using IotUiApp.Utils;

namespace IotUiApp.SpeechRecognition
{
    class SpeechRecognitionEngine
    {

        public static async void RecognizeSpeech()
        {
            SpeechRecognizer recognizer = new SpeechRecognizer();
            recognizer.Timeouts.BabbleTimeout = System.TimeSpan.FromSeconds(120.0);
            recognizer.Timeouts.EndSilenceTimeout = System.TimeSpan.FromSeconds(120.0);
            recognizer.Timeouts.InitialSilenceTimeout = System.TimeSpan.FromSeconds(120.0);
            SpeechRecognitionTopicConstraint topicConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "Message");
            recognizer.Constraints.Add(topicConstraint);
            await recognizer.CompileConstraintsAsync();
            try {
                SpeechRecognitionResult result = await recognizer.RecognizeAsync();
                //use result.GetAlternates for more precisivion
                if (result.Confidence != SpeechRecognitionConfidence.Rejected)
                {

                    if (result.Text != "")
                    {
                        string speechResult = result.Text.Remove(result.Text.Length - 1);
                        int num;
                        bool isNumber = Int32.TryParse(speechResult, out num);
                        if (isNumber)
                        {

                            MainPage.SetAudioTempCommand(speechResult);
                        }
                        else
                        {
                            UiUtils.ShowNotification("Your message could not be parsed as number. Please specify a number!");
                        }
                    }
                    else
                    {
                        UiUtils.ShowNotification("Your message could not be parsed. Please repeat!");
                    }
                }
                else
                {

                    UiUtils.ShowNotification("Sorry, could not get that. Can you repeat?");
                }
            }catch (Exception ex)
            {
                const int privacyPolicyHResult = unchecked((int)0x80045509);
                const int networkNotAvailable = unchecked((int)0x80045504);

                if (ex.HResult == privacyPolicyHResult)
                {
                    UiUtils.ShowNotification("You will need to accept the speech privacy policy in order to use speech recognition in this app. Consider activating `Get to know me` in 'Settings->Privacy->Speech, inking & typing`");
                   
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
}
