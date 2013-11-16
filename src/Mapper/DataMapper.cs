﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Sparrow.CommonLibrary.Mapper.Metadata;
using Sparrow.CommonLibrary.Entity;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Sparrow.CommonLibrary.Mapper
{

    /// <summary>
    /// 内部数据映射的实现。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataMapper<T> : IMapper<T>
    {
        private readonly IMetaInfo _metaInfo;

        /// <summary>
        /// 初始化
        /// </summary>
        public DataMapper()
            : this(new MetaInfo(null, typeof(T)))
        {
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public DataMapper(string name)
            : this(new MetaInfo(name, typeof(T)))
        {
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="metaInfo">元数据</param>
        public DataMapper(IMetaInfo metaInfo)
        {
            Type type = typeof(T);
            if (type.IsClass == false)
                throw new MapperException(string.Format("类型[{0}]不是[{1}]有效识别的实体类型。", type.FullName, this.GetType().FullName));

            if (metaInfo == null)
                throw new ArgumentNullException("metaInfo");

            if (metaInfo.EntityType != typeof(T))
                throw new MapperException("metaInfo元数据中的EntityType与泛型T不一致。");

            _metaInfo = metaInfo;
        }

        #region IMapper<T>

        private IPropertyAccessor<T>[] _pAccessors;
        private Func<T> _creator;

        IPropertyAccessor IMapper.this[string propertyName]
        {
            get
            {
                int i = MetaInfo.IndexOf(propertyName);
                return i > -1 ? _pAccessors[i] : null;
            }
        }

        IPropertyAccessor IMapper.this[int index]
        {
            get
            {
                if (index < 0 || _pAccessors.Length <= index)
                    return null;
                return _pAccessors[index];
            }
        }

        public IPropertyAccessor<T> this[string propertyName]
        {
            get
            {
                int i = MetaInfo.IndexOf(propertyName);
                return i > -1 ? _pAccessors[i] : null;
            }
        }

        public IPropertyAccessor<T> this[int index]
        {
            get
            {
                if (index < 0 || _pAccessors.Length <= index)
                    return null;
                return _pAccessors[index];
            }
        }

        public IMetaInfo MetaInfo
        {
            get { return _metaInfo; }
        }

        object IMapper.Create()
        {
            return Create();
        }

        public T Create()
        {
            return _creator();
        }

        public T MapSingle(object dataSource)
        {
            return DataSourceAdapter.Instance.ReadSingle(this, dataSource);
        }

        public List<T> MapList(object dataSource)
        {
            return DataSourceAdapter.Instance.ReadList(this, dataSource);
        }

        object IMapper.MapSingle(object dataSource)
        {
            return DataSourceAdapter.Instance.ReadSingle(this, dataSource);
        }

        System.Collections.IList IMapper.MapList(object dataSource)
        {
            return DataSourceAdapter.Instance.ReadList(this, dataSource);
        }

        #endregion

        #region DataMapper

        /// <summary>
        /// 添加需要映射的属性成员。
        /// </summary>
        /// <param name="propertyExp"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public DataMapper<T> AppendProperty(Expression<Func<T, object>> propertyExp, string propertyName)
        {
            if (propertyExp == null)
                throw new ArgumentNullException("propertyExp");
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException("propertyName");

            var propertyInfo = PropertyExpression.ExtractMemberExpression(propertyExp);
            MetaInfo.AddPropertyInfo(new MetaPropertyInfo(MetaInfo,propertyName,(PropertyInfo)propertyInfo.Member));
            return this;
        }

        /// <summary>
        /// 添加需要映射的属性成员。
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public DataMapper<T> AppendProperty(IMetaPropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");

            MetaInfo.AddPropertyInfo(propertyInfo);
            return this;
        }

        private bool isReorganize;

        private void Reorganize()
        {
            if (isReorganize)
                throw new MapperException("不能重复调用方法：Complete()。");

            if (MetaInfo.PropertyCount == 0)
                throw new MapperException("未设映射任何一个属性成员。");

            lock (this)
            {
                if (isReorganize)
                    throw new MapperException("不能重复调用方法：Complete()。");

                MetaInfo.MakeReadonly();

                Func<PropertyInfo, PropertyAccessor<T>> creater;
                if (null == (creater = _propertyAccessoCreater))
                {
                    creater = (x) => new PropertyAccessor<T>(x);
                }

                _pAccessors = new IPropertyAccessor<T>[MetaInfo.PropertyCount];
                for (var i = MetaInfo.PropertyCount - 1; i > -1; i--)
                {
                    _pAccessors[i] = creater(MetaInfo[i].PropertyInfo);
                }

                isReorganize = true;
            }
        }

        /// <summary>
        /// 完成映射（生命周期内只能调用一次）。
        /// </summary>
        /// <returns></returns>
        public DataMapper<T> Complete()
        {
            Reorganize();

            var ctor = typeof(T).GetConstructor(new Type[0]);
            if (ctor == null)
                throw new MapperException(string.Format("类型[{0}]无默认构造函数。", typeof(T).FullName));

            _creator = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();

            return this;
        }

        /// <summary>
        /// 完成映射（生命周期内只能调用一次）。
        /// </summary>
        /// <param name="builder">自定义类型<typeparamref name="T"/>的实例化。</param>
        /// <returns></returns>
        public DataMapper<T> Complete(Func<DataMapper<T>, Func<T>> builder)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");

            Reorganize();

            _creator = builder(this);

            return this;
        }

        #endregion

        private static Func<PropertyInfo, PropertyAccessor<T>> _propertyAccessoCreater;
        public static void ResetPropertyAccessorCreater(Func<PropertyInfo, PropertyAccessor<T>> creater)
        {
            if (_propertyAccessoCreater == null)
            {
                _propertyAccessoCreater = creater;
            }
            else
            {
                lock (_propertyAccessoCreater)
                {
                    _propertyAccessoCreater = creater;
                }
            }
        }

    }

}
