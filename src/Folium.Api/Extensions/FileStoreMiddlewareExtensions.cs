using Folium.Api.Infrastructure;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Folium.Api.Extensions {
    public static class FileStoreMiddlewareExtensions {
        public static IApplicationBuilder UseFileStore(
            this IApplicationBuilder builder) {
            return builder.UseMiddleware<FileStoreMiddleware>();
        }
    }
}
