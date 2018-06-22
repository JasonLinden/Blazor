// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Mono.WebAssembly.Interop;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    public class BrowserHostTest
    {
        [Fact]
        public async Task BrowserHost_StartAsync_ThrowsWithoutStartup()
        {
            // Arrange
            var builder = new BrowserHostBuilder();
            var host = builder.Build();
            
            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.StartAsync());

            // Assert
            Assert.Equal(
                "Could not find a registered Blazor Startup class. " +
                "Using IBrowserHost requires a call to IBrowserHostBuilder.UseBlazorStartup.",
                ex.Message);
        }

        [Fact]
        public async Task BrowserHost_StartAsync_RunsConfigureMethod()
        {
            // Arrange
            var builder = new BrowserHostBuilder();

            var startup = new MockStartup();
            builder.ConfigureServices((c, s) => { s.AddSingleton<IBlazorStartup>(startup); });

            var host = builder.Build();

            // Act
            await host.StartAsync();

            // Assert
            Assert.True(startup.ConfigureCalled);
        }

        [Fact]
        public async Task BrowserHost_StartAsync_SetsJSRuntime()
        {
            // Arrange
            var builder = new BrowserHostBuilder();
            builder.UseBlazorStartup<MockStartup>();

            var host = builder.Build();

            // Act
            await host.StartAsync();

            // Assert
            Assert.IsType<MonoWebAssemblyJSRuntime>(JSRuntime.Current);
        }

        private class MockStartup : IBlazorStartup
        {
            public bool ConfigureCalled { get; set; }

            public void Configure(IBlazorApplicationBuilder app, IServiceProvider services)
            {
                ConfigureCalled = true;
            }

            public void ConfigureServices(IServiceCollection services)
            {
            }
        }
    }
}
