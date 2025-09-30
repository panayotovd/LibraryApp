using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LibraryApp.Helpers
{
    public static class TempDataExtensions
    {
        public static void Success(this ITempDataDictionary td, string m) => td["Success"] = m;
        public static void Error(this ITempDataDictionary td, string m) => td["Error"] = m;
        public static void Warning(this ITempDataDictionary td, string m) => td["Warning"] = m;
        public static void Info(this ITempDataDictionary td, string m) => td["Info"] = m;
    }
}

