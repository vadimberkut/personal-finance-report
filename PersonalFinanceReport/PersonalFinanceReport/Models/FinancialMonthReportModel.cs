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
    }
}
