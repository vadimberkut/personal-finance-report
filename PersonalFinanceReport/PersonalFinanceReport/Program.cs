using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Extensions;
using PersonalFinanceReport.Models;
using PersonalFinanceReport.Toshl;
using PersonalFinanceReport.Toshl.Dto;
using PersonalFinanceReport.Utils;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PersonalFinanceReport
{
    // https://joshclose.github.io/
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
            // Console.ReadKey();
        }

        private static async Task RunAsync(IServiceProvider serviceProvider, ApplicationSettings config)
        {
            // doesn't work for some reason
            var logger = serviceProvider.GetRequiredService<ILogger<ToshHttpApiClient>>();

            var currentTz = DateTimeZoneProviders.Tzdb["Europe/Simferopol"];

            var reportFromLocal = LocalDateTime.FromDateTime(DateTime.Now).With(DateAdjusters.StartOfMonth);
            var reportToLocal = LocalDateTime.FromDateTime(DateTime.Now).With(DateAdjusters.EndOfMonth);
            reportFromLocal = reportFromLocal.Minus(Period.FromTicks(reportFromLocal.TimeOfDay.TickOfDay));
            reportToLocal = reportToLocal.Plus(Period.FromHours(24) - Period.FromTicks(reportToLocal.TimeOfDay.TickOfDay));

            var toshHttpApiClient = serviceProvider.GetRequiredService<ToshHttpApiClient>();

            //// Current month report
            //var monthReport = await BuildReportForAPeriodAsync(
            //    serviceProvider, config, toshHttpApiClient,
            //    reportFromLocal,
            //    reportToLocal,
            //    currentTz
            //);

            //Log.Information($"Month report: {reportFrom} - {reportTo}");
            //Log.Information($"TotalExpenes: {monthReport.TotalExpenes}.");
            //Log.Information($"TotalRegularExpenes: {monthReport.TotalRegularExpenes}.");
            //Log.Information($"TotalUnregularExpenes: {monthReport.TotalUnregularExpenes}.");
            //Log.Information($"TotalIncome: {monthReport.TotalIncome}.");
            //Log.Information($"TotalRegularIncome: {monthReport.TotalRegularIncome}.");
            //Log.Information($"TotalUnregularIncome: {monthReport.TotalUnregularIncome}.");
            //Log.Information($"");

            // All time report
            var allReportFromLocal = LocalDateTime.FromDateTime(new DateTime(2017, 01, 01)).With(DateAdjusters.StartOfMonth);
            // var allReportFromLocal = LocalDateTime.FromDateTime(new DateTime(2019, 12, 01)).With(DateAdjusters.StartOfMonth);
            var allReportToLocal = LocalDateTime.FromDateTime(DateTime.Now).With(DateAdjusters.EndOfMonth);
            var allReports = new List<FinancialMonthReportModel>();
            for (LocalDateTime fromLocal = allReportFromLocal; fromLocal < allReportToLocal;)
            {
                var month = Period.FromMonths(1);
                LocalDateTime toLocal = LocalDateTime.FromDateTime(fromLocal.ToDateTimeUnspecified()).With(DateAdjusters.EndOfMonth);
                ZonedDateTime fromUtc = fromLocal.InZoneLeniently(currentTz).ToInstant().InUtc();
                ZonedDateTime toUtc = toLocal.InZoneLeniently(currentTz).ToInstant().InUtc();

                Log.Information($"Querying {fromLocal} - {toLocal}...");
                var report = await BuildReportForAPeriodAsync(
                   serviceProvider, config, toshHttpApiClient,
                   fromLocal, 
                   toLocal,
                   currentTz
                );

                allReports.Add(report);

                // increment by month
                LocalDateTime nextMonthDayLocal = toLocal.PlusDays(1);
                fromLocal = nextMonthDayLocal.With(DateAdjusters.StartOfMonth);
            }

            // calc grand totals
            allReports = allReports.Select((x, i) =>
            {
                //var prevReports = allReports.GetRange(0, i);
                //x.GrandTotalTradingExpenes = prevReports.Aggregate((decimal)0 , (accum, curr) =>
                //{
                //    accum += curr.GrandTotalTradingExpenes;
                //    return accum;
                //});

                var prevReport = (i == 0 ? null : allReports[i - 1]);

                x.GrandTotalTradingExpenes += (prevReport == null ? 0 + x.TotalTradingExpenes : prevReport.GrandTotalTradingExpenes + x.TotalTradingExpenes);
                x.GrandTotalTradingIncome += (prevReport == null ? 0 + x.TotalTradingIncome : prevReport.GrandTotalTradingIncome + x.TotalTradingIncome);

                return x;
            }).ToList();

            // save into CSV
            var csvModels = allReports.Select(x => new FinancialMonthReportCsvModel()
            {
                From = x.From.ToString("yyyy-MM-dd"),
                To = x.To.ToString("yyyy-MM-dd"),
                PeriodName = x.PeriodName,
                TotalExpenes = x.TotalExpenes,
                TotalRegularExpenes = x.TotalRegularExpenes,
                TotalUnregularExpenes = x.TotalUnregularExpenes,
                TotalIncome = x.TotalIncome,
                TotalRegularIncome = x.TotalRegularIncome,
                TotalUnregularIncome = x.TotalUnregularIncome,

                TotalTradingExpenes = x.TotalTradingExpenes,
                TotalTradingIncome = x.TotalTradingIncome,

                GrandTotalTradingExpenes = x.GrandTotalTradingExpenes,
                GrandTotalTradingIncome = x.GrandTotalTradingIncome,
            });

            string exportPath = "./data-reports";
            Directory.CreateDirectory(exportPath);
            using (var streamWriter = new StreamWriter(Path.Combine(exportPath, $"full-history-report-{allReportFromLocal.ToDateTimeUnspecified().ToString("yyyy-MM-dd")}--{allReportToLocal.ToDateTimeUnspecified().ToString("yyyy-MM-dd")}.csv")))
            {
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteHeader<FinancialMonthReportCsvModel>();
                    await csvWriter.NextRecordAsync();
                    await csvWriter.WriteRecordsAsync(csvModels);
                    await csvWriter.FlushAsync();
                }
            }
            Log.Information($"");
        }


        private static IEnumerable<AccountResponseDto> _accounts = null;
        private static IEnumerable<CategoryResponseDto> _expenseCategories = null;
        private static IEnumerable<CategoryResponseDto> _incomeCategories = null;
        private static async Task<FinancialMonthReportModel> BuildReportForAPeriodAsync(
            IServiceProvider serviceProvider,
            ApplicationSettings config,
            ToshHttpApiClient toshHttpApiClient,
            LocalDateTime fromLocal,
            LocalDateTime toLocal,
            DateTimeZone dateTimeZone
        )
        {
            ZonedDateTime fromUtc = fromLocal.InZoneLeniently(dateTimeZone).ToInstant().InUtc();
            ZonedDateTime toUtc = toLocal.InZoneLeniently(dateTimeZone).ToInstant().InUtc();

            // var me = await toshHttpApiClient.MeAsync();

            const string cashAccountName = "Cash";
            const string dreamsAccountName = "Dreams";
            const string cryptoAccountName = "Crypto";

            const string taxesCategoryName = "Taxes";
            const string loansCategoryName = "Loans";

            const string depositCategoryName = "Deposit";
            const string tradingCategoryName = "Trading";
            const string reimbursementsCategoryName = "Reimbursements";
            const string sellingCategoryName = "Selling";

            _accounts = _accounts ?? await toshHttpApiClient.AccountListAsync();
            var cashAccount = _accounts.Single(x => x.Name == cashAccountName);
            var dreamsAccount = _accounts.Single(x => x.Name == dreamsAccountName);
            var cryptoAccount = _accounts.Single(x => x.Name == cryptoAccountName);

            _expenseCategories = _expenseCategories ?? await toshHttpApiClient.CategoryListAsync(
               type: "expense",
               page: 0,
               perPage: ToshHttpApiClient.MaxPerPage
            );
            _incomeCategories = _incomeCategories ?? await toshHttpApiClient.CategoryListAsync(
              type: "income",
              page: 0,
              perPage: ToshHttpApiClient.MaxPerPage
           );

            var expenseEntries = await toshHttpApiClient.EntryListAllAsync(
                fromDate: fromUtc.Date.ToString("yyyy-MM-dd", null),
                toDate: toUtc.Date.ToString("yyyy-MM-dd", null),
                type: "expense",
                accounts: new List<string>() { cashAccount.Id },
                notCategories: new List<string>() { }
            );
            var incomeEntries = await toshHttpApiClient.EntryListAllAsync(
               fromDate: fromUtc.Date.ToString("yyyy-MM-dd", null),
               toDate: toUtc.Date.ToString("yyyy-MM-dd", null),
               type: "income",
               accounts: new List<string>() { cashAccount.Id },
               notCategories: new List<string>() { }
            );

            // entry filters
            var transfersToAccountsBlackList = new List<string>()
            {
                cryptoAccount.Id,
            };
            var expenseUnregularCategories = new List<string>()
            {
                _expenseCategories.Single(x => x.Name == taxesCategoryName).Id,
                _expenseCategories.Single(x => x.Name == loansCategoryName).Id,
            };
            var incomeUnregularCategories = new List<string>()
            {
                _incomeCategories.Single(x => x.Name == depositCategoryName).Id,
                _incomeCategories.Single(x => x.Name == tradingCategoryName).Id,
                _incomeCategories.Single(x => x.Name == reimbursementsCategoryName).Id,
                _incomeCategories.Single(x => x.Name == sellingCategoryName).Id,
            };
            var incomeTradingCategories = new List<string>()
            {
                _incomeCategories.Single(x => x.Name == tradingCategoryName).Id,
            };

            // filter entries
            Func<EntryResponseDto, bool> expenseEntryFilteringPredicate = (x) =>
            {
                if (x.IsTransfer && transfersToAccountsBlackList.Contains(x.Transaction.Account))
                {
                    return false;
                }
                if (expenseUnregularCategories.Contains(x.Category))
                {
                    return false;
                }
                return true;
            };
            Func<EntryResponseDto, bool> incomeEntryFilteringPredicate = (x) =>
            {
                //if (x.IsTransfer && transfersToAccountsBlackList.Contains(x.Transaction.Account))
                //{
                //    return false;
                //}
                if (incomeUnregularCategories.Contains(x.Category))
                {
                    return false;
                }
                return true;
            };
            
            var expenseRegularEntries = expenseEntries.Where(x => expenseEntryFilteringPredicate(x)).ToList();
            var expenseUnregularEntries = expenseEntries.Where(x => !expenseEntryFilteringPredicate(x)).ToList();
            var incomeRegularEntries = incomeEntries.Where(x => incomeEntryFilteringPredicate(x)).ToList();
            var incomeUnregularEntries = incomeEntries.Where(x => !incomeEntryFilteringPredicate(x)).ToList();

            var expenseTradingEntries = expenseEntries.Where(x =>
            {
                if (x.IsTransfer && x.Transaction.Account == cryptoAccount.Id)
                {
                    return true;
                }
                return false;
            }).ToList();
            var incomeTradingEntries = incomeEntries.Where(x =>
            {
                if (x.IsTransfer && x.Transaction.Account == cashAccount.Id)
                {
                    return true;
                }
                if (incomeTradingCategories.Contains(x.Category))
                {
                    return true;
                }
                return false;
            }).ToList();

            // build report
            decimal totalExpenes = expenseEntries.Aggregate(0m, (accum, curr) => accum + curr.Amount);
            decimal totalRegularExpenes = expenseRegularEntries.Aggregate(0m, (accum, curr) => accum + curr.Amount);
            decimal totalUnregularExpenes = expenseUnregularEntries.Aggregate(0m, (accum, curr) => accum + curr.Amount);

            decimal totalIncome = incomeEntries.Aggregate(0m, (accum, curr) => accum + curr.Amount);
            decimal totalRegularIncome = incomeRegularEntries.Aggregate(0m, (accum, curr) => accum + curr.Amount);
            decimal totalUnregularIncome = incomeUnregularEntries.Aggregate(0m, (accum, curr) => accum + curr.Amount);

            decimal totalTradingExpenes = expenseTradingEntries.Aggregate(0m, (accum, curr) => accum + curr.Amount);
            decimal totalTradingIncome = incomeTradingEntries.Aggregate(0m, (accum, curr) => accum + curr.Amount);

            return new FinancialMonthReportModel()
            {
                From = fromLocal.ToDateTimeUnspecified(),
                To = toLocal.ToDateTimeUnspecified(),
                PeriodName = fromLocal.ToDateTimeUnspecified().ToString("yyyy MMMM"), // 2020 April
                TotalExpenes = totalExpenes,
                TotalRegularExpenes = totalRegularExpenes,
                TotalUnregularExpenes = totalUnregularExpenes,
                TotalIncome = totalIncome,
                TotalRegularIncome = totalRegularIncome,
                TotalUnregularIncome = totalUnregularIncome,

                TotalTradingExpenes = totalTradingExpenes,
                TotalTradingIncome = totalTradingIncome,
            };
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
