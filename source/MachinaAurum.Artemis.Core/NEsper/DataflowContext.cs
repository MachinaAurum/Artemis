using Microsoft.Practices.Unity;

namespace MachinaAurum.Artemis.NEsper
{
    public class DataflowContext
    {
        IUnityContainer Unity;

        public DataflowContext(IUnityContainer unity)
        {
            Unity = unity;
        }

        internal T Resolve<T>()
        {
            return Unity.Resolve<T>();
        }
    }
}
