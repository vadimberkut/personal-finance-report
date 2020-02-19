using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalFinanceReport.Toshl.Dto
{
    public class EntryResponseDto
    {
        public EntryResponseDto()
        {
            Tags = new List<string>();
            Images = new List<object>();
            Reminders = new List<object>();
            @Readonly = new List<string>();
        }

        public string Id { get; set; }
        public decimal Amount { get; set; }
        public AccountCurrencyResponseDto Currency { get; set; }
        
        public string Date { get; set; }
        public DateTime DateAt => DateTime.ParseExact(Date, ToshHttpApiClient.DateFormat, null);

        public string Desc { get; set; }
        public string Account { get; set; }
        public string Category { get; set; }
        public List<string> Tags { get; set; }
        public object Location { get; set; }
        public string Created { get; set; }
        public string Modified { get; set; }
        public object Repeat { get; set; }

        /// <summary>
        /// If set, the entry is transfer
        /// </summary>
        public EntryTransactonResponseDto Transaction { get; set; }
        public bool IsTransfer => Transaction != null;

        public List<object> Images { get; set; }
        public List<object> Reminders { get; set; }
        public object Import { get; set; }
        public object Review { get; set; }
        public object Settle { get; set; }
        public object Split { get; set; }
        public List<string> @Readonly { get; set; }
        public bool Completed { get; set; }
        public bool Deleted { get; set; }
        public object Extra { get; set; }
    }

    public class EntryTransactonResponseDto
    {
        public string Id { get; set; }
        public decimal Amount { get; set; }

        /// <summary>
        /// Where transfered
        /// </summary>
        public string Account { get; set; }

        public AccountCurrencyResponseDto Currency { get; set; }
        public int MyProperty { get; set; }
    }
}
