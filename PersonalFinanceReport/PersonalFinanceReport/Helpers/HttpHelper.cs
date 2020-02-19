using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace PersonalFinanceReport.Helpers
{
    public static class HttpHelper
    {
        public static string BuildQueryStringParam<TValue>(string paramName, TValue paramValue) where TValue : struct
        {
            return $"{WebUtility.UrlEncode(paramName)}={WebUtility.UrlEncode(paramValue.ToString())}";
        }

        public static string BuildQueryStringParam<TValue>(string paramName, IEnumerable<TValue> paramValue) where TValue : struct
        {
            var parts = paramValue.Select(x => $"{WebUtility.UrlEncode(paramName)}[]={WebUtility.UrlEncode(x.ToString())}");
            return string.Join("&", parts);
        }

        /// <summary>
        /// Recursive
        /// </summary>
        public static string BuildQueryStringParam(string paramName, object paramValue, bool isArrayElement = false)
        {
            if(paramValue == null)
            {
                return string.Empty;
            }

            // value type, enum, string
            var paramType = paramValue.GetType();
            if(paramType == typeof(string) && String.IsNullOrEmpty(paramValue.ToString()))
            {
                return string.Empty;
            }
            if (paramType.IsValueType || paramType.IsEnum || paramType == typeof(string))
            {
                string arraySuffix = isArrayElement ? "[]" : "";
                return $"{WebUtility.UrlEncode(paramName)}{arraySuffix}={WebUtility.UrlEncode(paramValue.ToString().ToLowerInvariant())}";
            }

            // array
            if (paramType.IsArray)
            {
                var array = paramValue as object[];
                if(array.Length == 0)
                {
                    return string.Empty;
                }
                return string.Join("&", array.Select(x => BuildQueryStringParam(paramName, x, isArrayElement: true)));
            }

            // IEnumerable
            bool isImplementsIEnumerable = false;
            bool isElementsImplementsAreValueTypes = false;
            foreach (Type type in paramType.GetInterfaces())
            {
                if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    isImplementsIEnumerable = true;
                    var genericType = type.GetGenericArguments()[0];
                    isElementsImplementsAreValueTypes = genericType.IsValueType || paramType.IsEnum || genericType == typeof(string) ? true : false;
                }
            }
            if (isImplementsIEnumerable && isElementsImplementsAreValueTypes)
            {
                var ienumerable = paramValue as IEnumerable<object>;
                if (ienumerable.Count() == 0)
                {
                    return string.Empty;
                }
                return string.Join("&", ienumerable.Select(x => BuildQueryStringParam(paramName, x, isArrayElement: true)));
            }

            return string.Empty;
        }

        /// <summary>
        /// Builds query string for a params represented in a dictionary.
        /// <br/>
        /// Supported: value types, string, enum, array with prev types, IEnumerable with prev types (except array).
        /// </summary>
        public static string BuildQueryString(Dictionary<string, object> paramNameValues)
        {
            var parts = paramNameValues
                .Select(keyValuePair => BuildQueryStringParam(keyValuePair.Key, keyValuePair.Value))
                .Where(x => !String.IsNullOrEmpty(x))
                .ToList();
            return string.Join("&", parts);
        }

        /// <summary>
        /// Combines URI and query string
        /// </summary>
        public static string AddQueryString(string uri, string queryString)
        {
            string separator = "";
            if(!queryString.StartsWith("?") )
            {
                separator = "?";
            }
            return $"{uri}{separator}{queryString}";
        }
    }
}
