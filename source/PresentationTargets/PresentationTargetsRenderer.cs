using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Sitecore;
using Sitecore.Analytics.Pipelines.GetRenderingRules;
using Sitecore.Analytics.Pipelines.RenderingRuleEvaluated;
using Sitecore.Layouts;
using Sitecore.Mvc.Presentation;
using Sitecore.Rules;
using Sitecore.Rules.ConditionalRenderings;

namespace PresentationTargets
{
    public class PresentationTargetsRenderer : ItemRenderer
    {
        public RenderingParameters Params { get; set; }
        protected override List<Rendering> GetRenderings()
        {
            var renderings = new List<Rendering>();

            if (Params == null)
            {
                Params = new RenderingParameters(string.Empty);
            }

            var paramPlaceholders = Params.Where(k => k.Key.Equals(Constants.Settings.PlaceholdersParameterKey, StringComparison.CurrentCultureIgnoreCase)).Select(v => v.Value.ToLowerInvariant());
            var paramRenderings = Params.Where(k => k.Key.Equals(Constants.Settings.RenderingsParameterKey, StringComparison.CurrentCultureIgnoreCase)).Select(v => v.Value.ToLowerInvariant());
            var paramPersonalization = Params.Where(k => k.Key.Equals(Constants.Settings.PersonalizationRules, StringComparison.CurrentCultureIgnoreCase)).Select(v => v.Value.ToLowerInvariant());
            
            var refs = Item.Visualization.GetRenderings(Context.Device, false);
            if (!refs.Any())
                return null;

            var renderingReferences = refs.Where(r => !(Context.Database.GetItem(r.RenderingID).TemplateID.ToString() == Constants.Settings.RenderingTemplateId && string.IsNullOrWhiteSpace(r.Settings.DataSource))).ToList();

            if (!renderingReferences.Any())
                return null;
            
            if (paramPlaceholders.Any())
            {
                var paramPipe = paramPlaceholders.FirstOrDefault();

                if (paramPipe != null)
                {
                    var targetPlaceholders = paramPipe.Split('|').ToList();
                
                    foreach (var renderingReference in renderingReferences)
                    {
                        foreach (var targetPlaceholder in targetPlaceholders)
                        {
                            if (renderingReference.Placeholder.Contains(targetPlaceholder))
                            { 
                                var currentRendering = new Rendering
                                {
                                    RenderingItemPath = renderingReference.RenderingID.ToString(),
                                    Parameters = new RenderingParameters(HttpUtility.UrlDecode(renderingReference.Settings.Parameters)),
                                    DataSource = renderingReference.Settings.DataSource
                                };

                                if (paramPersonalization.Any())
                                {
                                    var paramPersonalize = paramPersonalization.FirstOrDefault();

                                    if (paramPersonalize != null && paramPersonalize == "1")
                                    {
                                        PersonalizeRendering(renderingReference, currentRendering);
                                    }
                                }

								renderings.Add(currentRendering);
							}
                        }
                    }
                }
            }
            else
            {
                foreach (var renderingReference in renderingReferences)
                {
                    var currentRendering = new Rendering
                    {
                        RenderingItemPath = renderingReference.RenderingID.ToString(),
                        Parameters = new RenderingParameters(HttpUtility.UrlDecode(renderingReference.Settings.Parameters)),
                        DataSource = renderingReference.Settings.DataSource
                    };

                    if (paramPersonalization.Any())
                    {
                        var paramPersonalize = paramPersonalization.FirstOrDefault();

                        if (paramPersonalize != null && paramPersonalize == "1")
                        {
                            PersonalizeRendering(renderingReference, currentRendering);
                        }
                    }

					renderings.Add(currentRendering);
				}
            }

            //Rendering Filtering
            if (paramRenderings.Any())
            {
                var paramPipe = paramRenderings.FirstOrDefault();
                var filteredRenderings = new List<Rendering>();

                if (paramPipe != null)
                {
                    var targetRenderings = paramPipe.Split('|').ToList();
                    filteredRenderings.AddRange(from targetRenderingTemplateId in targetRenderings from rendering in renderings let renderingId = rendering.RenderingItem.ID.ToString() where renderingId.ToLowerInvariant().Equals(targetRenderingTemplateId.ToLowerInvariant()) select rendering);
                    renderings = filteredRenderings;
                }
            }

            return renderings;
        }
        private void PersonalizeRendering(RenderingReference renderingReference, Rendering currentRendering)
        {
	        var renderingsRuleContext = new ConditionalRenderingsRuleContext(new List<RenderingReference>
	        {
		        renderingReference
	        }, renderingReference);

            var renderingRulesArgs = new GetRenderingRulesArgs(Item, renderingReference);
            GetRenderingRulesPipeline.Run(renderingRulesArgs);
            var ruleList = renderingRulesArgs.RuleList;

            ruleList.Evaluated += RulesEvaluatedHandler;
            ruleList.RunFirstMatching(renderingsRuleContext);

            var reference = renderingsRuleContext.References.Find(r => r.UniqueId == renderingsRuleContext.Reference.UniqueId);

            if (reference != null)
            {
                TransferRenderingItem(currentRendering, reference);
                TransferDataSource(currentRendering, reference);
            }
        }

        private static void RulesEvaluatedHandler(RuleList<ConditionalRenderingsRuleContext> ruleList, ConditionalRenderingsRuleContext ruleContext, Rule<ConditionalRenderingsRuleContext> rule)
        {
            RenderingRuleEvaluatedPipeline.Run(new RenderingRuleEvaluatedArgs(ruleList, ruleContext, rule));
        }

        private static void TransferDataSource(Rendering rendering, RenderingReference reference)
        {
            var dataSource = reference.Settings.DataSource;

            if (string.IsNullOrEmpty(dataSource) || dataSource.Equals(rendering.DataSource))
            {
                return;
            }

            rendering.DataSource = dataSource;

            var item = reference.Database.GetItem(dataSource);
            if (item == null)
            {
                return;
            }

            rendering.Item = item;
        }

        private static void TransferRenderingItem(Rendering rendering, RenderingReference reference)
        {
            if (reference.RenderingItem == null)
            {
                return;
            }

            rendering.RenderingItem = reference.RenderingItem;
        }
    }
}