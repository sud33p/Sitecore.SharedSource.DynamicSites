using System;
using System.Linq;
using System.Web;
using Sitecore.Sites;

namespace Sitecore.SharedSource.DynamicSites.Sites
{
    /// <summary>Sitecore Sites Provider that combines sites from other providers with their orders patched</summary>
    public class OrderedSitecoreSiteProvider : SitecoreSiteProvider
    {
        /// <summary>Gets the list of all known sites.</summary>
        /// <returns>The list of all known sites.</returns>
        public override SiteCollection GetSites()
        {
            //get collection from base
            var siteCollection = base.GetSites();

            //begin sort
            var patchEntries = siteCollection.Where(s => !string.IsNullOrWhiteSpace(s.Properties["placement"])).ToList();
            foreach (var patchSite in patchEntries)
            {
                var patchAttrib = HttpUtility.UrlDecode(patchSite.Properties["placement"]);
                var patchKeys = patchAttrib.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (patchKeys.Length < 2)   //needs to have at least 2 to proceed further
                {
                    continue;
                }

                var targetSiteName = patchKeys[1];

                //make sure patch target site name is not the same as current
                if (targetSiteName.Equals(patchSite.Name, StringComparison.OrdinalIgnoreCase))   //invalid
                {
                    continue;
                }

                //try to find the entry we are patching against
                var targetSite = siteCollection.FirstOrDefault(s => s.Name.Equals(targetSiteName, StringComparison.OrdinalIgnoreCase));
                if (targetSite == null)   //invalid site name
                {
                    continue;
                }

                //calculate patching pos
                int targetIndex;
                switch (patchKeys[0].ToLower())
                {
                    case "before":
                        //patch it before
                        targetIndex = siteCollection.IndexOf(targetSite);
                        break;
                    case "after":
                        //patch it after
                        targetIndex = siteCollection.IndexOf(targetSite) + 1;
                        break;
                    default:
                        //we don't know what to do here
                        continue;
                }

                //start patching for all sites in the group
                var removed = siteCollection.Remove(patchSite);
                if (removed)
                {
                    //add/insert to new index
                    if (targetIndex > siteCollection.Count)
                    {
                        siteCollection.Add(patchSite);
                    }
                    else
                    {
                        siteCollection.Insert(targetIndex, patchSite);
                    }
                }
            }
            return siteCollection;
        }
    }
}
