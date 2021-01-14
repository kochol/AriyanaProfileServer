using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Server.Filters
{
    public enum TimeUnit
    {
        Second = 1,
        Minute = 60,
        Hour = 3600,
        Day = 86400
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ThrottleAttribute : ActionFilterAttribute
    {
        public TimeUnit TimeUnit { get; set; }
        public int Count { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var seconds = Convert.ToInt32(TimeUnit);

            var controller = filterContext.ActionDescriptor as ControllerActionDescriptor;
            Console.WriteLine(filterContext.HttpContext.Connection.RemoteIpAddress);

            var key = string.Join(
                "-",
                seconds,
                filterContext.HttpContext.Request.Method,
                controller.ControllerName,
                controller.ActionName,
                filterContext.HttpContext.Connection.RemoteIpAddress
            );

            // increment the cache value
            var cnt = 1;
            IMemoryCache cache = filterContext.HttpContext.RequestServices.GetService<IMemoryCache>();
            if (cache.TryGetValue(key, out cnt))
            {
                cnt++;
            }
            cache.Set(key, cnt, DateTime.UtcNow.AddSeconds(seconds));

            if (cnt > Count)
            {
                filterContext.Result = new ContentResult
                {
                    Content = "You are allowed to make only " + Count + " requests per " + TimeUnit.ToString().ToLower()
                };
                filterContext.HttpContext.Response.StatusCode = 429;
            }
        }
    }
}
