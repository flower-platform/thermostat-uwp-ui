using ppatierno.AzureSBLite;
using ppatierno.AzureSBLite.Messaging;
using System;
using System.Text;

namespace IotUiApp.IOTHubCommunication
{
    class ReadDeviceToCloud
    {
        // Please replace the constants below, with actual values (taken from your Azure Portal)
        private static string ConnectionString = "???";
        private static string eventHubEntity = "???";
 
        //code adapted from tutorial https://paolopatierno.wordpress.com/2015/11/02/azure-iot-hub-get-telemetry-data-using-amqp-stack-and-azure-sb-lite/ 
        public static string GetMessage(string partitionId)
        {
            string result = null;
            ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(ConnectionString);
            builder.TransportType = TransportType.Amqp;

            MessagingFactory factory = MessagingFactory.CreateFromConnectionString(ConnectionString);

            EventHubClient client = factory.CreateEventHubClient(eventHubEntity);
            EventHubConsumerGroup group = client.GetDefaultConsumerGroup();

            var startingDateTimeUtc = DateTime.Now;

            EventHubReceiver receiver = group.CreateReceiver(partitionId, startingDateTimeUtc);

            EventData data = receiver.Receive();
            receiver.Close();
            client.Close();
            factory.Close();
            if (data != null)
            {
                result = Encoding.UTF8.GetString(data.GetBytes());
                return result;
            }
            else
            {
                return null;
            }

        }

    }
}
