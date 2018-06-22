// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    /// <summary>
    /// Provides Blazor-specific support for <see cref="IBrowserHost"/>.
    /// </summary>
    public static class BrowserHostBuilderExtensions
    {
        private const string BlazorStartupKey = "Blazor.Startup";

        /// <summary>
        /// Adds services to the container. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IBrowserHostBuilder" /> to configure.</param>
        /// <param name="configureDelegate"></param>
        /// <returns>The same instance of the <see cref="IBrowserHostBuilder"/> for chaining.</returns>
        public static IBrowserHostBuilder ConfigureServices(this IBrowserHostBuilder hostBuilder, Action<IServiceCollection> configureDelegate)
        {
            return hostBuilder.ConfigureServices((context, collection) => configureDelegate(collection));
        }

        /// <summary>
        /// Configures the <see cref="IBrowserHostBuilder"/> to use the provided startup class.
        /// </summary>
        /// <param name="builder">The <see cref="IBrowserHostBuilder"/>.</param>
        /// <param name="startupType">A type that configures a Blazor application.</param>
        /// <returns>The <see cref="IBrowserHostBuilder"/>.</returns>
        public static IBrowserHostBuilder UseBlazorStartup(this IBrowserHostBuilder builder, Type startupType)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder.Properties.ContainsKey(BlazorStartupKey))
            {
                throw new InvalidOperationException("A startup class has already been registered.");
            }

            // It would complicate the implementation to allow multiple startup classes, and we don't
            // really have a need for it.
            builder.Properties.Add(BlazorStartupKey, bool.TrueString);

            var startup = new ConventionBasedStartup(Activator.CreateInstance(startupType));

            builder.ConfigureServices(startup.ConfigureServices);
            builder.ConfigureServices(s => s.AddSingleton<IBlazorStartup>(startup));

            return builder;
        }

        /// <summary>
        /// Configures the <see cref="IBrowserHostBuilder"/> to use the provided startup class.
        /// </summary>
        /// <typeparam name="TStartup">A type that configures a Blazor application.</typeparam>
        /// <param name="builder">The <see cref="IBrowserHostBuilder"/>.</param>
        /// <returns>The <see cref="IBrowserHostBuilder"/>.</returns>
        public static IBrowserHostBuilder UseBlazorStartup<TStartup>(this IBrowserHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return UseBlazorStartup(builder, typeof(TStartup));
        }
    }
}
