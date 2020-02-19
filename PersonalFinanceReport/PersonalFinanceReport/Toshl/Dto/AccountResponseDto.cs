using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalFinanceReport.Toshl.Dto
{
    /// <summary>
    /// https://developer.toshl.com/docs/accounts/
    /// </summary>
    public class AccountResponseDto
    {
        public AccountResponseDto()
        {

        }

        public string Id { get; set; }
        public string Parent { get; set; }
        public string Name { get; set; }
        
        [JsonProperty("name_override")]
        public bool NameOverride { get; set; }

        public string Type { get; set; }
        public decimal Balance { get; set; }

        [JsonProperty("initial_balance")]
        public decimal InitialBalance { get; set; }
        
        public decimal Limit { get; set; }
        public AccountCurrencyResponseDto Currency { get; set; }

        [JsonProperty("daily_sum_median")]
        public AccountDailySumMedianResponseDto DailySumMedian { get; set; }
        
        public AccountAvgResponseDto Avg { get; set; }
        public string Status { get; set; }
        public int Order { get; set; }
        public string Modified { get; set; }
        public AccountGoalResponseDto Goal { get; set; }
        public object Connection { get; set; }
        public object Settle { get; set; }
        public object Billing { get; set; }
        public int Count { get; set; }
        public int Review { get; set; }
        public bool Deleted { get; set; }
        public bool Recalculated { get; set; }
        public object Extra { get; set; }
    }

    public class AccountDailySumMedianResponseDto
    {
        public decimal Expenses { get; set; }
        public decimal Incomes { get; set; }
    }

    public class AccountAvgResponseDto
    {
        public decimal Expenses { get; set; }
        public decimal Incomes { get; set; }
    }

    public class AccountGoalResponseDto
    {
        public decimal Amount { get; set; }
        public decimal Start { get; set; }
        public decimal End { get; set; }
    }
}
