using Raven.Client;
using Raven.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IMAP.Popup.Models
{
    public class PersistanceModel
    {
        private readonly IDocumentStore _documentStore;
        private const string ConfigurationDocumentId = "imap_popup/settings";
        private const string KeyValueDocumentId = "imap_popup/kvp";

        public PersistanceModel(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public bool Contains(string key)
        {
            using (var session = _documentStore.OpenSession())
            {
                var data = session.Load<RavenJObject>(KeyValueDocumentId);
                if (data == null)
                    return false;
                return data.ContainsKey(key);
            }
        }

        public T Get<T>(string key)
        {
            using (var session = _documentStore.OpenSession())
            {
                var data = session.Load<RavenJObject>(KeyValueDocumentId) ?? new RavenJObject();

                if (!data.ContainsKey(key))
                    return default(T);

                return data.Value<T>(key);
            }
        }

        public void Set<T>(string key, T value)
        {
            using (var session = _documentStore.OpenSession())
            {
                var data = session.Load<RavenJObject>(KeyValueDocumentId);
                if(data == null)                
                {
                    data = new RavenJObject();
                    session.Store(data,KeyValueDocumentId);
                    session.SaveChanges();
                }

                data[key] = RavenJToken.FromObject(value);

                session.SaveChanges();
            }
        }

        public Configuration LoadConfiguration()
        {
            using (var session = _documentStore.OpenSession())
                return session.Load<Configuration>(ConfigurationDocumentId) ?? new Configuration();
        }

        public void SaveConfiguration(Configuration configuration)
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(configuration,ConfigurationDocumentId);
                session.SaveChanges();
            }

            if (OnConfigurationSave != null)
                OnConfigurationSave(configuration);
        }

        public event Action<Configuration> OnConfigurationSave;
    }
}
