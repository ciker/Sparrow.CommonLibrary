﻿using System;
using System.Linq;
using System.Text;
using Sparrow.CommonLibrary.Entity;
using Sparrow.CommonLibrary.Database;
using Sparrow.CommonLibrary.Mapper;
using Sparrow.CommonLibrary.Mapper.Metadata;
using Sparrow.CommonLibrary.Utility;

namespace Sparrow.CommonLibrary.Database.SqlBuilder
{
    /// <summary>
    /// 实体对象转换成Sql语句
    /// </summary>
    public class EntityToSqlStatement
    {
        private readonly ISqlBuilder _stmBuilder;
        /// <summary>
        /// 
        /// </summary>
        public ISqlBuilder StmBuilder
        {
            get { return _stmBuilder; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stmBuilder">sql语句生成器</param>
        protected EntityToSqlStatement(ISqlBuilder stmBuilder)
        {
            _stmBuilder = stmBuilder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public virtual string GenerateInsert(IEntityExplain entity, ParameterCollection output)
        {
            bool hasIncrement;
            return GenerateInsert(entity, output, false, out hasIncrement);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="output"></param>
        /// <param name="includeIncrement"></param>
        /// <param name="incrementFieldName"> </param>
        /// <returns></returns>
        public virtual string GenerateInsert(IEntityExplain entity, ParameterCollection output, bool includeIncrement, out bool hasIncrement, string incrementFieldName = null)
        {
            var fields = entity.GetSettedFields();
            var values = entity.GetValues(fields);
            var metaTable = entity as IMetaInfoForDbTable;
            if (includeIncrement && metaTable != null && metaTable.Identity != null)
            {
                var sql = new StringBuilder();
                sql.AppendLine(_stmBuilder.Insert(entity, values, output, SqlOptions.None));
                sql.AppendLine(_stmBuilder.IncrementByQuery(metaTable, incrementFieldName, SqlOptions.None));
                hasIncrement = true;
                return sql.ToString();
            }
            //
            hasIncrement = false;
            return _stmBuilder.Insert(entity, values, output, SqlOptions.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public virtual string GenerateUpdate(IEntityExplain entity, ParameterCollection output)
        {
            var fields = entity.GetSettedFields();
            if (fields == null || fields.Any() == false)
                throw new ArgumentException("实体对象缺少主键值。");
            var conditions = entity.GetValues(entity.GetKeys());
            var values = entity.GetValues(fields.Where(x => entity.IsKey(x) == false));
            return _stmBuilder.Update(entity, values, conditions, output, SqlOptions.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public virtual string GenerateInsertOrUpdate(IEntityExplain entity, ParameterCollection output)
        {
            bool hasIncrement;
            return GenerateInsertOrUpdate(entity, output, false, out hasIncrement);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="output"></param>
        /// <param name="includeIncrement"></param>
        /// <param name="incrementFieldName"> </param>
        /// <returns></returns>
        public virtual string GenerateInsertOrUpdate(IEntityExplain entity, ParameterCollection output, bool includeIncrement, out bool hasIncrement, string incrementFieldName = null)
        {
            // 有增量标识的实体对象的特殊处理
            var metaDb = entity as IMetaInfoForDbTable;
            if (metaDb != null && metaDb.Identity != null)
            {
                // 实体对象如果包含主键信息，则只生成update语句,否则只生成insert语句。
                var val = entity[metaDb.Identity.Name];
                if (val != null && val != DBNull.Value && DbValueCast.Cast<int>(val) >= metaDb.Identity.StartVal)
                {
                    hasIncrement = false;
                    return GenerateUpdate(entity, output);
                }
                else
                {
                    return GenerateInsert(entity, output, includeIncrement, out hasIncrement, incrementFieldName);
                }
            }
            //
            var keys = entity.GetKeys();
            if (keys == null || keys.Length == 0)
                throw new ArgumentException("实体对象缺少主键值。");
            var conditions = entity.GetValues(keys);
            if (conditions == null || conditions.Any() == false)
                throw new ArgumentException("实体对象缺少主键值。");
            var ifConditionSql = _stmBuilder.Query(metaDb, new[] { keys[0] }, conditions, output, SqlOptions.None);
            var ifTrueSql = GenerateUpdate(entity, output);
            var ifFalseSql = GenerateInsert(entity, output, includeIncrement, out hasIncrement, incrementFieldName);
            return _stmBuilder.IfExistsFormat(ifConditionSql, ifTrueSql, ifFalseSql, SqlOptions.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public virtual string GenerateDelete(IEntityExplain entity, ParameterCollection output)
        {
            var conditions = entity.GetValues(entity.GetKeys());
            if (conditions == null || conditions.Any() == false)
                throw new ArgumentException("该实体对象不包含主键信息，无法执行删除命令。");
            return _stmBuilder.Delete(entity, conditions, output, SqlOptions.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="output"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public virtual string GenerateDelete<T>(object id, ParameterCollection output)
        {
            if (id == null)
                throw new ArgumentException("参数id不能为空。");
            var mapper = Map.GetIMapper<T>();
            var keys = mapper.MetaInfo.GetKeys();
            if (keys.Length != 1)
            {
                throw new ArgumentException(keys.Length > 0 ? "主键不能为复合主键。" : "该对象没有设置主键，无法执行删除命令。");
            }
            return _stmBuilder.Delete(mapper.MetaInfo, new[] { new ItemValue(keys[0], id), }, output, SqlOptions.None);
        }


        private static Func<ISqlBuilder, EntityToSqlStatement> _creater;

        /// <summary>
        /// 重置<paramref name="EntityToSqlStatement"/>构造器
        /// </summary>
        /// <param name="creater"></param>
        public static void ResetCreater(Func<ISqlBuilder, EntityToSqlStatement> creater)
        {
            _creater = creater;
        }
        /// <summary>
        /// 创建一个类型<paramref name="EntityToSqlStatement"/>的实力
        /// </summary>
        /// <param name="stmBuilder"></param>
        /// <returns></returns>
        public static EntityToSqlStatement Create(ISqlBuilder stmBuilder)
        {
            if (_creater != null)
                return _creater(stmBuilder);
            return new EntityToSqlStatement(stmBuilder);
        }

    }

}