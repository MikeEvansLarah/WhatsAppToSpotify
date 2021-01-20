using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WhatsAppToSpotify.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                          .AddEnvironmentVariables()
                          .AddJsonFile("settings.json", true, true)
                          .Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, config);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var authCommand = new Command("auth");

            authCommand.Handler = CommandHandler.Create(async () =>
            {
                var command = new SpotifyAuthenticateCommand();
                await serviceProvider.GetRequiredService<SpotifyAuthenticateHandler>().HandleAsync(command);   
            });

            var exportToPlaylistCommand = new Command("export-to-playlist")
            {
                new Option(
                   new string [] { "--export-file", "-e" },
                   "The WhatsApp export file")
                {
                   Argument = new Argument<FileInfo>("exportFile"),
                   Required = true
                },
                new Option(
                   new string [] { "--playlist-id", "-p" },
                   "Spotify playlist ID")
                {
                   Argument = new Argument<string>("playlistId"),
                   Required = true
                },
                new Option(
                   new string [] { "--start-from", "-s" },
                   "The time to start processing messages from (in ISO 8601 format)")
                {
                   Argument = new Argument<DateTime>("startFrom", () => DateTime.MinValue)
                },
                new Option(
                   new string [] { "--dry-run", "-d" },
                   "If set to true, then won't actually add tracks to playlist")
                {
                   Argument = new Argument<bool>("dryRun", () => false)
                },
            };

            exportToPlaylistCommand.Handler = CommandHandler.Create<FileInfo, string, DateTime, bool>(async (exportFile, playlistId, startFrom, dryRun) =>
            {
                var command = new WhatsAppExportToPlaylistCommand
                {
                    ExportLines = File.ReadAllLines(exportFile.FullName),
                    PlaylistId = playlistId,
                    StartFrom = startFrom,
                    DryRun = dryRun
                };

                await serviceProvider.GetRequiredService<WhatsAppExportToPlaylistHandler>().HandleAsync(command);
            });

            var analyzeCommand = new Command("analyze")
            {
                new Option(
                   new string [] { "--export-file", "-e" },
                   "The WhatsApp export file")
                {
                   Argument = new Argument<FileInfo>("exportFile"),
                   Required = true
                },
                                new Option(
                   new string [] { "--output-file", "-o" },
                   "The output CSV file")
                {
                   Argument = new Argument<FileInfo>("outputFile"),
                   Required = true
                },
                new Option(
                   new string [] { "--start-from", "-s" },
                   "The time to start processing messages from (in ISO 8601 format)")
                {
                   Argument = new Argument<DateTime>("startFrom", () => DateTime.MinValue)
                }
            };

            analyzeCommand.Handler = CommandHandler.Create<FileInfo, FileInfo, DateTime>(async (exportFile, outputFile, startFrom) =>
            {
                var command = new AnalyzeCommand
                {
                    ExportLines = File.ReadAllLines(exportFile.FullName),
                    OutputFile = outputFile,
                    StartFrom = startFrom
                };

                await serviceProvider.GetRequiredService<AnalyzeHandler>().HandleAsync(command);
            });

            var rootCommand = new RootCommand("wats")
            {
                authCommand,
                exportToPlaylistCommand,
                analyzeCommand,
            };

            rootCommand.Description = "WhatsApp to Spotify. Ensure you have set spotify:clientId and spotify:clientSecret, either through environment variables or in settings.json file.";

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }

        private static void ConfigureServices(ServiceCollection services, IConfiguration config)
        {
            services.AddLogging(configure => configure.AddConsole())
                    .AddSingleton<Spotify>()
                    .AddTransient<SpotifyAuthenticateHandler>()
                    .AddTransient<WhatsAppExportToPlaylistHandler>()
                    .AddTransient<AnalyzeHandler>()
                    .AddOptions()
                    .Configure<SpotifyOptions>(config.GetSection("spotify"));
        }
    }
}
