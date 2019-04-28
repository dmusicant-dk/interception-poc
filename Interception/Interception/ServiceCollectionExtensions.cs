using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Interception.Interception
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddControllersAsServices(this IServiceCollection services, IEnumerable<Type> controllerTypes)
        {
            foreach (var type in controllerTypes)
            {
                services.AddTransient(type);
            }

            return services;
        }
    }
}