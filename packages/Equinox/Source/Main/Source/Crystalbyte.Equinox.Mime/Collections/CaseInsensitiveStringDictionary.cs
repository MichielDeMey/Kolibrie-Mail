using System;
using System.Collections.Generic;

namespace Crystalbyte.Equinox.Mime.Collections
{
    public class CaseInsensitiveStringDictionary : IDictionary<string, string>
    {
        private Dictionary<string, string> _innerDictionary;

        public CaseInsensitiveStringDictionary()
        {
            _innerDictionary = new Dictionary<string, string>();
        }

        public void Add(string key, string value)
        {
            var normalizedKey = key.ToLower();
            _innerDictionary.Add(normalizedKey, value);
        }

        public bool ContainsKey(string key)
        {
            var normalizedKey = key.ToLower();
            return _innerDictionary.ContainsKey(normalizedKey);
        }

        public ICollection<string> Keys
        {
            get { return _innerDictionary.Keys; }
        }

        public bool Remove(string key)
        {
            var normalizedKey = key.ToLower();
            return _innerDictionary.Remove(normalizedKey);
        }

        public bool TryGetValue(string key, out string value)
        {
            var normalizedKey = key.ToLower();
            return _innerDictionary.TryGetValue(normalizedKey, out value);
        }

        public ICollection<string> Values
        {
            get { return _innerDictionary.Values; }
        }

        public string this[string key]
        {
            get
            {
                var normalizedKey = key.ToLower();
                return _innerDictionary[normalizedKey];
            }
            set
            {
                var normalizedKey = key.ToLower();
                _innerDictionary[normalizedKey] = value;
            }
        }

        public void Add(KeyValuePair<string, string> item)
        {
            var normalizedKey = item.Key.ToLower();
            _innerDictionary.Add(normalizedKey, item.Value);
        }

        public void Clear()
        {
            _innerDictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            var normalizedKey = item.Key.ToLower();
            return _innerDictionary.ContainsKey(normalizedKey)
                && _innerDictionary[normalizedKey] == item.Value;
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _innerDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            var normalizedKey = item.Key.ToLower();
            return _innerDictionary.Remove(normalizedKey);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _innerDictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    } 
}
