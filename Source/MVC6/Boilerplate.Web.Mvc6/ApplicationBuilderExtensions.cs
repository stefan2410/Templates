﻿namespace Boilerplate.Web.Mvc
{
    using System.Globalization;
    using System.Net;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Diagnostics;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.Framework.DependencyInjection;

    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension methods.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="UrlHelperExtensions"/> and <see cref="OpenGraphMetadata"/> with the 
        /// <see cref="IHttpContextAccessor"/>, so that they have access to the current <see cref="HttpContext"/> and
        /// can get the current request path. Also configures the <see cref="AtomActionResult"/> with the 
        /// <see cref="IHostingEnvironment"/>.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>The application.</returns>
        public static IApplicationBuilder UseBoilerplate(this IApplicationBuilder application)
        {
            IHostingEnvironment hostingEnvironment = 
                application.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            IHttpContextAccessor httpContextAccessor = 
                application.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            Context.Configure(hostingEnvironment, httpContextAccessor);
            return application;
        }

        /// <summary>
        /// Adds a StatusCodePages middle-ware to the pipeline. Specifies that the response body should be generated by 
        /// re-executing the request pipeline using an alternate path. This path may contain a '{0}' placeholder of the 
        /// status description. Use <see cref="UseStatusCodePagesWithReExecute"/> if you want to use the status code
        /// number as the placeholder instead.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="pathFormat">The string representing the path to the error page. This path may contain a '{0}' 
        /// placeholder of the status description.</param>
        /// <returns>The application.</returns>
        public static IApplicationBuilder UseStatusNamePagesWithReExecute(
            this IApplicationBuilder application, 
            string pathFormat)
        {
            return application.UseStatusCodePages(
                async context =>
                {
                    int statusCode = context.HttpContext.Response.StatusCode;
                    var status = (HttpStatusCode)context.HttpContext.Response.StatusCode;
                    var newPath = new PathString(string.Format(
                        CultureInfo.InvariantCulture,
                        pathFormat,
                        statusCode,
                        status.ToString()));

                    var originalPath = context.HttpContext.Request.Path;
                    // Store the original paths so the application can check it.
                    context.HttpContext.SetFeature<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature()
                    {
                        OriginalPathBase = context.HttpContext.Request.PathBase.Value,
                        OriginalPath = originalPath.Value,
                    });

                    context.HttpContext.Request.Path = newPath;
                    try
                    {
                        await context.Next(context.HttpContext);
                    }
                    finally
                    {
                        context.HttpContext.Request.Path = originalPath;
                        context.HttpContext.SetFeature<IStatusCodeReExecuteFeature>(null);
                    }
                });
        }
    }
}
