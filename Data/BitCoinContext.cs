using Nop.Core;
using Nop.Data;
using Nop.Data.Mapping.Orders;
using Nop.Plugin.Payments.BitCoin.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.BitCoin.Data
{
    public class BitCoinContext : DbContext, IDbContext
    {
        public bool ProxyCreationEnabled
        {
            get
            {
                return Configuration.ProxyCreationEnabled;
            }
            set
            {
                Configuration.ProxyCreationEnabled = value;
            }
        }
        public bool AutoDetectChangesEnabled
        {
            get
            {
                return Configuration.AutoDetectChangesEnabled;
            }
            set
            {
                Configuration.AutoDetectChangesEnabled = value;
            }
        }

        public BitCoinContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            ((IObjectContextAdapter) this).ObjectContext.ContextOptions.LazyLoadingEnabled = true;
        }

        public void Detach(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            ((IObjectContextAdapter)this).ObjectContext.Detach(entity);
        }

        public int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) where TEntity : BaseEntity, new()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        IDbSet<TEntity> IDbContext.Set<TEntity>()
        {
            return base.Set<TEntity>();
        }

        public void Install()
        {
            //create the table
            var dbScript = CreateDatabaseScript();
            Database.ExecuteSqlCommand(dbScript);
            SaveChanges();
        }

        public void Uninstall()
        {
            this.DropPluginTable("BitCoinAddresses");
        }

        public string CreateDatabaseScript()
        {
            return "BEGIN TRANSACTION"
                    + " CREATE TABLE dbo.BitCoinAddresses"
                    + "     ("
                    + "     Id int IDENTITY(1,1) NOT NULL,"
                    + "     PublicKey nvarchar(500) NOT NULL,"
                    + "     PrivateKey nvarchar(500) NOT NULL,"
                    + "     OrderId int NULL,"
                    + "     [Price] [decimal](18, 18) NOT NULL,"
                    + "     [Time] [datetime] NOT NULL,"
                    + " 	)  ON[PRIMARY]"
                    + " ALTER TABLE dbo.BitCoinAddresses ADD CONSTRAINT"
                    + "     FK_BitCoinAddresses_Order FOREIGN KEY"
                    + "     ("
                    + "     OrderId"
                    + " "
                    + "     ) REFERENCES dbo.[Order]"
                    + "     ("
                    + "     Id"
                    + " "
                    + "     ) ON UPDATE NO ACTION"
                    + "      ON DELETE NO ACTION"
                    + " COMMIT";
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new Nop.Data.Mapping.Customers.RewardPointsHistoryMap());
            modelBuilder.Configurations.Add(new OrderMap());
            modelBuilder.Configurations.Add(new BitCoinAddressMap());
            base.OnModelCreating(modelBuilder);
        }
    }
}
