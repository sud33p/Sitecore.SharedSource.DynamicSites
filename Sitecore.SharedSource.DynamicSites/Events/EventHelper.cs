using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Publishing;

namespace Sitecore.SharedSource.DynamicSites.Events
{
    public class EventHelper
    {
        public static void GetPublishingInfo(EventArgs args, out Item rootItem, out Database database)
        {
            rootItem = null;
            database = null;

            //Check for a local event first.
            var eventArgs = args as SitecoreEventArgs;
            if (eventArgs != null)
            {
                if (eventArgs.Parameters == null || !eventArgs.Parameters.Any())
                {
                    Log.Fatal("No event args parameter", typeof(EventHelper));
                    return;
                }
                var publisher = eventArgs.Parameters[0] as Publisher;
                if (publisher == null)
                {
                    Log.Fatal("Publisher is null", typeof(EventHelper));
                    return;
                }

                var publishOptions = publisher.Options;
                if (publishOptions == null)
                {
                    Log.Fatal("publishOptions is null", typeof(EventHelper));
                    return;
                }

                rootItem = publishOptions.RootItem;
                database = publishOptions.TargetDatabase;
            }
            if (rootItem != null && database != null) return;

            //There is the posibility of this being a remote event.
            var remoteEventArgs = args as PublishEndRemoteEventArgs;
            if (remoteEventArgs != null)
            {
                if (!string.IsNullOrWhiteSpace(remoteEventArgs.TargetDatabaseName))
                {
                    database = Sitecore.Data.Database.GetDatabase(remoteEventArgs.TargetDatabaseName);
                    if (database != null)
                    {
                        rootItem = database.GetItem(new ID(remoteEventArgs.RootItemId));
                    }
                }
            }
        }
    }
}
