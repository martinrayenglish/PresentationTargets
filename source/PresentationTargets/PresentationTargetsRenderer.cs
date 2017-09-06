using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Sitecore;
using Sitecore.Mvc.Presentation;
using Sitecore.Layouts;

namespace PresentationTargets
{
    public class PresentationTargetsRenderer : ItemRenderer
    {
        public RenderingParameters Params { get; set; }
        protected override List<Rendering> GetRenderings()
        {
            var renderRefs = new List<Sitecore.Layouts.RenderingReference>();


            var renderings = new List<Rendering>();
            var paramPlaceholders = Params.Where(k => k.Key.Equals(Constants.Settings.PlaceholdersParameterKey, StringComparison.CurrentCultureIgnoreCase)).Select(v => v.Value.ToLowerInvariant());
            var paramRenderings = Params.Where(k => k.Key.Equals(Constants.Settings.RenderingsParameterKey, StringComparison.CurrentCultureIgnoreCase)).Select(v => v.Value.ToLowerInvariant());

            var refs = Item.Visualization.GetRenderings(Context.Device, false).Where(x => !x.RenderingID.IsNull);
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
                    var targetPlaceholders = paramPipe.ToLowerInvariant().Split('|').ToList();
                    renderRefs.AddRange(renderingReferences.Where(p => targetPlaceholders.Contains(p.Placeholder.ToLowerInvariant().Split('/').Last())));
                }
            }
            //Rendering Filtering
            if (paramRenderings.Any())
            {
                var paramPipe = paramRenderings.FirstOrDefault();
                if (paramPipe != null)
                {
                    var targetRenderings = paramPipe.ToLowerInvariant().Split('|').ToList();
                    renderRefs = renderRefs.Where(r => targetRenderings.Any(tr => tr == r.RenderingItem.ID.ToString().ToLowerInvariant())).ToList();
                    //renderRefs=(from targetRenderingTemplateId in targetRenderings from rendering in renderRefs let renderingId = rendering.RenderingItem.ID.ToString() where renderingId.ToLowerInvariant().Equals(targetRenderingTemplateId.ToLowerInvariant()) select rendering).ToList();
                    //renderings = filteredRenderings;
                }
            }
            renderings.AddRange(renderRefs.Select(r => GetPersonalizedRendering(r)).Where(r => r != null));
            return renderings;
        }

        private Rendering GetPersonalizedRendering(RenderingReference reference)
        {
            if (reference == null)
                return null;
            if (reference.Settings == null)
                return null;
            var renderingItemPath = reference.RenderingID.ToString();
            var parameters = new RenderingParameters(HttpUtility.UrlDecode(reference.Settings.Parameters));
            var dataSource = reference.Settings.DataSource;
            if (reference.Settings.Rules == null || reference.Settings.Rules.Count == 0)
            {
                return new Rendering
                {
                    RenderingItemPath = renderingItemPath,
                    Parameters = parameters,
                    DataSource = dataSource
                };
            }
            if (reference.Settings.Rules != null && reference.Settings.Rules.Count > 0)
            {
                foreach (var r in reference.Settings.Rules.Rules)
                {
                    var ruleContext = new Sitecore.Rules.ConditionalRenderings.ConditionalRenderingsRuleContext(new List<RenderingReference>() { reference }, reference)
                    {
                        Item = Sitecore.Context.Item
                    };
                    if (r.Evaluate(ruleContext))
                    {
                        foreach (var a in r.Actions)
                        {
                            var setDataSourceAction = a as Sitecore.Rules.ConditionalRenderings.SetDataSourceAction<Sitecore.Rules.ConditionalRenderings.ConditionalRenderingsRuleContext>;
                            if (setDataSourceAction != null)
                            {
                                dataSource = setDataSourceAction.DataSource;
                                continue;
                            }
                            var setRenderingAction = a as Sitecore.Rules.ConditionalRenderings.SetRenderingAction<Sitecore.Rules.ConditionalRenderings.ConditionalRenderingsRuleContext>;
                            if (setRenderingAction != null)
                            {
                                renderingItemPath = setRenderingAction.RenderingItem;
                                continue;
                            }
                            var hideRenderingAction = a as Sitecore.Rules.ConditionalRenderings.HideRenderingAction<Sitecore.Rules.ConditionalRenderings.ConditionalRenderingsRuleContext>;
                            if (hideRenderingAction != null)
                            {
                                return null;
                            }
                        }
                        break;
                    }
                }
            }
            return new Rendering
            {
                RenderingItemPath = renderingItemPath,
                Parameters = parameters,
                DataSource = dataSource
            };
        }
    }
}