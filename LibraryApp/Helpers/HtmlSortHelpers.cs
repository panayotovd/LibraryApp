using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;

namespace LibraryApp.Helpers
{
    public static class HtmlSortHelpers
    {
        public static IHtmlContent SortLink(
            this IHtmlHelper html,
            string column,
            string label,
            object filters) // може да е anonymous type, ViewBag.Filters, RouteValueDictionary и т.н.
        {
            // 1) Вземи всички текущи филтри безопасно като речник
            var dict = filters as RouteValueDictionary ?? new RouteValueDictionary(filters);

            string currentSort = dict.TryGetValue("sort", out var s) ? s?.ToString() ?? "" : "";
            string currentDir = dict.TryGetValue("dir", out var d) ? d?.ToString() ?? "asc" : "asc";
            int pageSize = 10;
            if (dict.TryGetValue("pageSize", out var ps) && int.TryParse(ps?.ToString(), out var psInt))
                pageSize = psInt;

            // 2) Следваща посока
            string nextDir = (currentSort == column && currentDir == "asc") ? "desc" : "asc";

            // 3) Построй новите route values: пазим всичко, но сменяме sort/dir и връщаме page=1
            var route = new RouteValueDictionary(dict
                .Where(kv => !string.Equals(kv.Key, "page", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(k => k.Key, v => v.Value));

            route["sort"] = column;
            route["dir"] = nextDir;
            route["page"] = 1;
            route["pageSize"] = pageSize;

            var urlHelperFactory = (IUrlHelperFactory)html.ViewContext.HttpContext.RequestServices
                .GetService(typeof(IUrlHelperFactory))!;
            var urlHelper = urlHelperFactory.GetUrlHelper(html.ViewContext);

            string url = urlHelper.Action("Index", route) ?? "#";

            // 4) Caret (▲/▼) само при активната колона
            string caret = (currentSort == column)
                ? (currentDir == "asc" ? " &#9650;" : " &#9660;")
                : "";

            return new HtmlString($"<a href='{url}' class='text-decoration-none'>{label}{caret}</a>");
        }
    }
}
