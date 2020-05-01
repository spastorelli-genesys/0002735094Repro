using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Extensions;
using PureCloudPlatform.Client.V2.Extensions.Notifications;
using PureCloudPlatform.Client.V2.Model;

namespace repro
{
    class Program
    {
        static void Main(string[] args)
        {
            var accessTokenInfo = Configuration.Default.ApiClient.PostToken(
                "clientId",
                "clientSecret");
            Configuration.Default.AccessToken = accessTokenInfo.AccessToken;

            PureCloudRegionHosts region = PureCloudRegionHosts.us_east_1;
            Configuration.Default.ApiClient.setBasePath(region);

            var usersApi = new UsersApi();
            var userId = "<REPLACE_WITH_TEST_USER_ID>";

            var handler = new NotificationHandler();
            var topic = $"v2.users.{userId}.conversations";
            handler.AddSubscription(topic, typeof(ConversationEventTopicConversation));
            Console.WriteLine($"Subscribed to {topic} topic");

            handler.NotificationReceived += (data) =>
            {
                if (data.GetType() == typeof(NotificationData<ConversationEventTopicConversation>))
                {
                    var conversation = (NotificationData<ConversationEventTopicConversation>)data;
                    var conversationId = conversation.EventBody.Id;
                    var eventRawJson = JsonConvert.SerializeObject(data, Formatting.Indented);
                    var eventJsonAsDict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(eventRawJson);

                    Console.WriteLine("\n-----------------------------------------------");
                    Console.WriteLine($"Notification for Conversation ID: {conversation.EventBody.Id}");

                    var eventBodyJsonAsDict = eventJsonAsDict["EventBody"];
                    var participants = conversation.EventBody.Participants.ToArray();
                    for (var i = 0; i < participants.Length; i++)
                    {
                        var participant = participants[i];
                        var participantJsonAsDict = eventBodyJsonAsDict["participants"][i];

                        if (participant.Purpose == "agent")
                        {
                            Console.WriteLine($"Participant ID: {participant.UserId}");
                            Console.WriteLine($"Calls:");
                            var participantCalls = participant.Calls.ToArray();
                            for (var j = 0; j < participantCalls.Length; j++)
                            {
                                var call = participantCalls[j];
                                var participantCallJson = participantJsonAsDict["calls"][j];
                                var stateJson = participantCallJson["state"];
                                Console.WriteLine($" -Call Id: {call.Id}, State (as Deserialized): {(int)call.State}, State (in JSON): {stateJson}");
                            }
                            var participantRawJson = JsonConvert.SerializeObject(participantJsonAsDict, Formatting.Indented);
                            Console.WriteLine($"Participant RawJSON (from Event):\n{participantRawJson}");
                        }
                    }
                    Console.WriteLine("-----------------------------------------------");
                }
            };

            Console.WriteLine("Websocket connected, awaiting messages...");
            Console.WriteLine("Press any key to stop and remove all subscriptions");
            Console.ReadKey(true);

            handler.RemoveAllSubscriptions();
            Console.WriteLine("All subscriptions removed, exiting...");
        }
    }
}
