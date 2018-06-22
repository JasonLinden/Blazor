// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    /// <summary>
    /// Used to to create instances a Blazor host builder for a Browser application.
    /// </summary>
    public static class BlazorBrowserHost
    {
        /// <summary>
        /// Creates a an instance of <see cref="IBrowserHostBuilder"/>.
        /// </summary>
        /// <returns>The <see cref="IBrowserHostBuilder"/>.</returns>
        public static IBrowserHostBuilder CreateDefaultBuilder()
        {
            return new BrowserHostBuilder();
        }
    }
}
