#region Microsoft Public License (Ms-PL)

// // Microsoft Public License (Ms-PL)
// // 
// // This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
// // 
// // 1. Definitions
// // 
// // The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
// // 
// // A "contribution" is the original software, or any additions or changes to the software.
// // 
// // A "contributor" is any person that distributes its contribution under this license.
// // 
// // "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// // 
// // 2. Grant of Rights
// // 
// // (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// // 
// // (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// // 
// // 3. Conditions and Limitations
// // 
// // (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// // 
// // (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// // 
// // (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// // 
// // (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// // 
// // (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Crystalbyte.Equinox.Mime.Text;
using Crystalbyte.Equinox.Mime.Collections;

namespace Crystalbyte.Equinox.Mime
{
    /// <summary>
    ///   MIME defines a number of new RFC 822 header fields that are used to describe the content of a MIME entity.
    ///   http://tools.ietf.org/html/rfc2045#section-3
    ///   This document defines the initial IANA registration for permanent mail and MIME message header fields, per RFC 3864.
    ///   http://tools.ietf.org/html/rfc4021
    /// </summary>
    [DebuggerDisplay("Name = {Name}, Value = {Value}, Parameters = {Parameters}")]
    public class HeaderField
    {
        public HeaderField()
            : this(string.Empty, string.Empty) {}

        public HeaderField(string key, string value)
        {
            Name = key;
            Value = value;
            Parameters = new ParameterDictionary();
        }

        public string Name { get; set; }
        public string Value { get; set; }
        public ParameterDictionary Parameters { get; private set; }
        internal HeaderEncodingTypes HeaderEncoding { get; set; }

        public virtual string Serialize()
        {
            if (Parameters.Count > 0) {
                var parameters = Parameters.Serialize();
                return string.Format("{0}: {1};{2} {3}", Name, Value, Environment.NewLine, parameters);
            }

            // encode if necessary
            var value = TransferEncoder.EncodeHeaderIfNecessary(Value, HeaderEncoding);


            return string.Format("{0}: {1}", Name, value);
        }

        public virtual void Deserialize(string literals)
        {
            var index = literals.IndexOf(":");
            Name = literals.Substring(0, index);

            var tail = literals.Substring(index + 1).Trim();

            var decodedText = TransferEncoder.DecodeHeaderIfNecessary(tail);


            ParseRemainder(decodedText);
        }

        private void ParseRemainder(string tail)
        {
            var splitsy = tail.Split(Characters.Semicolon);
            if (splitsy.Length == 1) {
                Value = tail;
                return;
            }

            Value = splitsy[0];
            var seperatedParameters = new List<SeperatedParameter>();
            for (var i = 1; i <= splitsy.Length - 1; i++) {
                var attribute = splitsy[i];
                if (attribute.IsSeperatedAttribute()) {
                    var seperatedParameter = SeperatedParameter.FromString(attribute);
                    seperatedParameters.Add(seperatedParameter);
                    continue;
                }

                KeyValuePair<string,string> parameter;
                var success = Parameter.TryParse(attribute, out parameter);
                if (success) {
                    var key = parameter.Key;
                    var value = parameter.Value;
                    if (!Parameters.ContainsKey(key)) {
                        Parameters.Add(key, value);
                    } else {
                        Parameters[key] = value;
                    }
                } else {
                    var message = string.Format("Param skipped: {0}", attribute);
                    Debug.WriteLine(message);
                }
            }

            if (seperatedParameters.Count > 0) {
                var mergedParameters = MergeSeperatedParameters(seperatedParameters);
                Parameters.AddRange(mergedParameters);
            }
        }

        private static IEnumerable<KeyValuePair<string,string>> MergeSeperatedParameters(List<SeperatedParameter> attributes)
        {
            var index = 0;
            var result = new List<KeyValuePair<string,string>>();
            var combinedValue = string.Empty;
            var lastName = attributes[0].Name;
            attributes.Sort(new SeperatedParameterComparer());

            while (true) {
                var current = attributes[index];
                if (current.Name != lastName) {
                    var param = new KeyValuePair<string,string>(current.Name, combinedValue);
                    result.Add(param);

                    combinedValue = string.Empty;
                } else {
                    combinedValue += current.Value;
                }

                lastName = current.Name;
                if (index++ == attributes.Count - 1) {
                    // TODO: this seems fishy
                    var param = new KeyValuePair<string, string>(current.Name, combinedValue);
                    result.Add(param);
                    break;
                }
            }
            return result;
        }

        public override string ToString()
        {
            return Serialize();
        }

        #region Nested type: SeperatedParameter

        private class SeperatedParameter
        {
            private SeperatedParameter() {}

            public string Name { get; private set; }
            public int Index { get; private set; }
            public string Value { get; private set; }

            internal static SeperatedParameter FromString(string attribute)
            {
                var seperatedAttribute = new SeperatedParameter();
                var split = attribute.Split(Characters.EqualitySign);

                seperatedAttribute.Value = split[1].TrimQuotesAndWhiteSpaces();
                var innerSplit = split[0].Split(Characters.Asterisk);
                seperatedAttribute.Name = innerSplit[0].TrimQuotesAndWhiteSpaces();
                seperatedAttribute.Index = Int32.Parse(innerSplit[1]);
                return seperatedAttribute;
            }
        }

        #endregion

        #region Nested type: SeperatedParameterComparer

        private sealed class SeperatedParameterComparer : Comparer<SeperatedParameter>
        {
            public override int Compare(SeperatedParameter x, SeperatedParameter y)
            {
                var xc = x.Name.GetHashCode() + x.Index;
                var yc = y.Name.GetHashCode() + y.Index;
                return xc - yc;
            }
        }

        #endregion
    }
}