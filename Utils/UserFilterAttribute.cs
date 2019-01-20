using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using XiaoBuPark.Tables;

namespace XiaoBuPark.Web.App_Start
{
    /// <summary>
    /// 整个网站session过滤器
    /// </summary>
    public class UserFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            S_UserTableT user = HttpContext.Current.Session["user"] as S_UserTableT;
            if (user == null)
            {
                if (IsAjax()) //若是ajax请求，头部添加请求超时信息
                {
                    HttpContext.Current.Response.ContentType = "text/html";
                    HttpContext.Current.Response.AddHeader("sessionstatus", "timeout");
                    HttpContext.Current.Response.End();
                }
                else  //否则返回登录页面
                {
                    filterContext.Result = new RedirectResult("/Login/Index");
                }
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// 是否是Ajax请求
        /// </summary>
        /// <returns></returns>
        public static bool IsAjax()
        {
            return HttpContext.Current.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}