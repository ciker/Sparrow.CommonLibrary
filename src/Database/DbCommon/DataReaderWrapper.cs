﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Sparrow.CommonLibrary.Database.DbCommon
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DataReaderWrapper : MarshalByRefObject, IDataReader
    {
        public static readonly IDataReader Empty = new EmptyDataReaderWrapper();

        private readonly ConnectionWrapper connnection;
        private readonly IDataReader datareader;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connnection"></param>
        /// <param name="datareader"></param>
        public DataReaderWrapper(ConnectionWrapper connnection, IDataReader datareader)
        {
            this.connnection = connnection;
            this.datareader = datareader;
            this.connnection.AddReference();
        }

        #region IDispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!datareader.IsClosed)
                {
                    datareader.Close();
                    this.connnection.Dispose();
                }
            }
        }

        #endregion

        #region IDataReader

        public void Close()
        {
            if (!datareader.IsClosed)
            {
                datareader.Close();
                this.connnection.Dispose();
            }
        }

        public int Depth
        {
            get { return datareader.Depth; }
        }

        public DataTable GetSchemaTable()
        {
            return datareader.GetSchemaTable();
        }

        public bool IsClosed
        {
            get { return datareader.IsClosed; }
        }

        public bool NextResult()
        {
            return datareader.NextResult();
        }

        public bool Read()
        {
            return datareader.Read();
        }

        public int RecordsAffected
        {
            get { return datareader.RecordsAffected; }
        }

        public int FieldCount
        {
            get { return datareader.FieldCount; }
        }

        public bool GetBoolean(int i)
        {
            return datareader.GetBoolean(i);
        }

        public byte GetByte(int i)
        {
            return datareader.GetByte(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return datareader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        public char GetChar(int i)
        {
            return datareader.GetChar(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return datareader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        public IDataReader GetData(int i)
        {
            return datareader.GetData(i);
        }

        public string GetDataTypeName(int i)
        {
            return datareader.GetDataTypeName(i);
        }

        public DateTime GetDateTime(int i)
        {
            return datareader.GetDateTime(i);
        }

        public decimal GetDecimal(int i)
        {
            return datareader.GetDecimal(i);
        }

        public double GetDouble(int i)
        {
            return datareader.GetDouble(i);
        }

        public Type GetFieldType(int i)
        {
            return datareader.GetFieldType(i);
        }

        public float GetFloat(int i)
        {
            return datareader.GetFloat(i);
        }

        public Guid GetGuid(int i)
        {
            return datareader.GetGuid(i);
        }

        public short GetInt16(int i)
        {
            return datareader.GetInt16(i);
        }

        public int GetInt32(int i)
        {
            return datareader.GetInt32(i);
        }

        public long GetInt64(int i)
        {
            return datareader.GetInt64(i);
        }

        public string GetName(int i)
        {
            return datareader.GetName(i);
        }

        public int GetOrdinal(string name)
        {
            return datareader.GetOrdinal(name);
        }

        public string GetString(int i)
        {
            return datareader.GetString(i);
        }

        public object GetValue(int i)
        {
            return datareader.GetValue(i);
        }

        public int GetValues(object[] values)
        {
            return datareader.GetValues(values);
        }

        public bool IsDBNull(int i)
        {
            return datareader.IsDBNull(i);
        }

        public object this[string name]
        {
            get { return datareader[name]; }
        }

        public object this[int i]
        {
            get { return datareader[i]; }
        }

        #endregion

        private class EmptyDataReaderWrapper : MarshalByRefObject, IDataReader
        {
            #region IDispose

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public void Dispose(bool disposing)
            {
                if (disposing)
                {

                }
            }

            #endregion

            #region IDataReader

            public void Close()
            {
            }

            public int Depth
            {
                get { return 0; }
            }

            public DataTable GetSchemaTable()
            {
                return null;
            }

            public bool IsClosed
            {
                get { return true; }
            }

            public bool NextResult()
            {
                return false;
            }

            public bool Read()
            {
                return false;
            }

            public int RecordsAffected
            {
                get { return 0; }
            }

            public int FieldCount
            {
                get { return 0; }
            }

            public bool GetBoolean(int i)
            {
                throw new NotImplementedException();
            }

            public byte GetByte(int i)
            {
                throw new NotImplementedException();
            }

            public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException();
            }

            public char GetChar(int i)
            {
                throw new NotImplementedException();
            }

            public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException();
            }

            public IDataReader GetData(int i)
            {
                throw new NotImplementedException();
            }

            public string GetDataTypeName(int i)
            {
                throw new NotImplementedException();
            }

            public DateTime GetDateTime(int i)
            {
                throw new NotImplementedException();
            }

            public decimal GetDecimal(int i)
            {
                throw new NotImplementedException();
            }

            public double GetDouble(int i)
            {
                throw new NotImplementedException();
            }

            public Type GetFieldType(int i)
            {
                throw new NotImplementedException();
            }

            public float GetFloat(int i)
            {
                throw new NotImplementedException();
            }

            public Guid GetGuid(int i)
            {
                throw new NotImplementedException();
            }

            public short GetInt16(int i)
            {
                throw new NotImplementedException();
            }

            public int GetInt32(int i)
            {
                throw new NotImplementedException();
            }

            public long GetInt64(int i)
            {
                throw new NotImplementedException();
            }

            public string GetName(int i)
            {
                throw new NotImplementedException();
            }

            public int GetOrdinal(string name)
            {
                throw new NotImplementedException();
            }

            public string GetString(int i)
            {
                throw new NotImplementedException();
            }

            public object GetValue(int i)
            {
                throw new NotImplementedException();
            }

            public int GetValues(object[] values)
            {
                throw new NotImplementedException();
            }

            public bool IsDBNull(int i)
            {
                throw new NotImplementedException();
            }

            public object this[string name]
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public object this[int i]
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            #endregion

        }
    }
}
