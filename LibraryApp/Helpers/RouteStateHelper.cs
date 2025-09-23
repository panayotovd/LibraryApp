namespace LibraryApp.Helpers
{
    public static class RouteStateHelper
    {
        public static RouteValueDictionary BuildFromRequest(HttpRequest request)
        {
            var rvd = new RouteValueDictionary();

            // 1) всичко от Query (както е)
            foreach (var kv in request.Query)
            {
                if (kv.Key.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
                rvd[kv.Key] = kv.Value.ToString();
            }

            // 2) само "неймспейснатите" скрити полета от Form: state_*
            if (request.HasFormContentType)
            {
                foreach (var kv in request.Form)
                {
                    var key = kv.Key;
                    if (!key.StartsWith("state_", StringComparison.OrdinalIgnoreCase)) continue;

                    var origKey = key.Substring("state_".Length);
                    if (origKey.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;

                    rvd[origKey] = kv.Value.ToString();
                }
            }

            return rvd;
        }
    }
}
