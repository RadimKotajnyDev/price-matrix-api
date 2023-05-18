using LTD.Data.LTDLive;
using LTD.Data.LTDLive.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceMatrixApi.Model;

namespace PriceMatrixApi.Controllers
{
    [ApiController]
    [Route("pricematrix")]
    public class PriceMatrixController : ControllerBase
    {
        private readonly LTDLiveContext context;

        public PriceMatrixController(LTDLiveContext context)
        {
            this.context = context;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Model.PriceMatrixInfo> Get(int id)
        {
            var matrix = await context.PriceMatrixes
                                 .Include(pm => pm.PriceMatrixRuleSets)
                                 .ThenInclude(rs => rs.PriceMatrixRules)
                                 .FirstAsync(pm => pm.PriceMatrixId == id);

            return new Model.PriceMatrixInfo
            {
                Id = id,
                Name = matrix.Name,
                RuleSets = matrix.PriceMatrixRuleSets
                                 .OrderByDescending<PriceMatrixRuleSet, int>(d => d.Priority)
                                 .Select(RuleSetInfoMapping())
                                 .ToList()
            };
        }

        [HttpPost]
        [Route("{matrixId}/ruleset/{id}/priority/{direction}")]
        public async Task<IActionResult> ChangePriority(int matrixId, int id, int direction)
        {
            if (direction == 0)
                return Ok();

            var rs0 = await context.PriceMatrixRuleSets.SingleAsync(a => a.PriceMatrixRuleSetId == id);
            
            var rs1 = direction > 0
                        ? await context.PriceMatrixRuleSets.Where(c => c.PriceMatrixId == matrixId).OrderBy(b => b.Priority).FirstOrDefaultAsync(a => a.Priority > rs0.Priority)
                        : await context.PriceMatrixRuleSets.Where(c => c.PriceMatrixId == matrixId).OrderByDescending(b => b.Priority).FirstOrDefaultAsync(a => a.Priority < rs0.Priority);

            if (rs1 == null)
                return BadRequest();

            // swap priorities
            int rs0Priority = rs0.Priority;
            rs0.Priority = rs1.Priority;
            rs1.Priority = rs0Priority;

            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [Route("{matrixId}/ruleset")]
        public async Task<IActionResult> AddRuleSet(int matrixId, RuleSetInfo info)
        {
            var priority = (await context.PriceMatrixRuleSets.Where(a => a.PriceMatrixId == matrixId).MaxAsync(n => (int?)n.Priority)) ?? 0;
            var ruleSet = new PriceMatrixRuleSet
            {
                PriceMatrixId = matrixId,
                Priority = (priority + 1)
            };

            FillRuleSetInfo(info, ruleSet);
            FillRules(info, ruleSet);

            context.PriceMatrixRuleSets.Add(ruleSet);

            await context.SaveChangesAsync();

            var createdRuleset = new[] { ruleSet }.Select(RuleSetInfoMapping()).First();

            return Created($"{matrixId}/ruleset/{ruleSet.PriceMatrixRuleSetId}", createdRuleset);
        }

        [HttpPut]
        [Route("{matrixId}/ruleset/{id}")]
        public async Task<RuleSetInfo> UpdateRuleSet(int matrixId, int id, RuleSetInfo info)
        {
            var ruleSet = await context.PriceMatrixRuleSets.Include(rs => rs.PriceMatrixRules).SingleAsync(a => a.PriceMatrixRuleSetId == id);
            var rules = ruleSet.PriceMatrixRules.ToList();
            if (rules.Count > 0)
            {
                context.PriceMatrixRules.RemoveRange(rules);
            }

            FillRuleSetInfo(info, ruleSet);

            if (ruleSet.PriceSelling == null && ruleSet.BookingFeePercent == null && ruleSet.BookingFeeAbsolute == null)
            {
                ruleSet.BookingFeeAbsolute = 0;
            }

            FillRules(info, ruleSet);

            await context.SaveChangesAsync();
            return new[] { ruleSet }.Select(RuleSetInfoMapping()).First();
        }

        [HttpDelete]
        [Route("{matrixId}/ruleset/{id}")]
        public async Task<IActionResult> RemoveRuleSet(int matrixId, int id)
        {
            var ruleSet = await context.PriceMatrixRuleSets.SingleAsync(a => a.PriceMatrixRuleSetId == id);
            context.PriceMatrixRuleSets.Remove(ruleSet);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private void FillRuleSetInfo(RuleSetInfo info, PriceMatrixRuleSet ruleSet)
        {
            ruleSet.LogicalOperatorId = info.LogicalOperatorId;
            ruleSet.Note = info.Note;
            ruleSet.OfferCode = info.OfferCode;
            ruleSet.PriceSelling = info.PriceSelling == 0 ? null : info.PriceSelling;
            ruleSet.BookingFeePercent = info.BookingFeePercent == 0 ? null : info.BookingFeePercent;
            ruleSet.BookingFeeAbsolute = info.BookingFeeAbsolute == 0 ? null : info.BookingFeeAbsolute;
            ruleSet.InsideCommissionRate = info.InsideCommissionRate;
        }

        private void FillRules(RuleSetInfo info, PriceMatrixRuleSet d)
        {
            if (info.Rules == null || info.Rules.Count == 0) return;

            foreach (var item in info.Rules)
            {
                d.PriceMatrixRules.Add(new PriceMatrixRule
                {
                    PriceMatrixRuleSetId = info.RuleSetId,
                    PriceMatrixFieldId = item.FieldId,
                    DateTimeValue = item.ValueDateTime,
                    IntegerValue = item.ValueInt,
                    StringValue = string.IsNullOrEmpty(item.ValueString) ? null : item.ValueString.Trim(),
                    CompareOperatorId = item.CompareOperatorId,
                });
            }
        }


        private static Func<PriceMatrixRuleSet, RuleSetInfo> RuleSetInfoMapping()
        {
            return a => new RuleSetInfo()
            {
                LogicalOperatorId = a.LogicalOperatorId,
                Note = a.Note,
                BookingFeeAbsolute = a.BookingFeeAbsolute,
                BookingFeePercent = a.BookingFeePercent,
                InsideCommissionRate = a.InsideCommissionRate,
                PriceSelling = a.PriceSelling,
                OfferCode = a.OfferCode,
                Priority = a.Priority,
                RuleSetId = a.PriceMatrixRuleSetId,
                Rules = a.PriceMatrixRules.Select<PriceMatrixRule, RuleInfo>(RuleInfoMapping()).ToList()
            };
        }

        private static Func<PriceMatrixRule, RuleInfo> RuleInfoMapping()
        {
            return e => new RuleInfo()
            {
                CompareOperatorId = e.CompareOperatorId,
                FieldId = e.PriceMatrixFieldId,
                ValueString = e.StringValue,
                ValueInt = e.IntegerValue,
                ValueDecimal = e.DecimalValue,
                ValueDateTime = e.DateTimeValue,
                Priority = e.Priority,
                RuleId = e.PriceMatrixRuleId,
                RuleSetId = e.PriceMatrixRuleSetId
            };
        }
    }
}