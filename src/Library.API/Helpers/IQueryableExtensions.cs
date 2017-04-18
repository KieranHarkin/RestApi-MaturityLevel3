using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using AutoMapper.Configuration;
using Library.API.Services;

namespace Library.API.Helpers
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string orderBy,
            Dictionary<string, PropertyMappingValue> mappingDictionary)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (mappingDictionary == null)
            {
                throw new ArgumentNullException(nameof(mappingDictionary));
            }

            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return source;
            }

            // the orderBy string is seperated by "," so we will split it.
            var orderByAfterSplit = orderBy.Split(',');

            // apply each orderby clause in reverse order
            // otherwise the IQueryable will be ordered the wrong way
            foreach (var orderByClause in orderByAfterSplit.Reverse())
            {
                var trimmedOrderByClause = orderByClause.Trim();

                // if the sort option ends with " desc", we order descending otherwise ascending
                var orderDescending = trimmedOrderByClause.EndsWith(" desc");

                // remove "asc" or "desc" from the orderByClause, so we 
                // get the property name to look for in the mapping dictionary
                var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" ");
                var propertyName = indexOfFirstSpace == -1
                    ? trimmedOrderByClause
                    : trimmedOrderByClause.Remove(indexOfFirstSpace);

                // find matching property
                if (!mappingDictionary.ContainsKey(propertyName))
                {
                    throw new ArgumentException($"Key mapping for {propertyName} is missing.");
                }

                // get the PropertyMappingValue
                var propertyMappingValue = mappingDictionary[propertyName];

                if (propertyMappingValue == null)
                {
                    throw new ArgumentNullException(nameof(propertyMappingValue));
                }

                // Run through the property names in reverse
                // so the orderby clauses are applied in the correct order
                foreach (var destinationProperty in propertyMappingValue.DestinationProperties.Reverse())
                {
                    if (propertyMappingValue.Revert)
                    {
                        orderDescending = !orderDescending;
                    }

                    source = source.OrderBy(destinationProperty + (orderDescending ? " descending" : " ascending"));
                }

            }

            return source;
        }
    }
}
