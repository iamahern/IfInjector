using System;
using System.Linq;
using System.Xml.Linq;

namespace IocPerformance.Adapters
{
    public abstract class ContainerAdapterBase : IContainerAdapter
    {
		private XDocument PackagesConfig() {
			try {
				return XDocument.Load("packages.config");
			} catch {
				return XDocument.Load("../../packages.config");
			}
		}

        public virtual string Version
        {
            get
            {
				try {
					return PackagesConfig()
	                      .Root
	                      .Elements()
	                      .First(e => e.Attribute("id").Value == this.PackageName)
	                      .Attribute("version").Value;
				} catch (Exception e) {
					Console.WriteLine ("++++++++++ Error for " + this.PackageName);
					throw e;
				}
            }
        }

        public virtual string Name
        {
            get { return this.PackageName; }
        }

        public abstract string PackageName
        {
            get;
        }

        public abstract string Url
        {
            get;
        }

        public virtual bool SupportsInterception
        {
            get { return false; }
        }

        public virtual bool SupportsPropertyInjection
        {
            get { return false; }
        }

        public virtual bool SupportsConditional
        {
            get { return false; }
        }

        public virtual bool SupportGeneric
        {
            get { return false; }
        }

        public virtual bool SupportsMultiple
        {
            get { return false; }
        }

        public abstract void Prepare();

        public abstract object Resolve(Type type);

        public virtual object ResolveProxy(Type type)
        {
            return this.Resolve(type);
        }

        public abstract void Dispose();
    }
}