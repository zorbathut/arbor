using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arbor
{
    public class Blackboard
    {
        private readonly Dictionary<string, object> data = new Dictionary<string, object>();
        private readonly Dictionary<string, Type> types = new Dictionary<string, Type>();

        public static Blackboard Global = new Blackboard();

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
                Dbg.Err($"Missing item `{id}` when trying to get blackboard info");
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
                Dbg.Err($"Missing item `{id}` when trying to set blackboard info");
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
    }
}
