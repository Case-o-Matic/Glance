using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glance.Editor.Core.Graphics
{
    public class ServiceContainer : IServiceProvider
    {
        private Dictionary<Type, object> services;

        public ServiceContainer()
        {
            services = new Dictionary<Type, object>();
        }

        public void AddService<T>(T service)
        {
            services.Add(typeof(T), service);
        }

        public object GetService(Type serviceType)
        {
            object service;
            services.TryGetValue(serviceType, out service);

            return service;
        }
    }
}
