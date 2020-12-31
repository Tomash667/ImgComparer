﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace ImgComparer
{
    public class SortableList<T> : BindingList<T>
    {
        // reference to the list provided at the time of instantiation
        List<T> originalList;
        ListSortDirection sortDirection;
        PropertyDescriptor sortProperty;

        // a cache of functions that perform the sorting
        // for a given type, property, and sort direction
        static Dictionary<string, Func<List<T>, IEnumerable<T>>>
           cachedOrderByExpressions = new Dictionary<string, Func<List<T>,
                                                     IEnumerable<T>>>();

        public SortableList()
        {
            originalList = new List<T>();
        }

        public SortableList(IEnumerable<T> enumerable)
        {
            originalList = enumerable.ToList();
            ResetItems(originalList);
        }

        public SortableList(List<T> list)
        {
            originalList = list;
            ResetItems(originalList);
        }

        protected override void ApplySortCore(PropertyDescriptor prop,
                                ListSortDirection direction)
        {
            /*
             Look for an appropriate sort method in the cache if not found .
             Call CreateOrderByMethod to create one. 
             Apply it to the original list.
             Notify any bound controls that the sort has been applied.
             */

            sortProperty = prop;
            sortDirection = direction;

            var orderByMethodName = direction ==
                ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";
            var cacheKey = typeof(T).GUID + prop.Name + orderByMethodName;

            if (!cachedOrderByExpressions.ContainsKey(cacheKey))
            {
                CreateOrderByMethod(prop, orderByMethodName, cacheKey);
            }

            ResetItems(cachedOrderByExpressions[cacheKey](originalList).ToList());
            ResetBindings();
        }

        private void CreateOrderByMethod(PropertyDescriptor prop, string orderByMethodName, string cacheKey)
        {
            /*
             Create a generic method implementation for IEnumerable<T>.
             Cache it.
            */

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

        protected override void RemoveSortCore()
        {
            ResetItems(originalList);
        }

        private void ResetItems(List<T> items)
        {
            base.ClearItems();

            for (int i = 0; i < items.Count; i++)
            {
                base.InsertItem(i, items[i]);
            }
        }

        protected override bool SupportsSortingCore
        {
            get
            {
                // indeed we do
                return true;
            }
        }

        protected override ListSortDirection SortDirectionCore
        {
            get
            {
                return sortDirection;
            }
        }

        protected override PropertyDescriptor SortPropertyCore
        {
            get
            {
                return sortProperty;
            }
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            originalList = base.Items.ToList();
        }
    }
}
