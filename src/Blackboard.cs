using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor
{
    public class Blackboard : Dec.IRecordable
    {
        private Dictionary<ulong, object> data = new Dictionary<ulong, object>();
        private Dictionary<ulong, Type> types = new Dictionary<ulong, Type>();  // this is kind of redundant and should maybe be removed to make cloning faster?

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
                types[uid] = typeof(T);
            }
        }

        public T Get<T>(BlackboardParameter<T> id)
        {
            if (id.identifier == null)
            {
                return id.constant;
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
            if (id.identifier != null && !types.ContainsKey(id.identifier.Value.uid))
            {
                Dbg.Err($"Parameter `{id}` is not a known blackboard parameter; when building the tree, either include it as part of an Arbor.Node or register it with `BlackboardParameter<>.RegisterWith()`");
                return;
            }

            TrySet(id, item);
        }

        /// <summary>
        /// Like <see cref="Set"/>, but treats "the tree never registered this parameter" as a valid no-op instead of an error. Use for parameters that may legitimately be absent from a given tree (e.g. the owning entity, which is only present when the tree contains an EntityNode).
        /// </summary>
        public void TrySet<T>(BlackboardParameter<T> id, T item)
        {
            if (id.identifier == null)
            {
                Dbg.Err("Attempted to set a blackboard parameter from a constant");
                return;
            }

            var uid = id.identifier.Value.uid;
            if (!types.ContainsKey(uid))
            {
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
            var tree = State.Current.Value.tree;
            foreach (var kvp in types)
            {
                if (data.TryGetValue(kvp.Key, out var value))
                {
                    yield return new KeyValuePair<string, object>(tree.blackboardRegistrations[tree.blackboardLocalIdLookup[kvp.Key]].name, value);
                }
                else
                {
                    // if the data is missing, we return a default value of the type
                    yield return new KeyValuePair<string, object>(tree.blackboardRegistrations[tree.blackboardLocalIdLookup[kvp.Key]].name, Activator.CreateInstance(kvp.Value));
                }
            }
        }

        public void Record(Dec.Recorder recorder)
        {
            if (recorder.Intent == Dec.Recorder.Purpose.Cloning)
            {
                // just copy it all over
                recorder.Record(ref data, nameof(data));
                recorder.Record(ref types, nameof(types));  // this is actually immutable and we should just be copying a reference (dec does not yet support this)
                return;
            }

            if (recorder.Mode == Dec.Recorder.Direction.Write)
            {
                // gotta convert data to canonical IDs
                var tree = State.Current.Value.tree;
                var canonData = new object[tree.blackboardLocalIdLookup.Count];
                foreach (var kvp in data)
                {
                    // this might be sparse, and that's OK
                    canonData[tree.blackboardLocalIdLookup[kvp.Key]] = kvp.Value;
                }
                recorder.RecordAsThis(ref canonData);

                // labels will be recreated from the treedec, we don't want to serialize it separately
            }
            else if (recorder.Mode == Dec.Recorder.Direction.Read)
            {
                var canonData = new object[State.Current.Value.tree.blackboardLocalIdLookup.Count];
                recorder.RecordAsThis(ref canonData);

                data = new Dictionary<ulong, object>(canonData.Length);
                for (int i = 0; i < canonData.Length; i++)
                {
                    if (canonData[i] != null)
                    {
                        data[State.Current.Value.tree.blackboardLocalId[i].uid] = canonData[i];
                    }
                }

                // now we need to yank types out of the tree
                var tree = State.Current.Value.tree;
                types = new Dictionary<ulong, Type>();
                for (int i = 0; i < tree.blackboardRegistrations.Count; i++)
                {
                    var type = tree.blackboardRegistrations[i].type;
                    var uid = tree.blackboardLocalId[i].uid;

                    types[uid] = type;
                }
            }
        }
    }
}
