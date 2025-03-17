using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Data.Sqlite;

namespace BlazorApp3.Data
{
    public class SqliteModelCustomizer : RelationalModelCustomizer
    {
        public SqliteModelCustomizer(ModelCustomizerDependencies dependencies)
            : base(dependencies)
        {
        }

        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);

            // Fix for SQLite not supporting max length
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetMaxLength() == -1)
                    {
                        property.SetMaxLength(null);
                    }
                }
            }
        }
    }
}