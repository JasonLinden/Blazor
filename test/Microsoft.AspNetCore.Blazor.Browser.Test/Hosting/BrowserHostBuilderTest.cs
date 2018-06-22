// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    public class BrowserHostBuilderTest
    {
        [Fact]
        public void HostBuilder_CanCallBuild_BuildsServices()
        {
            // Arrange
            var builder = new BrowserHostBuilder();

            // Act
            var host = builder.Build();

            // Assert
            Assert.NotNull(host.Services.GetService(typeof(IBrowserHostingEnvironment)));
        }

        [Fact]
        public void HostBuilder_CanConfigureAdditionalServices()
        {
            // Arrange
            var builder = new BrowserHostBuilder();
            builder.ConfigureServices((c, s) => s.AddSingleton<string>("foo"));
            builder.ConfigureServices((c, s) => s.AddSingleton<StringBuilder>(new StringBuilder("bar")));

            // Act
            var host = builder.Build();

            // Assert
            Assert.Equal("foo", host.Services.GetService(typeof(string)));
            Assert.Equal("bar", host.Services.GetService(typeof(StringBuilder)).ToString());
        }

        [Fact]
        public void HostBuilder_UseBlazorStartup_CanConfigureAdditionalServices()
        {
            // Arrange
            var builder = new BrowserHostBuilder();
            builder.UseBlazorStartup<MyStartup>();
            builder.ConfigureServices((c, s) => s.AddSingleton<StringBuilder>(new StringBuilder("bar")));

            // Act
            var host = builder.Build();

            // Assert
            Assert.Equal("foo", host.Services.GetService(typeof(string)));
            Assert.Equal("bar", host.Services.GetService(typeof(StringBuilder)).ToString());
        }

        [Fact]
        public void HostBuilder_UseBlazorStartup_DoesNotAllowMultiple()
        {
            // Arrange
            var builder = new BrowserHostBuilder();
            builder.UseBlazorStartup<MyStartup>();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => builder.UseBlazorStartup<MyStartup>());

            // Assert
            Assert.Equal("A startup class has already been registered.", ex.Message);
        }

        private class MyStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton<string>("foo");
            }
        }
    }
}
