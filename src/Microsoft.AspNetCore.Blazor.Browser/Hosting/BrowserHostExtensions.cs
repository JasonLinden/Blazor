// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    /// <summary>
    /// Extension methods for <see cref="IBrowserHost"/>.
    /// </summary>
    public static class BrowserHostExtensions
    {
        /// <summary>
        /// Runs the application.
        /// </summary>
        /// <param name="host">The <see cref="IBrowserHost"/> to run.</param>
        /// <remarks>
        /// Currently, Blazor applications running in the browser don't have a lifecycle - the application does not
        /// get a chance to gracefully shut down. For now, <see cref="Run(IBrowserHost)"/> simply starts the host
        /// and allows execution to continue.
        /// </remarks>
        public static void Run(this IBrowserHost host)
        {
            host.StartAsync().GetAwaiter().GetResult();
        }
    }
}