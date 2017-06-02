using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.SharedSource.DynamicSites.Utilities;
using Sitecore.Sites;
using Sitecore.Xml;

namespace Sitecore.SharedSource.DynamicSites.Sites
{
    [UsedImplicitly]
    public class DynamicSitesProvider : SiteProvider
    {
        private readonly object _initializeLocker = new object();
        private string _dynamicConfigPath;
        private Dictionary<string, Site> _dynamicSiteDictionary;

        protected Dictionary<string, Site> DynamicSites
        {
            get
            {
                if (_dynamicSiteDictionary == null)
                {
                    lock (_initializeLocker)
                    {
                        if (_dynamicSiteDictionary == null)
                        {
                            _dynamicSiteDictionary = GetDynamicSites();
                        }
                    }
                }
                return _dynamicSiteDictionary;
            }
        }

        public override Site GetSite(string siteName)
        {
            Assert.ArgumentNotNullOrEmpty(siteName,"siteName");
            return _dynamicSiteDictionary.GetSiteByKey(DynamicSiteManager.CleanCacheKeyName(siteName));
        }

        public override SiteCollection GetSites()
        {
            var siteCollection = new SiteCollection();
            siteCollection.AddRange((IEnumerable<Site>)DynamicSites.Values);
            return siteCollection;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            Assert.ArgumentNotNullOrEmpty(name, "name");
            Assert.ArgumentNotNull(config, "config");
            base.Initialize(name, config);
            _dynamicConfigPath = config["siteConfig"];
        }

        public void Reset()
        {
            _dynamicSiteDictionary = (Dictionary<string, Site>)null;
        }

        private Dictionary<string, Site> GetDynamicSites()
        {
            Assert.IsNotNullOrEmpty(_dynamicConfigPath,
                    "No siteConfig specified in DynamicSiteProvider configuration.");
            var collection = new SiteCollection();

            var nodes = Factory.GetConfigNodes(FileUtil.MakePath(_dynamicConfigPath, "defaultsite", '/'));
            Assert.IsFalse((nodes.Count > 1 ? 1 : 0) != 0, "Duplicate Dynamic Default Site Definition.");

            if (nodes.Count == 0)
            {
                return new Dictionary<string, Site>();
            }

            var defaultSite = ParseDefaultNode(nodes[0]);

            //Create Dictionary
            var siteDictionary = DynamicSiteManager.GetDynamicSitesDictionary(defaultSite).ToDictionary(k => k.Key, v => v.Value);
            ResolveInheritance(siteDictionary);

            return siteDictionary;
        }

        private Site ParseDefaultNode(XmlNode node)
        {
            var attributeDictionary = XmlUtil.GetAttributeDictionary(node);
            return new Site(DynamicSiteSettings.SiteName, attributeDictionary);
        }

        private void AddInheritedProperties(Site site, Dictionary<string, Site> siteDictionary)
        {
            var index = site.Properties["inherits"];
            var inheritedSite = siteDictionary.GetSiteByKey(DynamicSiteManager.CleanCacheKeyName(index));

            Assert.IsNotNull(inheritedSite, "Could not find base site '{0}' for site '{1}'.", DynamicSiteManager.CleanCacheKeyName(index), site.Name);

            foreach (var keyValuePair in inheritedSite.Properties.Where(keyValuePair => !site.Properties.ContainsKey(keyValuePair.Key)))
            {
                site.Properties[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        private void ResolveInheritance(Dictionary<string, Site> siteDictionary)
        {
            foreach (var site in siteDictionary.Where(site => !string.IsNullOrEmpty(site.Value.Properties["inherits"])))
            {
                AddInheritedProperties(site.Value, siteDictionary);
            }
        }
    }
}
