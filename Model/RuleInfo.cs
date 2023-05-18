using System;

namespace PriceMatrixApi.Model
{
    public class RuleInfo
    {
        public int RuleSetId { get; set; }

        public int RuleId { get; set; }

        public int FieldId { get; set; }

        public int CompareOperatorId { get; set; }

        public int? ValueInt { get; set; }

        public string? ValueString { get; set; }

        public DateTime? ValueDateTime { get; set; }

        public decimal? ValueDecimal { get; set; }

        public int? Priority { get; set; }

    }
}