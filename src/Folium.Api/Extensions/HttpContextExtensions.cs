/** 
 * Copyright 2017 The Royal Veterinary College, jbullock AT rvc.ac.uk
 * 
 * This file is part of Folium.
 * 
 * Folium is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Folium is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Folium.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Folium.Api.Extensions {
    public static class HttpContextExtensions {
        private static readonly RouteData EmptyRouteData = new RouteData();

        private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

        public static Task WriteModelAsync<TModel>(this HttpContext context, TModel model) {
            var result = new ObjectResult(model) {
                DeclaredType = typeof(TModel)
            };

            return context.ExecuteResultAsync(result);
        }

        public static Task ExecuteResultAsync<TResult>(this HttpContext context, TResult result)
            where TResult : IActionResult {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var executor = context.RequestServices.GetRequiredService<IActionResultExecutor<TResult>>();

            var routeData = context.GetRouteData() ?? EmptyRouteData;
            var actionContext = new ActionContext(context, routeData, EmptyActionDescriptor);

            return executor.ExecuteAsync(actionContext, result);
        }

        public static Task StreamFileAsync(this HttpContext context, string filePath, string contentType, string filename = null) {
            var fileStream = File.OpenRead(filePath);
            var result = new FileStreamResult(fileStream, contentType) {
                FileDownloadName = filename,
                EnableRangeProcessing = true
            };
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return context.ExecuteResultAsync(result);
        }

        public static bool IsLocalRequest(this HttpContext context) {
			return context.Connection.RemoteIpAddress.Equals(context.Connection.LocalIpAddress) || IPAddress.IsLoopback(context.Connection.RemoteIpAddress);
		}
    }
}
