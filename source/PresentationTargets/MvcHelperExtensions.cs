using System.IO;
using System.Web;

using Sitecore.Data.Items;
using Sitecore.Mvc.Helpers;
using Sitecore.Mvc.Pipelines;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;
using Sitecore.Mvc.Presentation;

namespace PresentationTargets
{
    public static class MvcHelperExtensions
    {  
        public static HtmlString PresentationTargetsRendering(this SitecoreHelper helper, Item item, object parameters)
        {
            var rendering = GetRendering("Item", parameters, "DataSource", item.ID.ToString());

            var renderingParams = (RenderingParameters)parameters;

            rendering.Renderer = new PresentationTargetsRenderer
            { 
                Item = item,
                Params = renderingParams
            };

            var stringWriter = new StringWriter();
            PipelineService.Get().RunPipeline("mvc.renderRendering", new RenderRenderingArgs(rendering, stringWriter));
            return new HtmlString(stringWriter.ToString());
        }

        private static Rendering GetRendering(string renderingType, object parameters, params string[] defaultValues)
        {
            var rendering = new Rendering { RenderingType = renderingType };
            var index = 0;

            while (index < defaultValues.Length - 1)
            {
                rendering[defaultValues[index]] = defaultValues[index + 1];
                index += 2;
            }
            return rendering;
        }
    }
}
