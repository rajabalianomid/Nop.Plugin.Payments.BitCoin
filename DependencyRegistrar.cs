using Autofac;
using Autofac.Core;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Plugin.Payments.BitCoin.Data;
using Nop.Plugin.Payments.BitCoin.Domain;
using Nop.Plugin.Payments.BitCoin.Repository;
using Nop.Plugin.Payments.BitCoin.Service;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.BitCoin
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<BitCoinService>().As<IBitCoinService>().InstancePerLifetimeScope();

            //data context
            this.RegisterPluginDataContext<BitCoinContext>(builder, "nop_object_context_bit_coin");

            //override required repository with our custom context
            builder.RegisterType<EfRepository<BitCoinAddresses>>().As<IRepository<BitCoinAddresses>>().WithParameter(ResolvedParameter.ForNamed<IDbContext>("nop_object_context_bit_coin")).InstancePerLifetimeScope();
            builder.RegisterType<BitCoinAddressRepository>().As<IBitCoinAddressRepository>().InstancePerLifetimeScope();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 1; }
        }
    }
}
