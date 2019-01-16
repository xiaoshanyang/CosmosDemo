namespace todo
{
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;
    using todo.common;

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            DocumentDBRepository<todo.Models.Item>.Initialize();

            log4net.Config.XmlConfigurator.Configure();
            GlobalFilters.Filters.Add(new MyExceptionFilterAttribute());
        }
    }
}
