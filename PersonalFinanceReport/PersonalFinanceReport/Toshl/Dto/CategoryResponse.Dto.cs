using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalFinanceReport.Toshl.Dto
{
    // https://developer.toshl.com/docs/categories/
    public class CategoryResponseDto
    {
        public CategoryResponseDto()
        {

        }

        public string Id { get; set; }
        public string Name { get; set; }

        [JsonProperty("name_override")]
        public bool NameOverride { get; set; }

        public string Modified { get; set; }

        // expense, income, system
        public string Type { get; set; }
        public bool Deleted { get; set; }
        public CategoryCountsResponseDto Counts { get; set; }
        public object Extra { get; set; }
    }

    public class CategoryCountsResponseDto
    {
        [JsonProperty("entries")]
        public int Entries { get; set; }

        [JsonProperty("income_entries")]
        public int IncomeEntries { get; set; }

        [JsonProperty("expense_entries")]
        public int ExpenseEntries { get; set; }

        [JsonProperty("tags_used_with_category")]
        public int TagsUsedWithCategory { get; set; }

        [JsonProperty("income_tags_used_with_category")]
        public int IncomeTagsUsedWithCategory { get; set; }

        [JsonProperty("expense_tags_used_with_category")]
        public int ExpenseTagsUsedWithCategory { get; set; }

        [JsonProperty("tags")]
        public int Tags { get; set; }

        [JsonProperty("income_tags")]
        public int IncomeTags { get; set; }

        [JsonProperty("expense_tags")]
        public int ExpenseTags { get; set; }

        [JsonProperty("budgets")]
        public int Budgets { get; set; }

    }
}
