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
                    Dbg.Err("Type mismatch");
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
                Dbg.Err("Missing item");
                return default;
            }

            if (types[id] != typeof(T))
            {
                Dbg.Err("Type mismatch");
                return default;
            }

            data.TryGetValue(id, out object result);
            return (T)result;
        }

        public void Set<T>(string id, T item)
        {
            if (!types.ContainsKey(id))
            {
                Dbg.Err("Missing item");
                return;
            }

            if (types[id] != typeof(T))
            {
                Dbg.Err("Type mismatch");
                return;
            }

            data[id] = item;
        }
    }
}
