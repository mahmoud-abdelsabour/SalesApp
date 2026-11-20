using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication2.Services.Common
{
    /// <summary>
    /// One page of a query, with the paging numbers the views need.
    /// Skip/Take run in the database.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; private set; }
        public int Page { get; private set; }
        public int PageSize { get; private set; }
        public int TotalRecords { get; private set; }
        public int TotalPages { get; private set; }

        /// <param name="ordered">An ordered query (Skip needs an OrderBy).</param>
        /// <param name="clampToLastPage">
        /// When true, a page past the end shows the last page instead of an empty one.
        /// </param>
        public static PagedResult<T> Create(IQueryable<T> ordered, int page, int pageSize, bool clampToLastPage = false)
        {
            int total = ordered.Count();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);

            int current = Math.Max(1, page);
            if (clampToLastPage)
                current = Math.Min(current, Math.Max(1, totalPages));

            return new PagedResult<T>
            {
                Items = ordered.Skip((current - 1) * pageSize).Take(pageSize).ToList(),
                Page = current,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = totalPages
            };
        }
    }
}
