using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalFinanceReport.Models
{
    public class FinancialMonthReportModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string PeriodName { get; set; }
        public decimal TotalExpenes { get; set; }
        public decimal TotalRegularExpenes { get; set; }
        public decimal TotalUnregularExpenes { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalRegularIncome { get; set; }
        public decimal TotalUnregularIncome { get; set; }

        public decimal TotalTradingExpenes { get; set; }
        public decimal TotalTradingIncome { get; set; }

        // Grand total: summed up results from prev periods
        public decimal GrandTotalTradingExpenes { get; set; } = 0;
        public decimal GrandTotalTradingIncome { get; set; } = 0;
    }
}
