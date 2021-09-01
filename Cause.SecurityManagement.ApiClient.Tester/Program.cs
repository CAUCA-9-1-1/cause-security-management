using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Cauca.ApiClient.Configuration;
using Cauca.ApiClient.Services;
using Flurl.Http.Testing;

namespace Cause.SecurityManagement.ApiClient.Tester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Console.WriteLine("Hello World!");

            var config = new Configuration();
            var service = new WhateverService(config);
            var message = new CommProviderAlertDetail
            {
                LanguageCode = "fr",
                MessageType = CommunicationType.Email,
                RecipientInformations = new List<CommProviderRecipient>
                {
                    new CommProviderRecipient{ Recipient = "fredcat@catefred.co", SendDigits = "1", StatusUrl = "whatever"}
                }
            };

            using var httpTest = new HttpTest();
            httpTest.RespondWith("farlouche");

            var response = await service.GetWhateve();

            /*httpTest.ShouldHaveCalled("http://test/yo")
                .WithVerb(HttpMethod.Get)
                .Times(1);*/

            

            //await service.SendMessage(new List<CommProviderAlertDetail>{message});

            Console.WriteLine("Funky whop");
        }
    }

    class WhateverService : BaseService<Configuration>
    {
        public WhateverService(Configuration configuration) : base(configuration)
        {
        }

        public Task<TransactionProvider> SendMessage(List<CommProviderAlertDetail> details)
        {
            return PostAsync<TransactionProvider>("message", details);
        }

        public async Task<string> GetWhateve()
        {
            var response = await GetStringAsync("yo");
            return response;
        }
    }

    public class TransactionProvider 
    {
        public Guid Id { get; set; }
        public List<Object> Errors { get; set; }
    }

    public class CommProviderAlertDetail
    {
        /*        [JsonIgnore]
                public CommunicationType MessageTypeEnum { get; set; }*/
        public CommunicationType MessageType { get; set; }
        public string LanguageCode { get; set; }
        public List<CommProviderRecipient> RecipientInformations { get; set; } = new List<CommProviderRecipient>();

        /*private string MessageTypeToString()
        {
            if (MessageTypeEnum == CommunicationType.Email)
                return "Email";
            if (MessageTypeEnum == CommunicationType.Sms)
                return "Sms";
            if (MessageTypeEnum == CommunicationType.Voice)
                return "Voice";

            return "";
        }*/
    }

    public class CommProviderRecipient
    {
        public string Recipient { get; set; }
        public string SendDigits { get; set; }
        public string StatusUrl { get; set; }
    }

    public enum CommunicationType
    {
        Voice,
        Email,
        Sms
    }

    class Configuration : IConfiguration
    {
        public string ApiBaseUrl { get; set; } = "http://localhost:5000/api/";
        public string ApiBaseUrlForAuthentication { get; set; }
        public string UserId { get; set; } = "9F3C60EDC1DDD704EE6C2C78AD48A64545E3D23F59AEC974710FB7E39C5433C0";
        public string Password { get; set; }
        public bool UseExternalSystemLogin { get; set; } = true;
        public string AuthorizationType { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int RequestTimeoutInSeconds { get; set; }
    }
}
