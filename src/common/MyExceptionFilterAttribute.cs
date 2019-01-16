using System.Web.Mvc;

namespace todo.common
{
    public class MyExceptionFilterAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            base.OnException(filterContext);

            //处理错误消息。跳转到一个错误页面
            LogHelper.WriteLog(filterContext.Exception.ToString());
            //页面跳转到错误页面
            filterContext.HttpContext.Response.Redirect("/Shared/Error");
        }
    }
}