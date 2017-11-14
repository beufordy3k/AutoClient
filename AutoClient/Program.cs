using System;
using System.Net.Http;
using System.Threading.Tasks;

using Fclp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;

namespace AutoClient
{
    internal class Program
    {
        private const string DefaultEndpointUrl = "http://vautointerview.azurewebsites.net";

        private static async Task Main(string[] args)
        {
            //Setup Command Line Parsing
            var fclp = new FluentCommandLineParser();

            var endpointUrl = DefaultEndpointUrl;
            var logLevel = LogLevel.Information;

            fclp.Setup<string>('u', "endpointUrl")
                .Callback(u => endpointUrl = u)
                .WithDescription($"[Optional] Used to specify an endpoint url. Default: {DefaultEndpointUrl}");

            fclp.Setup<LogLevel>('l', "logLevel")
                .Callback(l => logLevel = l)
                .WithDescription(
                    $"[Optional] Used to specify the logging level. Options: {string.Join(", ", Enum.GetNames(typeof(LogLevel)))}. Default: {LogLevel.Information}");

            fclp.SetupHelp("h", "?", "help")
                .Callback(s => Console.WriteLine(s));

            var result = fclp.Parse(args); //Parse command line args

            if (result.HelpCalled)
            {
                return; //Help displayed nothing left to do
            }

            if (result.HasErrors)
            {
                Console.WriteLine("Error with command line arguments");
                fclp.HelpOption.ShowHelp(fclp.Options); //Provide command line arguments syntax

                return; //Error in arguments specified, do nothing
            }

            var servicesProvider = SetupContainer(logLevel); //Create DI container

            var autoClient = servicesProvider.GetRequiredService<VAutoClient>();

            //Execute client run
            var response = await autoClient.Run(endpointUrl);

            //Handle Response Output
            var responseOutput = JsonConvert.SerializeObject(response, Formatting.Indented);

            Console.WriteLine("");
            Console.WriteLine("AutoClient Results:");
            Console.WriteLine(responseOutput);
        }

        private static IServiceProvider SetupContainer(LogLevel logLevel)
        {
            var services = new ServiceCollection();

            services.AddTransient<VAutoClient>();
            services.AddSingleton<HttpClient>(); //Reuse the HTTP client for performance

            //Setting up logging with nlog
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddLogging(builder => builder.SetMinimumLevel(logLevel));

            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            loggerFactory.AddNLog(new NLogProviderOptions
            {
                CaptureMessageTemplates = true,
                CaptureMessageProperties = true
            });

            loggerFactory.ConfigureNLog("nlog.config"); //Used to setup additional targets (E.g. - file target)

            return serviceProvider;
        }
    }
}
