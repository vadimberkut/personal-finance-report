using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Extensions;
using PersonalFinanceReport.Toshl;
using PersonalFinanceReport.Toshl.Dto;
using PersonalFinanceReport.Utils;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PersonalFinanceReport
{
    class Program
    {
        private const string _appName = "PersonalFinanceReport";

        static void Main(string[] args)
        {
            // TODO
            // - build report for month
            // - delete unused tags and categories
            // - rate limit checks

            var configuration = GetConfiguration();
            var config = configuration.Get<ApplicationSettings>();

            // here we congigure the Serilog. Nothing special all according documentation of Serilog
            Log.Logger = GetSerilogLogger(configuration, config);

            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<ApplicationSettings>(configuration);
            services.AddHttpClient();
            services.EnableLoggingForBadRequests();
            services.AddHttpClientLogging(configuration);

            services.AddTransient<HttpUtil>();
            services.AddTransient<ToshHttpApiClient>(sp => {
                return new ToshHttpApiClient(
                    sp.GetRequiredService<ILogger<ToshHttpApiClient>>(),
                    sp.GetRequiredService<IOptions<ApplicationSettings>>().Value.Toshl,
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<HttpUtil>()
                );
            });

            var serviceProvider = services.BuildServiceProvider();


            RunAsync(serviceProvider, config).GetAwaiter().GetResult();

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }

        private static async Task RunAsync(IServiceProvider serviceProvider, ApplicationSettings config)
        {
            var currentTz = DateTimeZoneProviders.Tzdb["Europe/Simferopol"];

            var reportFrom = LocalDateTime.FromDateTime(DateTime.Now).With(DateAdjusters.StartOfMonth).InZoneLeniently(currentTz);
            var reportTo = LocalDateTime.FromDateTime(DateTime.Now).With(DateAdjusters.EndOfMonth).InZoneLeniently(currentTz);
            reportFrom = reportFrom.Minus(Duration.FromTicks(reportFrom.TimeOfDay.TickOfDay));
            reportTo = reportTo.Plus(Duration.FromHours(24) - Duration.FromTicks(reportTo.TimeOfDay.TickOfDay));

            var reportFromUtc = reportFrom.ToInstant().InUtc();
            var reportToUtc = reportTo.ToInstant().InUtc();

            var toshHttpApiClient = serviceProvider.GetRequiredService<ToshHttpApiClient>();
            
            var me = await toshHttpApiClient.MeAsync();

            const string cashAccountName = "Cash";
            const string dreamsAccountName = "Dreams";
            const string cryptoAccountName = "Crypto";

            const string taxesCategoryName = "Taxes";
            const string loansCategoryName = "Loans";

            var accounts = await toshHttpApiClient.AccountListAsync();
            var cashAccount = accounts.Single(x => x.Name == cashAccountName);
            var dreamsAccount = accounts.Single(x => x.Name == dreamsAccountName);
            var cryptoAccount = accounts.Single(x => x.Name == cryptoAccountName);

            var categories = await toshHttpApiClient.CategoryListAsync(
               type: "expense",
               page: 0,
               perPage: ToshHttpApiClient.MaxPerPage
            );
            var categoryNames = categories.Select(x => x.Name).ToList();

            var expenseEntries = await toshHttpApiClient.EntryListAllAsync(
                fromDate: reportFromUtc.Date.ToString("yyyy-MM-dd", null),
                toDate: reportToUtc.Date.ToString("yyyy-MM-dd", null),
                type: "expense",
                accounts: new List<string>() { cashAccount.Id },
                notCategories: new List<string>() { }
            );
            var incomeEntries = await toshHttpApiClient.EntryListAllAsync(
               fromDate: reportFromUtc.Date.ToString("yyyy-MM-dd", null),
               toDate: reportToUtc.Date.ToString("yyyy-MM-dd", null),
               type: "income",
               accounts: new List<string>() { cashAccount.Id },
               notCategories: new List<string>() { }
            );

            // entry filters
            var transfersToAccountsBlackList = new List<string>() 
            {
                cryptoAccount.Id,
            };
            var categoriesBlackList = new List<string>()
            {
                categories.Single(x => x.Name == taxesCategoryName).Id,
                categories.Single(x => x.Name == loansCategoryName).Id,
            };

            // filter entries
            Func<EntryResponseDto, bool> expenseEntryFilteringPredicate = (x) =>
            {
                if (x.IsTransfer && transfersToAccountsBlackList.Contains(x.Transaction.Account))
                {
                    return false;
                }
                if (categoriesBlackList.Contains(x.Category))
                {
                    return false;
                }
                return true;
            };
            var expenseEntriesFilteredOut = expenseEntries.Where(x => !expenseEntryFilteringPredicate(x)).ToList();
            var expenseEntriesFiltered = expenseEntries.Where(x => expenseEntryFilteringPredicate(x)).ToList();

            // build report
            decimal totalRegularExpenes = expenseEntriesFiltered.Aggregate(0m, (accum, curr) => accum + curr.Amount);
            decimal totalRegularIncome = incomeEntries.Aggregate(0m, (accum, curr) => accum + curr.Amount);

            int a = 1;
        }

        #region Private members


        private static IConfiguration GetConfiguration()
        {
            DotNetEnv.Env.Load(".env");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{HostingEnvironmentHelper.Environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var configuration = builder.Build();

            //// validate ApplicationSettings
            //var config = configuration.Get<ApplicationSettings>();
            //try
            //{
            //    Log.Information("Validating configuration...");
            //    CustomValidationHelper.Validate(config);
            //    Log.Information("Configuration is valid.");
            //}
            //catch (ValidationErrorException ex)
            //{
            //    throw new Exception("Configuration validation failed. Check appsettings.json, appsettings.<environment>.json files.", ex);
            //}

            return configuration;
        }

        private static Serilog.ILogger GetSerilogLogger(IConfiguration configuration, ApplicationSettings config)
        {

            var logger = new LoggerConfiguration()
               .Enrich.WithProperty("ApplicationContext", _appName) //define the context in logged data
               .Enrich.WithProperty("ApplicationEnvironment", HostingEnvironmentHelper.Environment) //define the environment
               .Enrich.FromLogContext() //allows to use specific context if nessesary
               .ReadFrom.Configuration(configuration);

            if (HostingEnvironmentHelper.IsDevelopmentLocalhost())
            {
                // write to file for development purposes
                logger.WriteTo.File(
                    path: Path.Combine("./serilog-logs/local-logs.txt"),
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100mb
                    restrictedToMinimumLevel: LogEventLevel.Warning
                );
            }

            return logger.CreateLogger();
        }

        #endregion
    }
}
