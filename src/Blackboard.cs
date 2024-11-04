using System;
using System.Collections.Generic;

namespace Arbor
{
    public class Blackboard : Dec.IRecordable
    {
        private Dictionary<string, object> data = new Dictionary<string, object>();
        private Dictionary<string, Type> types = new Dictionary<string, Type>();

        public void Register(string id, Type type)
        {
            if (types.ContainsKey(id))
            {
                if (types[id] != type)
                {
                    Dbg.Err($"Type mismatch: parameter `{id}` is registered as {types[id]} but being accessed as {type}");
                    return;
                }
            }
            else
            {
                types[id] = type;
            }
        }

        public T Get<T>(string id)
        {
            if (!types.ContainsKey(id))
            {
                Dbg.Err($"Parameter `{id}` is not a known blackboard parameter; when building the tree, either include it as part of an Arbor.Node or register it with `BlackboardParameter<>.RegisterWith()`");
                return default;
            }

            if (types[id] != typeof(T))
            {
                Dbg.Err($"Type mismatch: parameter `{id}` is registered as {types[id]} but being accessed as {typeof(T)}");
                return default;
            }

            data.TryGetValue(id, out object result);
            if (result != null)
            {
                return (T)result;
            }

            return default;
        }

        public void Set<T>(string id, T item)
        {
            if (!types.ContainsKey(id))
            {
                Dbg.Err($"Parameter `{id}` is not a known blackboard parameter; when building the tree, either include it as part of an Arbor.Node or register it with `BlackboardParameter<>.RegisterWith()`");
                return;
            }

            if (types[id] != typeof(T))
            {
                Dbg.Err($"Type mismatch: parameter `{id}` is registered as {types[id]} but being accessed as {typeof(T)}");
                return;
            }

            data[id] = item;
        }

        public void RegisterAndSet<T>(string id, T item)
        {
            Register(id, typeof(T));
            Set(id, item);
        }

        public IEnumerable<KeyValuePair<string, object>> GetAll_Debug()
        {
            // splice in the types as default objects
            var dataCopy = new Dictionary<string, object>(data);
            foreach (var kvp in types)
            {
                if (!dataCopy.ContainsKey(kvp.Key))
                {
                    dataCopy[kvp.Key] = kvp.Value.IsValueType ? Activator.CreateInstance(kvp.Value) : null;
                }
            }

            return dataCopy;
        }

        public void Record(Dec.Recorder recorder)
        {
            recorder.Record(ref data, nameof(data));
            recorder.Record(ref types, nameof(types));
        }
    }
}
