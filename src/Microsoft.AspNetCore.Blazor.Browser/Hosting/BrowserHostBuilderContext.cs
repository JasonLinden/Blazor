// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    /// <summary>
    /// Context containing the common services on the <see cref="IBrowserHost" />. Some properties may be null until set by the <see cref="IBrowserHost" />.
    /// </summary>
    public sealed class BrowserHostBuilderContext
    {
        /// <summary>
        /// Creates a new <see cref="BrowserHostBuilderContext" />.
        /// </summary>
        /// <param name="properties">The property collection.</param>
        public BrowserHostBuilderContext(IDictionary<object, object> properties)
        {
            Properties = properties ?? throw new System.ArgumentNullException(nameof(properties));
        }

        /// <summary>
        /// The <see cref="IBrowserHostingEnvironment" /> initialized by the <see cref="IBrowserHost" />.
        /// </summary>
        public IBrowserHostingEnvironment HostingEnvironment { get; set; }

        /// <summary>
        /// A central location for sharing state between components during the host building process.
        /// </summary>
        public IDictionary<object, object> Properties { get; }
    }
}