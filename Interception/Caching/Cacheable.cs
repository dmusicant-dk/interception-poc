using Interception.Interception;
using System;
using System.Collections.Generic;

namespace Interception.Caching
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class Cacheable : InterceptableCall
    {
        public String cacheName { get; set; } = "";
        public List<String> keyFromArgs { get; set; }

        public Cacheable(string cacheName)
        {
            this.cacheName = cacheName;
        }

        public Cacheable(string cacheName, List<string> keyFromArgs)
        {
            this.cacheName = cacheName;
            this.keyFromArgs = keyFromArgs;
        }
    }
}