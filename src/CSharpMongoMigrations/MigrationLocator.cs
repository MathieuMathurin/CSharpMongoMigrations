﻿using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CSharpMongoMigrations
{
    public interface IMigrationLocator
    {
        IEnumerable<VersionedMigration> GetMigrations(MigrationVersion after, MigrationVersion before);
    }

    public class MigrationLocator : IMigrationLocator
    {
        private readonly Assembly _assembly;
        private readonly IMongoDatabase _database;

        public MigrationLocator(string assemblyName, IMongoDatabase database)
        {
            _assembly = Assembly.LoadFrom(assemblyName);
            _database = database;
        }

        public IEnumerable<VersionedMigration> GetMigrations(MigrationVersion after, MigrationVersion before)
        {
            var migrations =
                (
                    from type in _assembly.GetTypes()
                    where typeof(IMigration).IsAssignableFrom(type) && !type.IsAbstract
                    let attribute = type.GetCustomAttribute<MigrationAttribute>()
                    where attribute != null && after.Version < attribute.Version && attribute.Version <= before.Version
                    orderby attribute.Version
                    select new { Migration = (IMigration)Activator.CreateInstance(type), Version = new MigrationVersion(attribute.Version, attribute.Description) }
                ).ToList();

            foreach (var m in migrations)
                ((IDbMigration)m.Migration).SetDatabase(_database);

            return migrations.Select(x => new VersionedMigration(x.Migration, x.Version));                
        }
    }
}
