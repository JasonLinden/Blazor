// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Blazor.Browser.Http;
using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Mono.WebAssembly.Interop;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    //
    // This code was taken virtually as-is from the Microsoft.Extensions.Hosting project in aspnet/Hosting and then
    // lots of things were removed.
    //
    internal class BrowserHostBuilder : IBrowserHostBuilder
    {
        private List<Action<BrowserHostBuilderContext, IServiceCollection>> _configureServicesActions = new List<Action<BrowserHostBuilderContext, IServiceCollection>>();
        private bool _hostBuilt;
        private BrowserHostBuilderContext _BrowserHostBuilderContext;
        private IBrowserHostingEnvironment _hostingEnvironment;
        private IServiceProvider _appServices;

        /// <summary>
        /// A central location for sharing state between components during the host building process.
        /// </summary>
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        /// <summary>
        /// Adds services to the container. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="configureDelegate"></param>
        /// <returns>The same instance of the <see cref="IBrowserHostBuilder"/> for chaining.</returns>
        public IBrowserHostBuilder ConfigureServices(Action<BrowserHostBuilderContext, IServiceCollection> configureDelegate)
        {
            _configureServicesActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Run the given actions to initialize the host. This can only be called once.
        /// </summary>
        /// <returns>An initialized <see cref="IBrowserHost"/></returns>
        public IBrowserHost Build()
        {
            if (_hostBuilt)
            {
                throw new InvalidOperationException("Build can only be called once.");
            }
            _hostBuilt = true;

            CreateHostingEnvironment();
            CreateBrowserHostBuilderContext();
            CreateServiceProvider();

            return _appServices.GetRequiredService<IBrowserHost>();
        }

        private void CreateHostingEnvironment()
        {
            _hostingEnvironment = new BrowserHostingEnvironment();
        }

        private void CreateBrowserHostBuilderContext()
        {
            _BrowserHostBuilderContext = new BrowserHostBuilderContext(Properties)
            {
                HostingEnvironment = _hostingEnvironment,
            };
        }

        private void CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);
            services.AddSingleton(_BrowserHostBuilderContext);
            services.AddSingleton<IBrowserHost, BrowserHost>();
            services.AddSingleton<IJSRuntime, MonoWebAssemblyJSRuntime>();

            services.AddSingleton<IUriHelper, BrowserUriHelper>();
            services.AddSingleton<HttpClient>(s =>
            {
                // Creating the URI helper needs to wait until the JS Runtime is initialized, so defer it.
                var uriHelper = s.GetRequiredService<IUriHelper>();
                return new HttpClient(new BrowserHttpMessageHandler())
                {
                    BaseAddress = new Uri(uriHelper.GetBaseUri())
                };
            });

            foreach (var configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(_BrowserHostBuilderContext, services);
            }

            _appServices = services.BuildServiceProvider();
        }
    }
}