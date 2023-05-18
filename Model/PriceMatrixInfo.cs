namespace PriceMatrixApi.Model
{
    public class PriceMatrixInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<RuleSetInfo> RuleSets { get; set; } = new List<RuleSetInfo>();
    }
}
