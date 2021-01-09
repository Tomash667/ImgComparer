using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace ImgComparer
{
    public class SortableList<T> : BindingList<T>
    {
        // reference to the list provided at the time of instantiation
        private List<T> originalList, filteredList;
        private ListSortDirection sortDirection;
        private PropertyDescriptor sortProperty;
        private bool filtered;
        private Func<List<T>, IEnumerable<T>> sortFunc;

        // a cache of functions that perform the sorting
        // for a given type, property, and sort direction
        private static Dictionary<string, Func<List<T>, IEnumerable<T>>> cachedOrderByExpressions =
            new Dictionary<string, Func<List<T>, IEnumerable<T>>>();

        protected override bool SupportsSortingCore => true;
        protected override ListSortDirection SortDirectionCore => sortDirection;
        protected override PropertyDescriptor SortPropertyCore => sortProperty;

        public SortableList(IEnumerable<T> enumerable)
        {
            originalList = enumerable.ToList();
            filteredList = originalList;
            ResetItems(filteredList);
        }

        /// <summary>
        /// Look for an appropriate sort method in the cache if not found .
        /// Call CreateOrderByMethod to create one.
        /// Apply it to the original list.
        /// Notify any bound controls that the sort has been applied.
        /// </summary>
        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            if (sortProperty == prop && sortDirection == direction)
                return;

            sortProperty = prop;
            sortDirection = direction;

            var orderByMethodName = direction ==
                ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";
            var cacheKey = typeof(T).GUID + prop.Name + orderByMethodName;

            if (!cachedOrderByExpressions.ContainsKey(cacheKey))
                CreateOrderByMethod(prop, orderByMethodName, cacheKey);

            sortFunc = cachedOrderByExpressions[cacheKey];
            ResetItems(sortFunc(filteredList));
            ResetBindings();
        }

        /// <summary>
        /// Create a generic method implementation for IEnumerable<T>.
        /// Cache it.
        /// </summary>
        private void CreateOrderByMethod(PropertyDescriptor prop, string orderByMethodName, string cacheKey)
        {
            Type t = typeof(T);
            var sourceParameter = Expression.Parameter(typeof(List<T>), "source");
            var lambdaParameter = Expression.Parameter(t, "lambdaParameter");
            var accesedMember = t.GetProperty(prop.Name);
            var attribute = (OrderByAttribute)accesedMember.GetCustomAttributes(typeof(OrderByAttribute), true).FirstOrDefault();
            Type propertyType = accesedMember.PropertyType;
            if (attribute != null)
            {
                accesedMember = t.GetProperty(attribute.field);
                propertyType = accesedMember.PropertyType;
            }
            var propertySelectorLambda =
                Expression.Lambda(Expression.MakeMemberAccess(lambdaParameter, accesedMember), lambdaParameter);
            var orderByMethod = typeof(Enumerable).GetMethods()
                                          .Where(a => a.Name == orderByMethodName && a.GetParameters().Length == 2)
                                          .Single()
                                          .MakeGenericMethod(t, propertyType);

            var orderByExpression = Expression.Lambda<Func<List<T>, IEnumerable<T>>>(
                                        Expression.Call(orderByMethod,
                                                new Expression[] { sourceParameter, propertySelectorLambda }),
                                                sourceParameter);

            cachedOrderByExpressions.Add(cacheKey, orderByExpression.Compile());
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
        }

        protected override void RemoveSortCore()
        {
            ResetItems(filteredList);
        }

        private void ResetItems(IEnumerable<T> items)
        {
            ClearItems();
            foreach (T item in items)
                Add(item);
        }

        private IEnumerable<T> ApplySort(List<T> items)
        {
            if (sortFunc != null)
                return sortFunc(items);
            else
                return items;
        }

        public void ApplyFilter(Func<T, bool> pred)
        {
            filtered = true;
            filteredList = new List<T>();
            foreach (T item in originalList)
            {
                if (pred(item))
                    filteredList.Add(item);
            }
            ResetItems(ApplySort(filteredList));
            base.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        public void ClearFilter()
        {
            if (filtered)
            {
                filtered = false;
                filteredList = originalList;
                ResetItems(ApplySort(filteredList));
                base.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }
        }
    }
}
