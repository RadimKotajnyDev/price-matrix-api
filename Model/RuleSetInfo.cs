using System.Collections.Generic;

namespace PriceMatrixApi.Model
{
    public class RuleSetInfo
    {
        public int RuleSetId { get; set; }

        public int LogicalOperatorId { get; set; }

        public int? Priority { get; set; }

        public List<RuleInfo> Rules { get; set; } = new List<RuleInfo>();

        public decimal? PriceSelling { get; set; }

        public decimal? BookingFeePercent { get; set; }

        public decimal? BookingFeeAbsolute { get; set; }

        public decimal? InsideCommissionRate { get; set; }

        public string? Note { get; set; }

        public string OfferCode { get; set; }
    }
}