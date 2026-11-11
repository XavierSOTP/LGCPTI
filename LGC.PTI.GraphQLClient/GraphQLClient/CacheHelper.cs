using SoftSolute.Common.Utilities.Cache;
using System;
using System.Runtime.Caching;

namespace LGC.PTI.Common
{
    public static class CacheHelper
    {
        public static void AddToBackOff(string key,
            string domain,
            int timeInCache,
            CacheExpiration cacheExpiration = CacheExpiration.Absolute)
        {
            if (string.IsNullOrEmpty(key)) return;
            MemoryCacheSS.AddItem(
                key,
                domain,
                timeInCache,
                cacheExpiration);
        }

        public static void AddToBackOff(string key,
            string domain,
            int timeInCache,
            Action<CacheEntryRemovedArguments> onRemoved,
            CacheExpiration cacheExpiration = CacheExpiration.Absolute)
        {
            if (string.IsNullOrEmpty(key)) return;
            MemoryCacheSS.AddItem(
                key,
                domain,
                timeInCache,
                onRemoved,
                cacheExpiration);
        }

        public static bool ExistInBackOff(string key, string domain)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return MemoryCacheSS.Exist(key, domain);
        }

    }
}