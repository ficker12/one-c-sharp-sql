﻿using Microsoft.Data.SqlClient;
using OneCSharp.Metadata.Model;
using System.Collections.Generic;
using System.Text;

namespace OneCSharp.Metadata.Services
{
    public sealed class SQLMetadataLoader
    {
        private sealed class FieldSqlInfo
        {
            public FieldSqlInfo() { }
            public int    ORDINAL_POSITION;
            public string COLUMN_NAME;
            public string DATA_TYPE;
            public int    CHARACTER_MAXIMUM_LENGTH;
            public byte   NUMERIC_PRECISION;
            public int    NUMERIC_SCALE;
            public bool   IS_NULLABLE;
        }
        private sealed class ClusteredIndexInfo
        {
            public ClusteredIndexInfo() { }
            public string NAME;
            public bool   IS_UNIQUE;
            public bool   IS_PRIMARY_KEY;
            public List<ClusteredIndexColumnInfo> COLUMNS = new List<ClusteredIndexColumnInfo>();
            public bool HasNullableColumns
            {
                get
                {
                    bool result = false;
                    foreach (ClusteredIndexColumnInfo item in COLUMNS)
                    {
                        if (item.IS_NULLABLE)
                        {
                            return true;
                        }
                    }
                    return result;
                }
            }
            public ClusteredIndexColumnInfo GetColumnByName(string name)
            {
                ClusteredIndexColumnInfo info = null;
                for (int i = 0; i < COLUMNS.Count; i++)
                {
                    if (COLUMNS[i].NAME == name) return COLUMNS[i];
                }
                return info;
            }
        }
        private sealed class ClusteredIndexColumnInfo
        {
            public ClusteredIndexColumnInfo() { }
            public byte   KEY_ORDINAL;
            public string NAME;
            public bool   IS_NULLABLE;
        }
        private string ConnectionString { get; set; }
        public void Load(string connectionString, InfoBase infoBase)
        {
            this.ConnectionString = connectionString;
            foreach (BaseObject bo in infoBase.BaseObjects)
            {
                GetSQLMetadata(bo);
            }
        }
        private void GetSQLMetadata(BaseObject bo)
        {
            foreach (MetaObject @object in bo.MetaObjects)
            {
                ReadSQLMetadata(@object);
                foreach (MetaObject nested in @object.MetaObjects)
                {
                    ReadSQLMetadata(nested);
                }
            }
        }
        private List<FieldSqlInfo> GetSqlFields(string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE,");
            sb.AppendLine(@"    ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,");
            sb.AppendLine(@"    ISNULL(NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,");
            sb.AppendLine(@"    ISNULL(NUMERIC_SCALE, 0) AS NUMERIC_SCALE,");
            sb.AppendLine(@"    CASE WHEN IS_NULLABLE = 'NO' THEN CAST(0x00 AS bit) ELSE CAST(0x01 AS bit) END AS IS_NULLABLE");
            sb.AppendLine(@"FROM");
            sb.AppendLine(@"    INFORMATION_SCHEMA.COLUMNS");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    TABLE_NAME = N'{0}'");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"    ORDINAL_POSITION ASC;");

            string sql = string.Format(sb.ToString(), tableName);

            List<FieldSqlInfo> list = new List<FieldSqlInfo>();
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            FieldSqlInfo item = new FieldSqlInfo()
                            {
                                ORDINAL_POSITION         = reader.GetInt32(0),
                                COLUMN_NAME              = reader.GetString(1),
                                DATA_TYPE                = reader.GetString(2),
                                CHARACTER_MAXIMUM_LENGTH = reader.GetInt32(3),
                                NUMERIC_PRECISION        = reader.GetByte(4),
                                NUMERIC_SCALE            = reader.GetInt32(5),
                                IS_NULLABLE              = reader.GetBoolean(6)
                            };
                            list.Add(item);
                        }
                    }
                }
            }
            return list;
        }
        private void ReadSQLMetadata(MetaObject @object)
        {
            List<FieldSqlInfo> sql_fields = GetSqlFields(@object.Table);

            ClusteredIndexInfo indexInfo = this.GetClusteredIndexInfo(@object.Table);

            foreach (FieldSqlInfo info in sql_fields)
            {
                bool found = false; Field field = null;
                foreach (Property p in @object.Properties)
                {
                    foreach (Field f in p.Fields)
                    {
                        if (f.Name == info.COLUMN_NAME)
                        {
                            field = f;
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    Property property = new Property()
                    {
                        Name    = info.COLUMN_NAME,
                        Purpose = PropertyPurpose.System
                    };
                    field = new Field()
                    {
                        Name     = info.COLUMN_NAME,
                        Purpose  = FieldPurpose.Value
                    };
                    property.Fields.Add(field);
                    @object.Properties.Add(property);
                }
                field.TypeName   = info.DATA_TYPE;
                field.Length     = info.CHARACTER_MAXIMUM_LENGTH;
                field.Precision  = info.NUMERIC_PRECISION;
                field.Scale      = info.NUMERIC_SCALE;
                field.IsNullable = info.IS_NULLABLE;

                if (indexInfo != null)
                {
                    ClusteredIndexColumnInfo columnInfo = indexInfo.GetColumnByName(info.COLUMN_NAME);
                    if (columnInfo != null)
                    {
                        field.IsPrimaryKey = true;
                        field.KeyOrdinal   = columnInfo.KEY_ORDINAL;
                    }
                }
            }
        }
        private ClusteredIndexInfo GetClusteredIndexInfo(string tableName)
        {
            ClusteredIndexInfo info = null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    i.name,");
            sb.AppendLine(@"    i.is_unique,");
            sb.AppendLine(@"    i.is_primary_key,");
            sb.AppendLine(@"    c.key_ordinal,");
            sb.AppendLine(@"    f.name,");
            sb.AppendLine(@"    f.is_nullable");
            sb.AppendLine(@"FROM sys.indexes AS i");
            sb.AppendLine(@"INNER JOIN sys.tables AS t ON t.object_id = i.object_id");
            sb.AppendLine(@"INNER JOIN sys.index_columns AS c ON c.object_id = t.object_id AND c.index_id = i.index_id");
            sb.AppendLine(@"INNER JOIN sys.columns AS f ON f.object_id = t.object_id AND f.column_id = c.column_id");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    t.object_id = OBJECT_ID(@table) AND i.type = 1 -- CLUSTERED");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"c.key_ordinal ASC;");
            string sql = sb.ToString();

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();

                command.Parameters.AddWithValue("table", tableName);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        info = new ClusteredIndexInfo()
                        {
                            NAME           = reader.GetString(0),
                            IS_UNIQUE      = reader.GetBoolean(1),
                            IS_PRIMARY_KEY = reader.GetBoolean(2)
                        };
                        info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                        {
                            KEY_ORDINAL = reader.GetByte(3),
                            NAME        = reader.GetString(4),
                            IS_NULLABLE = reader.GetBoolean(5)
                        });
                        while (reader.Read())
                        {
                            info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                            {
                                KEY_ORDINAL = reader.GetByte(3),
                                NAME        = reader.GetString(4),
                                IS_NULLABLE = reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }
            return info;
        }   
    }
}
//SqlConnectionStringBuilder helper = new SqlConnectionStringBuilder()
//{
//    DataSource = response.Server,
//    InitialCatalog = response.Database,
//    IntegratedSecurity = string.IsNullOrWhiteSpace(response.UserName)
//};
//if (!helper.IntegratedSecurity)
//{
//  helper.UserID = response.UserName;
//  helper.Password = response.Password;
//  helper.PersistSecurityInfo = false;
//}
//infoBase.Server = helper.DataSource;
//infoBase.Database = helper.InitialCatalog;
//infoBase.UserName = helper.UserID;
//infoBase.Password = helper.Password;
//(new SQLMetadataAdapter()).Load(helper.ToString(), infoBase);