using Amqp;
using Amqp.Framing;
using ppatierno.AzureSBLite.Utility;
using System;
using System.Text;
using IotUiApp.Utils;

namespace IotUiApp
{
    class SendCloudToDevice
    {
        // Please replace the constants below, with actual values (taken from your Azure Portal)
        private const string HOST = "arduino-mkr1000.azure-devices.net";
        private const string DEVICE_ID = "mkr1000";
        private const string DEVICE_KEY = "H3pZf6Y3MtKqJ1u5zQarKQ==";
        static string sharedAccessKeyName = "iothubowner";
        static string sharedAccessKey = "atQwsAzJOGBo6A1tV2ZN55BizfzPVRv2ZIc1XmSN3Lo=";

        private const int PORT = 5671;
        private static Address address = new Address(HOST, PORT, null, null);
        private static Connection connection = new Connection(address);
        private string audience = Fx.Format("{0}/devices/{1}", HOST, DEVICE_ID);
        private string resourceUri = Fx.Format("{0}/devices/{1}", HOST, DEVICE_ID);
        private static DateTime startingDateTimeUtc;
        private static Session session = new Session(connection);



        //source: https://github.com/ppatierno/codesamples/blob/master/IoTHubAmqp/IoTHubAmqpService/Program.cs
        static private  bool PutCbsToken(Connection connection, string host, string shareAccessSignature, string audience)
        {
            bool result = true;
            Session session = new Session(connection);

            string cbsReplyToAddress = "cbs-reply-to";
            var cbsSender = new SenderLink(session, "cbs-sender", "$cbs");
            var cbsReceiver = new ReceiverLink(session, cbsReplyToAddress, "$cbs");

            // construct the put-token message
            var request = new Message(shareAccessSignature);
            request.Properties = new Properties();
            request.Properties.MessageId = Guid.NewGuid().ToString();
            request.Properties.ReplyTo = cbsReplyToAddress;
            request.ApplicationProperties = new ApplicationProperties();
            request.ApplicationProperties["operation"] = "put-token";
            request.ApplicationProperties["type"] = "azure-devices.net:sastoken";
            request.ApplicationProperties["name"] = audience;
            cbsSender.Send(request);

            // receive the response
            var response = cbsReceiver.Receive();
            if (response == null || response.Properties == null || response.ApplicationProperties == null)
            {
                result = false;
            }
            else
            {
                int statusCode = (int)response.ApplicationProperties["status-code"];
                string statusCodeDescription = (string)response.ApplicationProperties["status-description"];
                if (statusCode != (int)202 && statusCode != (int)200) // !Accepted && !OK
                {
                    result = false;
                }
            }

            // the sender/receiver may be kept open for refreshing tokens
            cbsSender.Close();
            cbsReceiver.Close();
            session.Close();

            return result;
        }
        private static readonly long UtcReference = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).Ticks;

        //source: https://github.com/ppatierno/codesamples/blob/master/IoTHubAmqp/IoTHubAmqpService/Program.cs
        static string GetSharedAccessSignature(string keyName, string sharedAccessKey, string resource, TimeSpan tokenTimeToLive)
        {
            // http://msdn.microsoft.com/en-us/library/azure/dn170477.aspx
            // the canonical Uri scheme is http because the token is not amqp specific
            // signature is computed from joined encoded request Uri string and expiry string

#if NETMF
            // needed in .Net Micro Framework to use standard RFC4648 Base64 encoding alphabet
            System.Convert.UseRFC4648Encoding = true;
#endif
            string expiry = ((long)(DateTime.UtcNow - new DateTime(UtcReference, DateTimeKind.Utc) + tokenTimeToLive).TotalSeconds()).ToString();
            string encodedUri = HttpUtility.UrlEncode(resource);

            byte[] hmac = SHA.computeHMAC_SHA256(Convert.FromBase64String(sharedAccessKey), Encoding.UTF8.GetBytes(encodedUri + "\n" + expiry));
            string sig = Convert.ToBase64String(hmac);

            if (keyName != null)
            {
                return Fx.Format(
                "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}",
                encodedUri,
                HttpUtility.UrlEncode(sig),
                HttpUtility.UrlEncode(expiry),
                HttpUtility.UrlEncode(keyName));
            }
            else
            {
                return Fx.Format(
                    "SharedAccessSignature sr={0}&sig={1}&se={2}",
                    encodedUri,
                    HttpUtility.UrlEncode(sig),
                    HttpUtility.UrlEncode(expiry));
            }
        }

        //code adapted from tutorial https://paolopatierno.wordpress.com/2015/10/31/azure-iot-hub-commands-and-feedback-using-amqp-net-lite/
        public static void SendCommand(string command)
        {

            string audience = Fx.Format("{0}/messages/devicebound", HOST);
            string resourceUri = Fx.Format("{0}/messages/devicebound", HOST);

            string sasToken = GetSharedAccessSignature(sharedAccessKeyName, sharedAccessKey, resourceUri, new TimeSpan(1, 0, 0));
            bool cbs = PutCbsToken(connection, HOST, sasToken, audience);

            if (cbs)
            {
                string to = Fx.Format("/devices/{0}/messages/devicebound", DEVICE_ID);
                string entity = "/messages/devicebound";

                SenderLink senderLink = new SenderLink(session, "sender-link", entity);

                var messageValue = Encoding.UTF8.GetBytes(command);
                Message message = new Message()
                {
                    BodySection = new Data() { Binary = messageValue }
                };
                message.Properties = new Properties();
                message.Properties.To = to;
                message.Properties.MessageId = Guid.NewGuid().ToString();
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["iothub-ack"] = "full";

                senderLink.Send(message);
                senderLink.Close();


            }
        }
    }
}
