﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace MySoft.Data
{
    /// <summary>
    /// 数组列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class ArrayList<T> : List<T>, IArrayList<T>, IDisposable
    {
        public ArrayList() { }

        /// <summary>
        /// 实例化ArrayList
        /// </summary>
        /// <param name="list"></param>
        public ArrayList(IList<T> list)
        {
            if (list != null) this.AddRange(list);
        }

        /// <summary>
        /// 获取当前索引的对象
        /// </summary>
        /// <returns></returns>
        public new T this[int index]
        {
            get
            {
                if (base.Count == 0)
                    return default(T);

                if (base.Count > index)
                    return base[index];

                return default(T);
            }
            set
            {
                if (base.Count > index)
                    base[index] = value;
            }
        }

        /// <summary>
        /// 获取当前DataSource
        /// </summary>
        /// <returns></returns>
        public IList<T> OriginalData
        {
            get
            {
                return this.ConvertAll<T>(p => p);
            }
        }

        #region 字典操作

        /// <summary>
        /// 返回字典
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="groupField"></param>
        /// <returns></returns>
        public IDictionary<TResult, IList<T>> ToDictionary<TResult>(Field groupField)
        {
            return ToDictionary<TResult>(groupField.PropertyName);
        }

        /// <summary>
        /// 返回字典
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public IDictionary<TResult, IList<T>> ToDictionary<TResult>(string propertyName)
        {
            IDictionary<TResult, IList<T>> group = new Dictionary<TResult, IList<T>>();
            if (this.Count == 0) return group;

            lock (group)
            {
                foreach (T t in this)
                {
                    object obj = CoreHelper.GetPropertyValue(t, propertyName);
                    TResult value = CoreHelper.ConvertValue<TResult>(obj);
                    if (!group.ContainsKey(value))
                    {
                        group[value] = new SourceList<T>();
                    }
                    group[value].Add(t);
                }

                return group;
            }
        }

        #endregion

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            this.Clear();
        }
    }

    /// <summary>
    /// 数组列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class SourceList<T> : ArrayList<T>, ISourceList<T>
    {
        public SourceList() { }

        /// <summary>
        /// 实例化SourceList
        /// </summary>
        /// <param name="list"></param>
        public SourceList(IList<T> list)
            : base(list)
        { }

        #region IArrayList<T> 成员

        /// <summary>
        /// 转换成SourceTable
        /// </summary>
        /// <returns></returns>
        public SourceTable ToTable()
        {
            DataTable dt = this.GetDataTable(typeof(T));
            return new SourceTable(dt);
        }

        /// <summary>
        /// 返回另一类型的列表(用于实体的解耦))
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <returns></returns>
        public SourceList<TOutput> ConvertTo<TOutput>()
        {
            return this.ConvertAll<TOutput>(p => DataHelper.ConvertType<T, TOutput>(p));
        }

        /// <summary>
        /// 查找符合条件的对象
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public new SourceList<T> FindAll(Predicate<T> match)
        {
            IList<T> list = base.FindAll(match);
            return new SourceList<T>(list);
        }

        /// <summary>
        /// 对当前列表进行排序
        /// </summary>
        /// <param name="sort"></param>
        /// <returns></returns>
        public SourceList<T> Sort(SortComparer<T> sort)
        {
            var list = new List<T>(this);
            list.Sort(sort);
            return new SourceList<T>(list);
        }

        /// <summary>
        /// 对当前列表进行排序
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public SourceList<T> Sort(int index, int count, SortComparer<T> sort)
        {
            var list = new List<T>(this);
            list.Sort(index, count, sort);
            return new SourceList<T>(list);
        }

        /// <summary>
        /// 返回指定数据条数
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public new SourceList<T> GetRange(int index, int size)
        {
            if (index < base.Count)
            {
                IList<T> list = null;
                if (index + size <= base.Count)
                    list = base.GetRange(index, size);
                else
                    list = base.GetRange(index, base.Count - index);

                return new SourceList<T>(list);
            }

            return new SourceList<T>();
        }

        /// <summary>
        /// 将当前类型转成另一种类型
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="converter"></param>
        /// <returns></returns>
        public new SourceList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            IList<TOutput> list = base.ConvertAll<TOutput>(converter);
            return new SourceList<TOutput>(list);
        }

        /// <summary>
        /// 返回另一类型的列表(输入为类、输出为接口，用于实体的解耦)
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <typeparam name="IOutput"></typeparam>
        /// <returns></returns>
        public SourceList<IOutput> ConvertTo<TOutput, IOutput>()
            where TOutput : IOutput
        {
            if (!typeof(TOutput).IsClass)
            {
                throw new DataException("TOutput必须是Class类型！");
            }

            if (!typeof(IOutput).IsInterface)
            {
                throw new DataException("IOutput必须是Interface类型！");
            }

            //进行两次转换后返回
            return ConvertTo<TOutput>().ConvertTo<IOutput>();
        }

        /// <summary>
        ///  转换成DataTable
        /// </summary>
        /// <returns></returns>
        internal DataTable GetDataTable(Type currType)
        {
            #region 对list进行转换

            DataTable dt = new DataTable();
            dt.TableName = currType.Name;

            PropertyInfo[] plist = CoreHelper.GetPropertiesFromType(currType);
            foreach (PropertyInfo p in plist)
            {
                Type propertyType = p.PropertyType;

                //nullables must use underlying types
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                //enums also need special treatment
                if (propertyType.IsEnum)
                    propertyType = Enum.GetUnderlyingType(propertyType); //probably Int32

                dt.Columns.Add(p.Name, propertyType);
            }

            //当无数据时直接返回表结构体
            if (this.Count == 0) return dt;

            foreach (T t in this)
            {
                DataRow dtRow = dt.NewRow();
                foreach (PropertyInfo p in plist)
                {
                    object value = CoreHelper.GetPropertyValue(t, p.Name);
                    dtRow[p.Name] = value == null ? DBNull.Value : value;
                }
                dt.Rows.Add(dtRow);
            }

            #endregion

            return dt;
        }

        #endregion
    }
}
