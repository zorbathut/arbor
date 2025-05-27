using System;
using System.Collections.Generic;

namespace Arbor
{
    public class Blackboard : Dec.IRecordable
    {
        private Dictionary<ulong, object> data = new Dictionary<ulong, object>();
        private Dictionary<ulong, string> labels = new Dictionary<ulong, string>();
        private Dictionary<ulong, Type> types = new Dictionary<ulong, Type>();

        internal static bool writeOnly = false;

        public void Register<T>(BlackboardParameter<T> id)
        {
            if (id.identifier == null)
            {
                Dbg.Err("Attempted to register a blackboard parameter from a constant");
                return;
            }

            var uid = id.identifier.Value.uid;
            if (types.ContainsKey(uid))
            {
                if (types[uid] != typeof(T))
                {
                    Dbg.Err($"Type mismatch: parameter `{id}` is registered as {types[uid]} but being accessed as {typeof(T)}");
                    return;
                }
            }
            else
            {
                labels[uid] = id.identifier.Value.label;
                types[uid] = typeof(T);
            }
        }

        public T Get<T>(BlackboardParameter<T> id)
        {
            if (id.identifier == null)
            {
                Dbg.Err("Attempted to get a blackboard parameter from a constant");
                return default;
            }

            if (writeOnly)
            {
                Dbg.Err(
                    "Attempting to read from Blackboard during initialization; this is not allowed because trees may override blackboard values");
                return default;
            }

            var uid = id.identifier.Value.uid;
            if (!types.ContainsKey(uid))
            {
                Dbg.Err($"Parameter `{id}` is not a known blackboard parameter; when building the tree, either include it as part of an Arbor.Node or register it with `BlackboardParameter<>.RegisterWith()`");
                return default;
            }

            if (types[uid] != typeof(T))
            {
                Dbg.Err($"Type mismatch: parameter `{id}` is registered as {types[uid]} but being accessed as {typeof(T)}");
                return default;
            }

            data.TryGetValue(uid, out object result);
            if (result != null)
            {
                return (T)result;
            }

            return default;
        }

        public void Set<T>(BlackboardParameter<T> id, T item)
        {
            if (id.identifier == null)
            {
                Dbg.Err("Attempted to set a blackboard parameter from a constant");
                return;
            }

            var uid = id.identifier.Value.uid;
            if (!types.ContainsKey(uid))
            {
                Dbg.Err($"Parameter `{id}` is not a known blackboard parameter; when building the tree, either include it as part of an Arbor.Node or register it with `BlackboardParameter<>.RegisterWith()`");
                return;
            }

            if (types[uid] != typeof(T))
            {
                Dbg.Err($"Type mismatch: parameter `{id}` is registered as {types[uid]} but being accessed as {typeof(T)}");
                return;
            }

            data[uid] = item;
        }

        public void RegisterAndSet<T>(BlackboardParameter<T> id, T item)
        {
            Register(id);
            Set(id, item);
        }

        public IEnumerable<KeyValuePair<string, object>> GetAll_Debug()
        {
            // note: data may be empty, this is permitted!
            // unfortunately we can't just do `default` because this is object world
            foreach (var kvp in types)
            {
                if (data.TryGetValue(kvp.Key, out var value))
                {
                    yield return new KeyValuePair<string, object>(labels[kvp.Key], value);
                }
                else
                {
                    // if the data is missing, we return a default value of the type
                    yield return new KeyValuePair<string, object>(labels[kvp.Key], Activator.CreateInstance(kvp.Value));
                }
            }
        }

        public void Record(Dec.Recorder recorder)
        {
            // this is not yet valid for long-term persistence, fix later
            // (we'll have to, what, convert it to/from canonical form according to the associated tree?)
            recorder.Record(ref data, nameof(data));
            recorder.Record(ref labels, nameof(labels));
            recorder.Record(ref types, nameof(types));
        }
    }
}
