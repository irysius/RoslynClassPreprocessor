using System.Web;
using System.Web.Mvc;
using Test.Test;

namespace SampleMvcProject
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new NewAttribute());
        }
    }
}
