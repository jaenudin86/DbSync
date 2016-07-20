﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbSync.Core.Services;
using System.Data.SqlClient;
using System.IO;
using DbSync.Core.DataWriter;

namespace DbSync.Core.Transfers
{
    public class SmartTransfer : Transfer
    {
        public static SmartTransfer Instance { get; } = new SmartTransfer();
        private SmartTransfer() { }
        public override void Run(JobSettings settings, string environment, IErrorHandler errorHandler)
        {
            Run(settings, errorHandler, false);
        }
        public void Run(JobSettings settings, IErrorHandler errorHandler, bool export)
        {
            using (var connection = new SqlConnection(settings.ConnectionString))
            {
                connection.Open();

                foreach (var table in settings.Tables)
                    using (var cmd = connection.CreateCommand())
                    {
                        table.Initialize(connection, settings, errorHandler);
                        cmd.CommandText = $"SELECT * FROM {table.QualifiedName}";
                        var diffGenerator = new DiffGenerator();
                        using (var target = cmd.ExecuteReader())
                        using (var source = new XmlRecordDataReader(Path.Combine(settings.Path, table.Name+".xml"), table))
                        using (var writer = export ? new XmlDataWriter(table, settings) as IDataWriter : new SqlSimpleDataWriter(settings.ConnectionString, table, settings))
                        {
                            diffGenerator.GenerateDifference(source, target, table, writer);
                        }
                    }
            }
        }
    }
}
