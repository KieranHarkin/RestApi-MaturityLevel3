﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Library.API.Services;
using Library.API.Entities;
using Microsoft.EntityFrameworkCore;
using Library.API.Models;
using Library.API.Helpers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Newtonsoft.Json.Serialization;
using NLog.Extensions.Logging;
using System.Linq;
using AspNetCoreRateLimit;

namespace Library.API
{
    public class Startup
    {
        public static IConfigurationRoot Configuration;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(setupAction =>
            {
                // E.g Don't return JSON if Accept Header is application/xml return 406 instead.
                setupAction.ReturnHttpNotAcceptable = true;
                setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());

                var xmlDataContractSerializerInputFormatter =
                    new XmlDataContractSerializerInputFormatter();

                xmlDataContractSerializerInputFormatter.SupportedMediaTypes
                    .Add("application/vnd.marvin.authorwithdateofdeath.full+xml");
                xmlDataContractSerializerInputFormatter.SupportedMediaTypes
                    .Add("application/vnd.marvin.author.full+xml");

                setupAction.InputFormatters.Add(xmlDataContractSerializerInputFormatter);


                var jsonOutputFormatter = setupAction.OutputFormatters
                    .OfType<JsonOutputFormatter>().FirstOrDefault();

                jsonOutputFormatter?.SupportedMediaTypes.Add("application/vnd.marvin.hateoas+json");

                var jsonInputFormatter = setupAction.InputFormatters
                    .OfType<JsonInputFormatter>().FirstOrDefault();

                if (jsonInputFormatter != null)
                {
                    jsonInputFormatter.SupportedMediaTypes
                        .Add("application/vnd.marvin.author.full+json");
                    jsonInputFormatter.SupportedMediaTypes
                        .Add("application/vnd.marvin.authorwithdateofdeath.full+json");
                }
            })
            .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver =
                        new CamelCasePropertyNamesContractResolver();
                });

            // register the DbContext on the container, getting the connection string from
            // appSettings (note: use this during development; in a production environment,
            // it's better to store the connection string in an environment variable)
            var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

            // register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository>();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddScoped<IUrlHelper, UrlHelper>(implementationFactory =>
            {
                var actionContext =
                    implementationFactory.GetService<IActionContextAccessor>().ActionContext;
                return new UrlHelper(actionContext);
            });

            services.AddTransient<IPropertyMappingService, PropertyMappingService>();

            services.AddTransient<ITypeHelperService, TypeHelperService>();

            services.AddHttpCacheHeaders((expirationModelOptions) =>
            {
                expirationModelOptions.MaxAge = 600;
            },
                (validationModelOptions) =>
                {
                    validationModelOptions.AddMustRevalidate = true;
                });

            services.AddMemoryCache();

            services.Configure<IpRateLimitOptions>((options) =>
            {
                options.GeneralRules = new List<RateLimitRule>()
                {
                    new RateLimitRule()
                    {
                        Endpoint = "*",
                        Limit = 1000,
                        Period = "5m"
                    },
                    new RateLimitRule()
                    {
                        Endpoint = "*",
                        Limit = 200,
                        Period = "10s"
                    }
                };
            });

            // We only want this created once
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, 
            ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            loggerFactory.AddConsole();

            loggerFactory.AddDebug(LogLevel.Information);

            loggerFactory.AddNLog();


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
                        if (exceptionHandler != null)
                        {
                            var logger = loggerFactory.CreateLogger("Global exception logger.");
                            logger.LogError(500, exceptionHandler.Error, exceptionHandler.Error.Message);
                        }
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected fault happened. Please try again later.");
                    });
                });
            }

            AutoMapper.Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Author, AuthorDto>()
                    .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                    .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.DateOfBirth.GetCurrentAge(src.DateOfDeath)));

                cfg.CreateMap<Book, BookDto>();

                cfg.CreateMap<AuthorForCreationDto, Author>();

                cfg.CreateMap<AuthorForCreationWithDateOfDeathDto, Author>();

                cfg.CreateMap<BookForCreationDto, Book>();

                cfg.CreateMap<BookForUpdateDto, Book>();

                cfg.CreateMap<Book, BookForUpdateDto>();
            });

            libraryContext.EnsureSeedDataForContext();

            app.UseIpRateLimiting();

            app.UseHttpCacheHeaders();

            app.UseMvc();
        }
    }
}
