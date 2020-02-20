using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using PersonalFinanceReport.Helpers;
using PersonalFinanceReport.Toshl.Dto;
using PersonalFinanceReport.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PersonalFinanceReport.Toshl
{
    public class ToshHttpApiClient
    {
        public const int DefaultPerPage = 200;
        public const int MaxPerPage = 500;
        public const string DateFormat = "yyyy-MM-dd";

        private readonly ILogger<ToshHttpApiClient> _logger;
        private readonly ToshlSettings _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpUtil _httpUtil;

        private HttpClient _httpClient
        {
            get
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, _config.PersonalApiToken);
                httpClient.BaseAddress = new Uri(_config.HttpApiUrl);
                return httpClient;
            }
        }

        public ToshHttpApiClient(
            ILogger<ToshHttpApiClient> logger,
            ToshlSettings config,
            IHttpClientFactory httpClientFactory,
            HttpUtil httpUtil
        )
        {
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _httpUtil = httpUtil;
        }

        public async Task<MeResponseDto> MeAsync()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/me");
            var httpResponse = await _httpClient.SendAsync(requestMessage);

            _httpUtil.EnsureSuccessStatusCode(httpResponse);
            string httpContent = await httpResponse.Content.ReadAsStringAsync();
            var dto = JsonConvert.DeserializeObject<MeResponseDto>(httpContent);
            return dto;
        }



        #region Accounts

        public async Task<IEnumerable<AccountResponseDto>> AccountListAsync(
          int page = 0,
          int perPage = DefaultPerPage,
          string status = "active",
          bool includeDeleted = false
        )
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/accounts?page={page}&per_page={perPage}&status={status}&include_deleted={includeDeleted}");
            var httpResponse = await _httpClient.SendAsync(requestMessage);

            _httpUtil.EnsureSuccessStatusCode(httpResponse);
            string httpContent = await httpResponse.Content.ReadAsStringAsync();
            var dto = JsonConvert.DeserializeObject<IEnumerable<AccountResponseDto>>(httpContent);
            return dto;
        }

        #endregion

        #region Categories

        // https://developer.toshl.com/docs/categories/list/
        public async Task<IEnumerable<CategoryResponseDto>> CategoryListAsync(
            int page = 0,
            int perPage = DefaultPerPage,
            string since = null,
            string type = null, // expense, income
            string search = null,
            bool includeDeleted = false
        )
        {
            string queryString = HttpHelper.BuildQueryString(new Dictionary<string, object>()
            {
                { "page", page },
                { "per_page", perPage },
                { "since", since },
                { "type", type },
                { "search", search },
                { "include_deleted", includeDeleted },
            });

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, HttpHelper.AddQueryString("/categories", queryString));
            var httpResponse = await _httpClient.SendAsync(requestMessage);

            _httpUtil.EnsureSuccessStatusCode(httpResponse);
            string httpContent = await httpResponse.Content.ReadAsStringAsync();
            var dto = JsonConvert.DeserializeObject<IEnumerable<CategoryResponseDto>>(httpContent);
            return dto;
        }

        #endregion

        #region Entries

        // https://developer.toshl.com/docs/entries/list/
        public async Task<IEnumerable<EntryResponseDto>> EntryListAsync(
            string fromDate, // including
            string toDate, // including
            string type, // expense, income, transaction
            List<string> accounts = null,
            List<string> categories = null,
            List<string> notCategories = null,
            int page = 0,
            int perPage = DefaultPerPage,
            bool includeDeleted = false
        )
        {
            string queryString = HttpHelper.BuildQueryString(new Dictionary<string, object>()
            {
                { "from", fromDate },
                { "to", toDate },
                { "type", type },
                { "accounts",  string.Join(",", accounts ?? new List<string>()) },
                { "categories", string.Join(",", categories ?? new List<string>()) },
                { "!categories", string.Join(",", notCategories ?? new List<string>()) },
                { "page", page },
                { "per_page", perPage },
                { "include_deleted", includeDeleted },
            });

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, HttpHelper.AddQueryString("/entries", queryString));
            var httpResponse = await _httpClient.SendAsync(requestMessage);

            _httpUtil.EnsureSuccessStatusCode(httpResponse);
            string httpContent = await httpResponse.Content.ReadAsStringAsync();
            var dto = JsonConvert.DeserializeObject<IEnumerable<EntryResponseDto>>(httpContent);
            dto = dto.OrderByDescending(x => x.DateAt).ToList();
            return dto;
        }

        public async Task<IEnumerable<EntryResponseDto>> EntryListAllAsync(
            string fromDate,
            string toDate,
            string type, // expense, income, transaction
            List<string> accounts = null,
            List<string> categories = null,
            List<string> notCategories = null,
            bool includeDeleted = false
        )
        {
            var entries = new List<EntryResponseDto>();
            const int maxPages = 20;
            for (int currentPage = 0; currentPage < maxPages; currentPage++)
            {
                var localEntries = await EntryListAsync(
                    fromDate: fromDate,
                    toDate: toDate,
                    type: type,
                    accounts: accounts,
                    categories: categories,
                    notCategories: notCategories,
                    page: currentPage,
                    perPage: MaxPerPage,
                    includeDeleted: includeDeleted
                );
                entries.AddRange(localEntries);
                if (localEntries.Count() == 0 || localEntries.Count() < MaxPerPage)
                {
                    break;
                }
            }
            return entries;
        }

        #endregion
    }
}
