﻿using System;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ntbs_service;
using ntbs_service.DataAccess;

namespace ntbs_integration_tests.Helpers
{
    public static class WebApplicationExtensions
    {

        public static WebApplicationFactory<Program> WithNotificationAndTbServiceConnected(this WebApplicationFactory<Program> factory,
                                                                                            int notificationId,
                                                                                            string tbServiceCode)
        {
            return factory.WithWebHostBuilder(builder =>
            {
                UpdateDatabase(builder, (db) => Utilities.SetServiceCodeForNotification(db, notificationId, tbServiceCode));
            });
        }

        public static WebApplicationFactory<Program> WithNotificationAndPostcodeConnected(this WebApplicationFactory<Program> factory,
                                                                                            int notificationId,
                                                                                            string postcode)
        {
            return factory.WithWebHostBuilder(builder =>
            {
                UpdateDatabase(builder, (db) => Utilities.SetPostcodeForNotification(db, notificationId, postcode));
            });
        }

        public static HttpClient CreateClientWithoutRedirects(this WebApplicationFactory<Program> factory)
        {
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.Timeout = TimeSpan.FromMinutes(3);
            return client;
        }

        private static void UpdateDatabase(IWebHostBuilder builder, Action<NtbsContext> updateMethod)
        {
            builder.ConfigureServices(services =>
            {
                var serviceProvider = services.BuildServiceProvider();

                using (var scope = serviceProvider.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices
                        .GetRequiredService<NtbsContext>();

                    updateMethod(db);
                }
            });
        }
    }
}
