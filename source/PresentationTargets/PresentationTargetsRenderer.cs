using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Sitecore;
using Sitecore.Mvc.Presentation;

namespace PresentationTargets
{
    public class PresentationTargetsRenderer : ItemRenderer
    {
        public RenderingParameters Params { get; set; }
        protected override List<Rendering> GetRenderings()
        {
            var renderings = new List<Rendering>();
            var paramPlaceholders = Params.Where(k => k.Key.Equals(Constants.Settings.PlaceholdersParameterKey, StringComparison.CurrentCultureIgnoreCase)).Select(v => v.Value.ToLowerInvariant());
            var paramRenderings = Params.Where(k => k.Key.Equals(Constants.Settings.RenderingsParameterKey, StringComparison.CurrentCultureIgnoreCase)).Select(v => v.Value.ToLowerInvariant());

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
                
                    renderings.AddRange(renderingReferences.Where(p => targetPlaceholders.Contains(p.Placeholder.ToLowerInvariant())).Select(r => new Rendering
                    {
                        RenderingItemPath = r.RenderingID.ToString(),
                        Parameters = new RenderingParameters(HttpUtility.UrlDecode(r.Settings.Parameters)),
                        DataSource = r.Settings.DataSource
                    }));
                }
            }
            else
            {
                renderings.AddRange(renderingReferences.Select(r => new Rendering
                {
                    RenderingItemPath = r.RenderingID.ToString(),
                    Parameters = new RenderingParameters(HttpUtility.UrlDecode(r.Settings.Parameters)),
                    DataSource = r.Settings.DataSource
                }));
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
    }
}